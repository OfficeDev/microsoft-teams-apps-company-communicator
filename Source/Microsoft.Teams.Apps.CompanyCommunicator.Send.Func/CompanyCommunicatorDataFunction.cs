// <copyright file="CompanyCommunicatorDataFunction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func
{
    using System;
    using System.Net;
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
    using Newtonsoft.Json;

    /// <summary>
    /// Azure Function App triggered by messages from a Service Bus queue
    /// Used for aggregating results for a sent notification.
    /// </summary>
    public class CompanyCommunicatorDataFunction
    {
        private static SentNotificationDataRepository sentNotificationDataRepository = null;

        private static NotificationDataRepository notificationDataRepository = null;

        private static SendingNotificationDataRepository sendingNotificationDataRepository = null;

        private static IConfiguration configuration = null;

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
            CompanyCommunicatorDataFunction.configuration = CompanyCommunicatorDataFunction.configuration ??
                new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();

            // Simply initialize the variable for certain build environments and versions
            var maxMinutesOfRetryingDataFunction = 0;

            // If parsing fails, out variable is set to 0, so need to set the default
            if (!int.TryParse(CompanyCommunicatorDataFunction.configuration["MaxMinutesOfRetryingDataFunction"], out maxMinutesOfRetryingDataFunction))
            {
                maxMinutesOfRetryingDataFunction = 1440;
            }

            var messageContent = JsonConvert.DeserializeObject<ServiceBusDataQueueMessageContent>(myQueueItem);

            CompanyCommunicatorDataFunction.sentNotificationDataRepository = CompanyCommunicatorDataFunction.sentNotificationDataRepository
                ?? new SentNotificationDataRepository(CompanyCommunicatorDataFunction.configuration, isFromAzureFunction: true);

            CompanyCommunicatorDataFunction.notificationDataRepository = CompanyCommunicatorDataFunction.notificationDataRepository
                ?? this.CreateNotificationRepository(CompanyCommunicatorDataFunction.configuration);

            CompanyCommunicatorDataFunction.sendingNotificationDataRepository = CompanyCommunicatorDataFunction.sendingNotificationDataRepository
                ?? new SendingNotificationDataRepository(CompanyCommunicatorDataFunction.configuration, isFromAzureFunction: true);

            var sentNotificationDataEntities = await CompanyCommunicatorDataFunction.sentNotificationDataRepository.GetAllAsync(
                messageContent.NotificationId);

            if (sentNotificationDataEntities == null)
            {
                if (DateTime.UtcNow >= messageContent.InitialSendDate + TimeSpan.FromMinutes(maxMinutesOfRetryingDataFunction))
                {
                    await this.SetEmptyNotificationDataEntity(messageContent.NotificationId);
                    return;
                }

                await this.SendTriggerToDataFunction(CompanyCommunicatorDataFunction.configuration, messageContent);
                return;
            }

            var succeededCount = 0;
            var failedCount = 0;
            var throttledCount = 0;
            var unknownCount = 0;

            DateTime lastSentDateTime = DateTime.MinValue;

            foreach (var sentNotificationDataEntity in sentNotificationDataEntities)
            {
                if (sentNotificationDataEntity.StatusCode == (int)HttpStatusCode.Created)
                {
                    succeededCount++;
                }
                else if (sentNotificationDataEntity.StatusCode == (int)HttpStatusCode.TooManyRequests)
                {
                    throttledCount++;
                }
                else if (sentNotificationDataEntity.StatusCode == 0)
                {
                    unknownCount++;
                }
                else
                {
                    failedCount++;
                }

                if (sentNotificationDataEntity.SentDate != null
                    && sentNotificationDataEntity.SentDate > lastSentDateTime)
                {
                    lastSentDateTime = sentNotificationDataEntity.SentDate ?? DateTime.MinValue;
                }
            }

            var notificationDataEntityUpdate = new UpdateNotificationDataEntity
            {
                PartitionKey = PartitionKeyNames.NotificationDataTable.SentNotificationsPartition,
                RowKey = messageContent.NotificationId,
                Succeeded = succeededCount,
                Failed = failedCount,
                Throttled = throttledCount,
                Unknown = unknownCount,
            };

            // Purposefully exclude the unknown count because those messages may be sent later
            var currentMessageCount = succeededCount
                + failedCount
                + throttledCount;

            if (currentMessageCount == messageContent.TotalMessageCount
                || DateTime.UtcNow >= messageContent.InitialSendDate + TimeSpan.FromMinutes(maxMinutesOfRetryingDataFunction))
            {
                notificationDataEntityUpdate.IsCompleted = true;
                notificationDataEntityUpdate.SentDate = lastSentDateTime != DateTime.MinValue
                    ? lastSentDateTime
                    : DateTime.UtcNow;
            }
            else
            {
                await this.SendTriggerToDataFunction(CompanyCommunicatorDataFunction.configuration, messageContent);
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
