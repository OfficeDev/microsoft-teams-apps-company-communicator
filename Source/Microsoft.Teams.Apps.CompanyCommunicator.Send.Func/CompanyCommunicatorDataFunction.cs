// <copyright file="CompanyCommunicatorDataFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services;
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for aggregating results for a sent notification.
    /// </summary>
    public class CompanyCommunicatorDataFunction
    {
        private static readonly int MaxMinutesOfRetrying = 35;

        private static SentNotificationDataRepository sentNotificationDataRepository = null;

        private static NotificationDataRepository notificationDataRepository = null;

        private static SendingNotificationDataRepository sendingNotificationDataRepository = null;

        private static NotificationDeliveryStatusHelper notificationDeliveryStatusHelper = null;

        /// <summary>
        /// Azure Function App triggered by messages from a Service Bus queue
        /// Used for aggregating results for a sent notification.
        /// </summary>
        /// <param name="myQueueItem">The Service Bus queue item.</param>
        /// <param name="deliveryCount">The deliver count.</param>
        /// <param name="enqueuedTimeUtc">The enqueued time.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="log">The logger.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName("CompanyCommunicatorDataFunction")]
        public async Task Run(
            [ServiceBusTrigger("company-communicator-data", Connection = "ServiceBusConnection")]
            string myQueueItem,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log,
            ExecutionContext context)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var messageContent = JsonConvert.DeserializeObject<ServiceBusDataQueueMessageContent>(myQueueItem);

            CompanyCommunicatorDataFunction.sentNotificationDataRepository = CompanyCommunicatorDataFunction.sentNotificationDataRepository
                ?? new SentNotificationDataRepository(configuration, isFromAzureFunction: true);

            CompanyCommunicatorDataFunction.notificationDataRepository = CompanyCommunicatorDataFunction.notificationDataRepository
                ?? this.CreateNotificationRepository(configuration);

            CompanyCommunicatorDataFunction.sendingNotificationDataRepository = CompanyCommunicatorDataFunction.sendingNotificationDataRepository
                ?? new SendingNotificationDataRepository(configuration, isFromAzureFunction: true);

            CompanyCommunicatorDataFunction.notificationDeliveryStatusHelper = CompanyCommunicatorDataFunction.notificationDeliveryStatusHelper
                ?? new NotificationDeliveryStatusHelper(CompanyCommunicatorDataFunction.sentNotificationDataRepository);

            var notificationDeliveryStatusDTO =
                await CompanyCommunicatorDataFunction.notificationDeliveryStatusHelper.GetNotificationDeliveryStatusAsync(
                    messageContent.NotificationId);
            if (notificationDeliveryStatusDTO == null)
            {
                if (DateTime.UtcNow >= messageContent.InitialSendDate + TimeSpan.FromMinutes(
                    CompanyCommunicatorDataFunction.MaxMinutesOfRetrying))
                {
                    await this.SetEmptyNotificationDataEntity(messageContent.NotificationId);
                    return;
                }

                await this.SendTriggerToDataFunction(configuration, messageContent);
                return;
            }

            var notificationDataEntityUpdate = new UpdateNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                RowKey = messageContent.NotificationId,
                Succeeded = notificationDeliveryStatusDTO.Succeeded,
                Failed = notificationDeliveryStatusDTO.Failed,
                Throttled = notificationDeliveryStatusDTO.Throttled,
                Unknown = notificationDeliveryStatusDTO.Unknown,
            };

            var currentMessageCount = notificationDeliveryStatusDTO.CurrentMessageCount;
            if (currentMessageCount == messageContent.TotalMessageCount
                || DateTime.UtcNow >= messageContent.InitialSendDate + TimeSpan.FromMinutes(
                    CompanyCommunicatorDataFunction.MaxMinutesOfRetrying))
            {
                notificationDataEntityUpdate.IsCompleted = true;

                notificationDeliveryStatusDTO.SetLastSentDateIfNotSetYet();
                notificationDataEntityUpdate.SentDate = notificationDeliveryStatusDTO.LastSentDate;
            }
            else
            {
                await this.SendTriggerToDataFunction(configuration, messageContent);
            }

            var operation = TableOperation.InsertOrMerge(notificationDataEntityUpdate);

            await CompanyCommunicatorDataFunction.notificationDataRepository.Table.ExecuteAsync(operation);
        }

        private async Task SendTriggerToDataFunction(
            IConfiguration configuration,
            ServiceBusDataQueueMessageContent queueMessageContent)
        {
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
            serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            string serviceBusConnectionString = configuration["ServiceBusConnection"];
            string queueName = "company-communicator-data";
            var messageSender = new MessageSender(serviceBusConnectionString, queueName);

            await messageSender.SendAsync(serviceBusMessage);
        }

        private NotificationDataRepository CreateNotificationRepository(IConfiguration configuration)
        {
            var tableRowKeyGenerator = new TableRowKeyGenerator();
            return new NotificationDataRepository(configuration, tableRowKeyGenerator, isFromAzureFunction: true);
        }

        private async Task SetEmptyNotificationDataEntity(string notificationId)
        {
            var notificationDataEntityUpdate = new UpdateNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                RowKey = notificationId,
                Succeeded = 0,
                Failed = 0,
                Throttled = 0,
                Unknown = 0,
            };

            notificationDataEntityUpdate.IsCompleted = true;
            notificationDataEntityUpdate.SentDate = DateTime.UtcNow;

            var operation = TableOperation.InsertOrMerge(notificationDataEntityUpdate);

            await CompanyCommunicatorDataFunction.notificationDataRepository.Table.ExecuteAsync(operation);
        }

        private class ServiceBusDataQueueMessageContent
        {
            public string NotificationId { get; set; }

            public DateTime InitialSendDate { get; set; }

            public int TotalMessageCount { get; set; }
        }

        private class UpdateNotificationDataEntity : TableEntity
        {
            /// <summary>
            /// Gets or sets the number of recipients who have received the notification successfully.
            /// </summary>
            public int? Succeeded { get; set; }

            /// <summary>
            /// Gets or sets the number of recipients who failed in receiving the notification.
            /// </summary>
            public int? Failed { get; set; }

            /// <summary>
            /// Gets or sets the number of recipients who were throttled out.
            /// </summary>
            public int? Throttled { get; set; }

            /// <summary>
            /// Gets or sets the number or recipients who have an unknown status.
            /// </summary>
            public int? Unknown { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the sending process is completed or not.
            /// </summary>
            public bool? IsCompleted { get; set; }

            /// <summary>
            /// Gets or sets the Sent DateTime value.
            /// </summary>
            public DateTime? SentDate { get; set; }
        }
    }
}
