// <copyright file="PreparingToSendOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.SendTriggersToAzureFunctions;

    /// <summary>
    /// This class is the durable framework orchestration for preparing to send notifications.
    /// </summary>
    public class PreparingToSendOrchestration
    {
        private readonly GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity;
        private readonly GetRecipientDataListForRostersActivity getRecipientDataListForRostersActivity;
        private readonly GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity;
        private readonly ProcessRecipientDataListActivity processRecipientDataListActivity;
        private readonly CreateSendingNotificationActivity createSendingNotificationActivity;
        private readonly SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity;
        private readonly SendTriggerToDataFunctionActivity sendTriggerToDataFunctionActivity;
        private readonly CleanUpActivity cleanUpActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreparingToSendOrchestration"/> class.
        /// </summary>
        /// <param name="getRecipientDataListForAllUsersActivity">Get all users recipient data batches activity.</param>
        /// <param name="getRecipientDataListForRostersActivity">Get rosters recipient data batches activity.</param>
        /// <param name="getRecipientDataListForTeamsActivity">Get teams recipient data batches activity.</param>
        /// <param name="processRecipientDataListActivity">Process recipient data list activity.</param>
        /// <param name="createSendingNotificationActivity">Create sending notification activity.</param>
        /// <param name="sendTriggersToSendFunctionActivity">Send triggers to send function activity.</param>
        /// <param name="sendTriggerToDataFunctionActivity">Send trigger to data function activity.</param>
        /// <param name="cleanUpActivity">Clean up activity.</param>
        public PreparingToSendOrchestration(
            GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity,
            GetRecipientDataListForRostersActivity getRecipientDataListForRostersActivity,
            GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity,
            ProcessRecipientDataListActivity processRecipientDataListActivity,
            CreateSendingNotificationActivity createSendingNotificationActivity,
            SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity,
            SendTriggerToDataFunctionActivity sendTriggerToDataFunctionActivity,
            CleanUpActivity cleanUpActivity)
        {
            this.getRecipientDataListForAllUsersActivity = getRecipientDataListForAllUsersActivity;
            this.getRecipientDataListForRostersActivity = getRecipientDataListForRostersActivity;
            this.getRecipientDataListForTeamsActivity = getRecipientDataListForTeamsActivity;
            this.processRecipientDataListActivity = processRecipientDataListActivity;
            this.createSendingNotificationActivity = createSendingNotificationActivity;
            this.sendTriggersToSendFunctionActivity = sendTriggersToSendFunctionActivity;
            this.sendTriggerToDataFunctionActivity = sendTriggerToDataFunctionActivity;
            this.cleanUpActivity = cleanUpActivity;
        }

        /// <summary>
        /// This is the durable orchestration method,
        /// which kicks of the preparing to send process.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(PrepareToSendOrchestrationAsync))]
        public async Task PrepareToSendOrchestrationAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var notificationDataEntity = context.GetInput<NotificationDataEntity>();

            try
            {
                var recipientDataBatches = await this.GetRecipientDataBatchesAsync(
                    context,
                    notificationDataEntity,
                    log);

                await this.createSendingNotificationActivity.RunAsync(context, notificationDataEntity);

                await this.sendTriggersToSendFunctionActivity.RunAsync(
                    context,
                    recipientDataBatches,
                    notificationDataEntity.Id);

                await this.sendTriggerToDataFunctionActivity.RunAsync(
                    context,
                    notificationDataEntity.Id,
                    recipientDataBatches);

                log.LogInformation($"\"PREPARE TO SEND\" IS DONE SUCCESSFULLY FOR NOTIFICATION {notificationDataEntity.Id}!");
            }
            catch (Exception ex)
            {
                await this.cleanUpActivity.RunAsync(context, notificationDataEntity, ex);
            }
        }

        private async Task<IEnumerable<IEnumerable<UserDataEntity>>> GetRecipientDataBatchesAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            ILogger log)
        {
            var recipientType = string.Empty;
            if (notificationDataEntity.AllUsers)
            {
                recipientType = "All users";
                await this.getRecipientDataListForAllUsersActivity.RunAsync(context, notificationDataEntity);
            }
            else if (notificationDataEntity.Rosters.Count() != 0)
            {
                recipientType = "Rosters";
                await this.getRecipientDataListForRostersActivity.RunAsync(context, notificationDataEntity);
            }
            else if (notificationDataEntity.Teams.Count() != 0)
            {
                recipientType = "General channels";
                await this.getRecipientDataListForTeamsActivity.RunAsync(context, notificationDataEntity);
            }
            else
            {
                recipientType = "No recipient type was defined";
                this.Log(context, log, notificationDataEntity.Id, recipientType);
                return null;
            }

            var recipientDataBatches = await this.processRecipientDataListActivity.RunAsync(context, notificationDataEntity.Id);

            this.Log(context, log, notificationDataEntity.Id, recipientType, recipientDataBatches.SelectMany(p => p));

            return recipientDataBatches;
        }

        private void Log(
            DurableOrchestrationContext context,
            ILogger log,
            string notificationDataEntityId,
            string recipientType)
        {
            if (context.IsReplaying)
            {
                return;
            }

            log.LogInformation(
                "Notification id:{0}. Audience option: {1}",
                notificationDataEntityId,
                recipientType);
        }

        private void Log(
            DurableOrchestrationContext context,
            ILogger log,
            string notificationDataEntityId,
            string recipientType,
            IEnumerable<UserDataEntity> recipientDataList)
        {
            if (context.IsReplaying)
            {
                return;
            }

            log.LogInformation(
                "Notification id:{0}. Audience option: {1}. Count: {2}",
                notificationDataEntityId,
                recipientType,
                recipientDataList.Count());
        }
    }
}