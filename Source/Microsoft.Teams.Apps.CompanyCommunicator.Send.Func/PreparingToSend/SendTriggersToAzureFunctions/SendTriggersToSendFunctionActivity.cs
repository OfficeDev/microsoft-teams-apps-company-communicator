// <copyright file="SendTriggersToSendFunctionActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// Send triggers to the Azure send function activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class SendTriggersToSendFunctionActivity
    {
        private readonly SendQueue sendMessageQueue;
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTriggersToSendFunctionActivity"/> class.
        /// </summary>
        /// <param name="sendMessageQueue">Send message queue service.</param>
        /// <param name="metadataProvider">Metadata provider.</param>
        public SendTriggersToSendFunctionActivity(
            SendQueue sendMessageQueue,
            MetadataProvider metadataProvider)
        {
            this.sendMessageQueue = sendMessageQueue;
            this.metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="recipientDataBatches">Receiver batches.</param>
        /// <param name="notificationDataEntityId">New sent notification id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            IEnumerable<IEnumerable<UserDataEntity>> recipientDataBatches,
            string notificationDataEntityId)
        {
            var recipientStatusDictionary =
                this.GetRecipientStatusDictionary(notificationDataEntityId);

            var tasks = new List<Task>();
            foreach (var batch in recipientDataBatches)
            {
                var task = context.CallSubOrchestratorAsync(
                    nameof(SendTriggersToSendFunctionActivity.ProcessRecipientBatchAsync),
                    new SendTriggersToSendFunctionActivityDTO
                    {
                        NotificationDataEntityId = notificationDataEntityId,
                        RecipientDataBatch = batch,
                        RecipientStatusDictionary = recipientStatusDictionary,
                    });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Process a recipient batch in a durable orchestration.
        /// 1). Send trigger for recipients whose status is 0 only.
        /// 2). Set recipients' status to 1 after sending triggers to the send queue.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [FunctionName(nameof(ProcessRecipientBatchAsync))]
        public async Task ProcessRecipientBatchAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var input = context.GetInput<SendTriggersToSendFunctionActivityDTO>();

            try
            {
                await context.CallActivityWithRetryAsync(
                    nameof(SendTriggersToSendFunctionActivity.SendTriggersToSendFunctionAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
                    input);

                await context.CallActivityWithRetryAsync(
                    nameof(SendTriggersToSendFunctionActivity.SetRecipientBatchSatusAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
                    input);
            }
            catch (Exception ex)
            {
                await this.metadataProvider.SaveWarningInNotificationDataEntityAsync(
                    input.NotificationDataEntityId,
                    ex.Message);
            }
        }

        /// <summary>
        /// Send trigger to the Azure send function.
        /// Send trigger for recipients whose status is 0 only.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggersToSendFunctionAsync))]
        public async Task SendTriggersToSendFunctionAsync(
            [ActivityTrigger] SendTriggersToSendFunctionActivityDTO input)
        {
            var recipientDataBatch = input.RecipientDataBatch;
            var notificationDataEntityId = input.NotificationDataEntityId;
            var recipientStatusDictionary = input.RecipientStatusDictionary;

            var messages = recipientDataBatch
                .Select(recipientData =>
                    this.CreateServiceBusQueueMessage(
                        recipientData,
                        notificationDataEntityId,
                        recipientStatusDictionary))
                .Where(message => message != null)
                .ToList();

            await this.sendMessageQueue.SendAsync(messages);
        }

        /// <summary>
        /// Set recipients' status to 1 after sending triggers to the send queue.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SetRecipientBatchSatusAsync))]
        public async Task SetRecipientBatchSatusAsync(
            [ActivityTrigger] SendTriggersToSendFunctionActivityDTO input)
        {
            var notificationDataEntityId = input.NotificationDataEntityId;
            var recipientDataBatch = input.RecipientDataBatch;

            await this.metadataProvider.SetStatusInSentNotificationDataAsync(
                notificationDataEntityId,
                recipientDataBatch);
        }

        private Message CreateServiceBusQueueMessage(
            UserDataEntity recipientData,
            string notificationDataEntityId,
            IDictionary<string, int> recipientStatusDictionary)
        {
            if (recipientStatusDictionary.TryGetValue(recipientData.AadId, out int status))
            {
                if (status == 0)
                {
                    var queueMessageContent = new SendQueueMessageContent
                    {
                        NotificationId = notificationDataEntityId,
                        UserDataEntity = recipientData,
                    };
                    var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                    return message;
                }
            }

            return null;
        }

        private IDictionary<string, int> GetRecipientStatusDictionary(string notificationDataEntityId)
        {
            var recipientStatusDictionary = new Dictionary<string, int>();

            var sentNotificationDataEntityList = this.metadataProvider
                .GetSentNotificationDataEntityListAsync(notificationDataEntityId)
                .Result;

            foreach (var sentNotificationDataEntity in sentNotificationDataEntityList)
            {
                recipientStatusDictionary.Add(
                    sentNotificationDataEntity.RowKey,
                    sentNotificationDataEntity.StatusCode);
            }

            return recipientStatusDictionary;
        }
    }
}
