// <copyright file="SendTriggersToAzureFunctionsOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get the recipient data batches for sending a notification.
    /// It's a durable framework sub-orchestration.
    /// </summary>
    public class SendTriggersToAzureFunctionsOrchestration
    {
        private readonly CreateSendingNotificationActivity createSendingNotificationActivity;
        private readonly SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity;
        private readonly SendTriggerToDataFunctionActivity sendTriggerToDataFunctionActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendTriggersToAzureFunctionsOrchestration"/> class.
        /// </summary>
        /// <param name="createSendingNotificationActivity">Create sending notification activity.</param>
        /// <param name="sendTriggersToSendFunctionActivity">Send triggers to send function activity.</param>
        /// <param name="sendTriggerToDataFunctionActivity">Send trigger to data function activity.</param>
        public SendTriggersToAzureFunctionsOrchestration(
            CreateSendingNotificationActivity createSendingNotificationActivity,
            SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity,
            SendTriggerToDataFunctionActivity sendTriggerToDataFunctionActivity)
        {
            this.createSendingNotificationActivity = createSendingNotificationActivity;
            this.sendTriggersToSendFunctionActivity = sendTriggersToSendFunctionActivity;
            this.sendTriggerToDataFunctionActivity = sendTriggerToDataFunctionActivity;
        }

        /// <summary>
        /// Run the orchestration.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <param name="recipientDataBatches">Recipient data batches.</param>
        /// <returns>It returns recipient data list.</returns>
        public async Task RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            IEnumerable<IEnumerable<UserDataEntity>> recipientDataBatches)
        {
            await context.CallSubOrchestratorAsync<IEnumerable<IEnumerable<UserDataEntity>>>(
                nameof(SendTriggersToAzureFunctionsOrchestration.SendTriggersToAzureFuntionsAsync),
                new SendTriggersToAzureFunctionsOrchestrationDTO
                {
                    NotificationDataEntity = notificationDataEntity,
                    RecipientDataBatches = recipientDataBatches,
                });
        }

        /// <summary>
        /// Start the get recipient data batches sub-orchestration.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <returns>Recipient data batches.</returns>
        [FunctionName(nameof(SendTriggersToAzureFunctionsOrchestration.SendTriggersToAzureFuntionsAsync))]
        public async Task SendTriggersToAzureFuntionsAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var sendTriggersToAzureFunctionsOrchestrationDTO = context.GetInput<SendTriggersToAzureFunctionsOrchestrationDTO>();
            var notificationDataEntity = sendTriggersToAzureFunctionsOrchestrationDTO.NotificationDataEntity;
            var recipientDataBatches = sendTriggersToAzureFunctionsOrchestrationDTO.RecipientDataBatches;

            await this.createSendingNotificationActivity.RunAsync(context, notificationDataEntity);

            await this.sendTriggersToSendFunctionActivity.RunAsync(context, recipientDataBatches, notificationDataEntity.Id);

            await this.sendTriggerToDataFunctionActivity.RunAsync(context, notificationDataEntity.Id, recipientDataBatches);
        }
    }
}