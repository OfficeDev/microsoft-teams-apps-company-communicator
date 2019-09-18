// <copyright file="PreparingToSendOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions;

    /// <summary>
    /// This class is the duralbe framework orchestration for preparing to send notifications.
    /// </summary>
    public class PreparingToSendOrchestration
    {
        private readonly GetRecipientDataBatchesOrchestration getRecipientDataBatchesOrchestration;
        private readonly SendTriggersToAzureFunctionsOrchestration sendTriggersToAzureFunctionsOrchestration;
        private readonly CleanUpActivity cleanUpActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreparingToSendOrchestration"/> class.
        /// </summary>
        /// <param name="getRecipientDataBatchesOrchestration">Get recipient data batches orchestration.</param>
        /// <param name="sendTriggersToAzureFunctionsOrchestration">Send triggers to Azure functions orchestration.</param>
        /// <param name="cleanUpActivity">Clean up activity.</param>
        public PreparingToSendOrchestration(
            GetRecipientDataBatchesOrchestration getRecipientDataBatchesOrchestration,
            SendTriggersToAzureFunctionsOrchestration sendTriggersToAzureFunctionsOrchestration,
            CleanUpActivity cleanUpActivity)
        {
            this.getRecipientDataBatchesOrchestration = getRecipientDataBatchesOrchestration;
            this.sendTriggersToAzureFunctionsOrchestration = sendTriggersToAzureFunctionsOrchestration;
            this.cleanUpActivity = cleanUpActivity;
        }

        /// <summary>
        /// This method starts the durable framework orchestration for preparing to send notifications.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(StartOrchestrationAsync))]
        public async Task StartOrchestrationAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var notificationDataEntity = context.GetInput<NotificationDataEntity>();

            try
            {
                var recipientDataBatches = await this.getRecipientDataBatchesOrchestration.RunAsync(
                    context,
                    notificationDataEntity);

                await this.sendTriggersToAzureFunctionsOrchestration.RunAsync(
                    context,
                    notificationDataEntity,
                    recipientDataBatches);
            }
            catch (Exception ex)
            {
                await this.cleanUpActivity.RunAsync(context, notificationDataEntity, ex);
            }
        }
    }
}