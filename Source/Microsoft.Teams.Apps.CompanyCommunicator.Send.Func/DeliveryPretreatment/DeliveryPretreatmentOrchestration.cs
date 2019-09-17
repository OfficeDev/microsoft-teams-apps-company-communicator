// <copyright file="DeliveryPretreatmentOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities;

    /// <summary>
    /// Notification delivery pretreatment service.
    /// </summary>
    public class DeliveryPretreatmentOrchestration
    {
        private readonly Activity1GetRecipientDataBatches getRecipientDataBatchesActivity;
        private readonly Activity2CreateSendingNotification createSendingNotificationActivity;
        private readonly Activity3SendTriggersToSendFunction sendTriggersToSendFunctionActivity;
        private readonly Activity4SendTriggerToDataFunction sendTriggerToDataFunctionActivity;
        private readonly Activity5CleanUp cleanUpActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPretreatmentOrchestration"/> class.
        /// </summary>
        /// <param name="getRecipientDataBatchesActivity">Get recipient data batches activity.</param>
        /// <param name="createSendingNotificationActivity">Create sending notification activity.</param>
        /// <param name="sendTriggersToSendFunctionActivity">Send triggers to send function activity.</param>
        /// <param name="sendTriggerToDataFunctionActivity">Send trigger to data function activity.</param>
        /// <param name="cleanUpActivity">Clean up activity.</param>
        public DeliveryPretreatmentOrchestration(
            Activity1GetRecipientDataBatches getRecipientDataBatchesActivity,
            Activity2CreateSendingNotification createSendingNotificationActivity,
            Activity3SendTriggersToSendFunction sendTriggersToSendFunctionActivity,
            Activity4SendTriggerToDataFunction sendTriggerToDataFunctionActivity,
            Activity5CleanUp cleanUpActivity)
        {
            this.getRecipientDataBatchesActivity = getRecipientDataBatchesActivity;
            this.createSendingNotificationActivity = createSendingNotificationActivity;
            this.sendTriggersToSendFunctionActivity = sendTriggersToSendFunctionActivity;
            this.sendTriggerToDataFunctionActivity = sendTriggerToDataFunctionActivity;
            this.cleanUpActivity = cleanUpActivity;
        }

        /// <summary>
        /// Start the delivery pretreatment orchestration..
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(StartOrchestrationAsync))]
        public async Task StartOrchestrationAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var notificationDataEntity = context.GetInput<NotificationDataEntity>();

            try
            {
                var receiverBatches =
                    await this.getRecipientDataBatchesActivity.RunAsync(context, notificationDataEntity, log);

                await this.createSendingNotificationActivity.RunAsync(context, notificationDataEntity);

                await this.sendTriggersToSendFunctionActivity.RunAsync(context, receiverBatches, notificationDataEntity.Id);

                await this.sendTriggerToDataFunctionActivity.RunAsync(context, notificationDataEntity.Id, receiverBatches);
            }
            catch (Exception ex)
            {
                await this.cleanUpActivity.RunAsync(context, notificationDataEntity, ex);
            }
        }
    }
}