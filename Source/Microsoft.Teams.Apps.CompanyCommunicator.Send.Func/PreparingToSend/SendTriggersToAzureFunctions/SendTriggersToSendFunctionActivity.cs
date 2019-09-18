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
                await this.GetRecipientStatusDictionaryAsync(notificationDataEntityId);

            var tasks = new List<Task>();
            foreach (var batch in recipientDataBatches)
            {
                var task = context.CallActivityWithRetryAsync(
                    nameof(SendTriggersToSendFunctionActivity.SendTriggersToSendFunctionAsync),
                    new RetryOptions(TimeSpan.FromSeconds(5), 3),
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
        /// Send trigger to the Azure send function.
        /// 1). Send trigger for recipients whose status is 0 only.
        /// 2). Set recipients' status to 1 after sending triggers for them.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(SendTriggersToSendFunctionAsync))]
        public async Task SendTriggersToSendFunctionAsync(
            [ActivityTrigger] SendTriggersToSendFunctionActivityDTO input)
        {
            var recipientDataBatch = input.RecipientDataBatch;
            var recipientStatusDictionary = input.RecipientStatusDictionary;
            var notificationDataEntityId = input.NotificationDataEntityId;

            var messages = recipientDataBatch
                .Select(userDataEntity =>
                    this.CreateServiceBusQueueMessage(
                        userDataEntity,
                        notificationDataEntityId,
                        recipientStatusDictionary))
                .Where(message => message != null)
                .ToList();

            await this.sendMessageQueue.SendAsync(messages);

            await this.metadataProvider.SetStatusInSentNotificationDataAsync(
                notificationDataEntityId,
                recipientDataBatch,
                1);
        }

        private Message CreateServiceBusQueueMessage(
            UserDataEntity userDataEntity,
            string notificationDataEntityId,
            IDictionary<string, int> recipientStatusDictionary)
        {
            if (recipientStatusDictionary.TryGetValue(userDataEntity.RowKey, out int status))
            {
                if (status == 0)
                {
                    var queueMessageContent = new SendQueueMessageContent
                    {
                        NotificationDataEntityId = notificationDataEntityId,
                        UserDataEntity = userDataEntity,
                    };
                    var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                    return new Message(Encoding.UTF8.GetBytes(messageBody));
                }
            }

            return null;
        }

        private async Task<IDictionary<string, int>> GetRecipientStatusDictionaryAsync(string notificationDataEntityId)
        {
            var recipientStatusDictionary = new Dictionary<string, int>();

            var sentNotificationDataEntityList =
                await this.metadataProvider.GetSentNotificationDataEntityListAsync(notificationDataEntityId);

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
