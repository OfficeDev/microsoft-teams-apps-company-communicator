// <copyright file="DeliveryPretreatmentOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Notification delivery pretreatment service.
    /// </summary>
    public class DeliveryPretreatmentOrchestration
    {
        /// <summary>
        /// Pretreat notification delivery for target users.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(PretreatAsync))]
        public async Task PretreatAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var draftNotificationEntity = context.GetInput<NotificationDataEntity>();

            var deduplicatedReceiverEntities = await this.GetAudienceDataListAsync(context, draftNotificationEntity);

            var newSentNotificationId = await this.MoveDraftToSentPartitionAsync(
                context,
                draftNotificationEntity,
                deduplicatedReceiverEntities);

            await this.CreateSendingNotificationAsync(context, draftNotificationEntity, newSentNotificationId);

            await this.SendTriggersToSendFunctionAsync(context, deduplicatedReceiverEntities, newSentNotificationId);

            await this.SendTriggerToDataFunctionAsync(context, deduplicatedReceiverEntities, newSentNotificationId);
        }

        private async Task SendTriggerToDataFunctionAsync(
            DurableOrchestrationContext context,
            IList<UserDataEntity> deduplicatedReceiverEntities,
            string newSentNotificationId)
        {
            await context.CallActivityAsync(
                nameof(SendTriggerToDataFunctionActivity.SendTriggerToDataFunctionAsync),
                new SendTriggerToDataFunctionActivityDTO
                {
                    NotificationId = newSentNotificationId,
                    TotalMessageCount = deduplicatedReceiverEntities.Count,
                });
        }

        private async Task SendTriggersToSendFunctionAsync(
            DurableOrchestrationContext context,
            IList<UserDataEntity> deduplicatedReceiverEntities,
            string newSentNotificationId)
        {
            await context.CallActivityAsync(
                nameof(SendTriggersToSendFunctionActivity.SendTriggersToSendFunctionAsync),
                new SendTriggersToSendFunctionActivityDTO
                {
                    DeduplicatedReceiverEntities = deduplicatedReceiverEntities,
                    NewSentNotificationId = newSentNotificationId,
                });
        }

        private async Task CreateSendingNotificationAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity draftNotificationEntity,
            string newSentNotificationId)
        {
            draftNotificationEntity.RowKey = newSentNotificationId;
            await context.CallActivityAsync(
                nameof(CreateSendingNotificationActivity.CreateSendingNotificationAsync),
                draftNotificationEntity);
        }

        private async Task<string> MoveDraftToSentPartitionAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity draftNotificationEntity,
            IList<UserDataEntity> deduplicatedReceiverEntities)
        {
            return await context.CallActivityAsync<string>(
                nameof(MoveDraftToSentNotificationPartitionActivity.MoveDraftToSentNotificationPartitionAsync),
                new MoveDraftToSentNotificationPartitionActivityDTO
                {
                    DraftNotificationEntity = draftNotificationEntity,
                    TotalAudienceCount = deduplicatedReceiverEntities.Count,
                });
        }

        private async Task<IList<UserDataEntity>> GetAudienceDataListAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity draftNotificationEntity)
        {
            return await context.CallActivityAsync<IList<UserDataEntity>>(
                nameof(GetAudienceDataListActivity.GetAudienceDataListAsync),
                draftNotificationEntity);
        }
    }
}