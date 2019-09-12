// <copyright file="DeliveryPretreatment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Newtonsoft.Json;

    /// <summary>
    /// Notification delivery pretreatment service.
    /// </summary>
    public class DeliveryPretreatment
    {
        private readonly NotificationDataRepository notificationDataRepository;
        private readonly MetadataProvider metadataProvider;
        private readonly SendingNotificationCreator sendingNotificationCreator;
        private readonly SendQueue sendMessageQueue;
        private readonly DataQueue dataMessageQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPretreatment"/> class.
        /// </summary>
        /// <param name="notificationDataRepository">Notification repository service.</param>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        /// <param name="sendingNotificationCreator">SendingNotification creator.</param>
        /// <param name="sendMessageQueue">The message queue service connected to the queue 'company-communicator-send'.</param>
        /// <param name="dataMessageQueue">The message queue service connected to the queue 'company-communicator-data'.</param>
        public DeliveryPretreatment(
            NotificationDataRepository notificationDataRepository,
            MetadataProvider metadataProvider,
            SendingNotificationCreator sendingNotificationCreator,
            SendQueue sendMessageQueue,
            DataQueue dataMessageQueue)
        {
            this.notificationDataRepository = notificationDataRepository;
            this.metadataProvider = metadataProvider;
            this.sendingNotificationCreator = sendingNotificationCreator;
            this.sendMessageQueue = sendMessageQueue;
            this.dataMessageQueue = dataMessageQueue;
        }

        /// <summary>
        /// Send a notification to target users.
        /// </summary>
        /// <param name="draftNotificationEntity">The draft notification to be sent.</param>
        /// <param name="log">The logger instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task SendAsync(
            NotificationDataEntity draftNotificationEntity,
            ILogger log)
        {
            if (draftNotificationEntity == null || !draftNotificationEntity.IsDraft)
            {
                return;
            }

            var deduplicatedReceiverEntities = await this.GetDeduplicatedReceiverEntititesAsync(draftNotificationEntity, log);

            draftNotificationEntity.TotalMessageCount = deduplicatedReceiverEntities.Count;
            var newSentNotificationId = await this.notificationDataRepository.MoveDraftToSentPartitionAsync(draftNotificationEntity);

            // Set in SendingNotification data
            await this.sendingNotificationCreator.CreateAsync(newSentNotificationId, draftNotificationEntity);

            await this.SendTriggersToSendFunctionAsync(newSentNotificationId, deduplicatedReceiverEntities);

            await this.SendTriggerToDataFunction(newSentNotificationId, deduplicatedReceiverEntities.Count);
        }

        private async Task<IList<UserDataEntity>> GetDeduplicatedReceiverEntititesAsync(
            NotificationDataEntity draftNotificationEntity,
            ILogger log)
        {
            List<UserDataEntity> deduplicatedReceiverEntities = new List<UserDataEntity>();

            if (draftNotificationEntity.AllUsers)
            {
                var usersUserDataEntityDictionary = await this.metadataProvider.GetUserDataDictionaryAsync();
                deduplicatedReceiverEntities.AddRange(usersUserDataEntityDictionary.Select(kvp => kvp.Value));
                this.Log(log, draftNotificationEntity.Id, "All users");
            }
            else if (draftNotificationEntity.Rosters.Count() != 0)
            {
                var rosterUserDataEntityDictionary = await this.metadataProvider.GetTeamsRostersAsync(draftNotificationEntity.Rosters);
                deduplicatedReceiverEntities.AddRange(rosterUserDataEntityDictionary.Select(kvp => kvp.Value));
                this.Log(log, draftNotificationEntity.Id, "Rosters", deduplicatedReceiverEntities.Count);
            }
            else if (draftNotificationEntity.Teams.Count() != 0)
            {
                var teamsReceiverEntities = await this.metadataProvider.GetTeamsReceiverEntities(draftNotificationEntity.Teams);
                deduplicatedReceiverEntities.AddRange(teamsReceiverEntities);
                this.Log(log, draftNotificationEntity.Id, "General channels", deduplicatedReceiverEntities.Count);
            }
            else
            {
                this.Log(log, draftNotificationEntity.Id, "No audience was selected");
            }

            return deduplicatedReceiverEntities;
        }

        private void Log(ILogger log, string draftNotificationEntityId, string audienceOption)
        {
            log.LogInformation(
                "Notification id:{0}. Audience option: {1}",
                draftNotificationEntityId,
                audienceOption);
        }

        private void Log(ILogger log, string draftNotificationEntityId, string audienceOption, int count)
        {
            log.LogInformation(
                "Notification id:{0}. Audience option: {1}. Count: {2}",
                draftNotificationEntityId,
                audienceOption,
                count);
        }

        private async Task SendTriggersToSendFunctionAsync(
            string newSentNotificationId,
            IList<UserDataEntity> deduplicatedReceiverEntities)
        {
            var allServiceBusMessages = deduplicatedReceiverEntities
                .Select(userDataEntity =>
                {
                    var queueMessageContent = new SendQueueMessageContent
                    {
                        NotificationId = newSentNotificationId,
                        UserDataEntity = userDataEntity,
                    };
                    var messageBody = JsonConvert.SerializeObject(queueMessageContent);
                    return new Message(Encoding.UTF8.GetBytes(messageBody));
                })
                .ToList();

            await this.sendMessageQueue.SendAsync(allServiceBusMessages);
        }

        private async Task SendTriggerToDataFunction(
            string notificationId,
            int totalMessageCount)
        {
            var queueMessageContent = new DataQueueMessageContent
            {
                NotificationId = notificationId,
                InitialSendDate = DateTime.UtcNow,
                TotalMessageCount = totalMessageCount,
            };
            var messageBody = JsonConvert.SerializeObject(queueMessageContent);
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(messageBody));
            serviceBusMessage.ScheduledEnqueueTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            await this.dataMessageQueue.SendAsync(serviceBusMessage);
        }
    }
}
