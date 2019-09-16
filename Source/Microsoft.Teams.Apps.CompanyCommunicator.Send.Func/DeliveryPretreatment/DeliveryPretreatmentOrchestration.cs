// <copyright file="DeliveryPretreatmentOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities;

    /// <summary>
    /// Notification delivery pretreatment service.
    /// </summary>
    public class DeliveryPretreatmentOrchestration
    {
        private readonly Activity1GetReceiverBatches getReceiverBatchesActivity;
        private readonly Activity2MoveDraftToSentNotificationPartition moveDraftToSentPartitionActivity;
        private readonly Activity3CreateSendingNotification createSendingNotificationActivity;
        private readonly Activity4SendTriggersToSendFunction sendTriggersToSendFunctionActivity;
        private readonly Activity5SendTriggerToDataFunction sendTriggerToDataFunctionActivity;
        private readonly Activity6CleanUp cleanUpActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPretreatmentOrchestration"/> class.
        /// </summary>
        /// <param name="getReceiverBatchesActivity">Get receiver batches activity.</param>
        /// <param name="moveDraftToSentPartitionActivity">Move draft to sent notification partition.</param>
        /// <param name="createSendingNotificationActivity">Create sending notification activity.</param>
        /// <param name="sendTriggersToSendFunctionActivity">Send triggers to send function activity.</param>
        /// <param name="sendTriggerToDataFunctionActivity">Send trigger to data function activity.</param>
        /// <param name="cleanUpActivity">Clean up activity.</param>
        public DeliveryPretreatmentOrchestration(
            Activity1GetReceiverBatches getReceiverBatchesActivity,
            Activity2MoveDraftToSentNotificationPartition moveDraftToSentPartitionActivity,
            Activity3CreateSendingNotification createSendingNotificationActivity,
            Activity4SendTriggersToSendFunction sendTriggersToSendFunctionActivity,
            Activity5SendTriggerToDataFunction sendTriggerToDataFunctionActivity,
            Activity6CleanUp cleanUpActivity)
        {
            this.getReceiverBatchesActivity = getReceiverBatchesActivity;
            this.moveDraftToSentPartitionActivity = moveDraftToSentPartitionActivity;
            this.createSendingNotificationActivity = createSendingNotificationActivity;
            this.sendTriggersToSendFunctionActivity = sendTriggersToSendFunctionActivity;
            this.sendTriggerToDataFunctionActivity = sendTriggerToDataFunctionActivity;
            this.cleanUpActivity = cleanUpActivity;
        }

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

            var newSentNotificationId = string.Empty;

            try
            {
                var receiverBatches =
                    await this.getReceiverBatchesActivity.RunAsync(context, draftNotificationEntity);

                newSentNotificationId =
                    await this.moveDraftToSentPartitionActivity.RunAsync(context, draftNotificationEntity, receiverBatches);

                await this.createSendingNotificationActivity.RunAsync(context, draftNotificationEntity, newSentNotificationId);

                await this.sendTriggersToSendFunctionActivity.RunAsync(context, receiverBatches, newSentNotificationId);

                await this.sendTriggerToDataFunctionActivity.RunAsync(context, newSentNotificationId, receiverBatches);
            }
            catch (Exception ex)
            {
                await this.cleanUpActivity.RunAsync(context, draftNotificationEntity, newSentNotificationId, ex);
            }
        }
    }
}