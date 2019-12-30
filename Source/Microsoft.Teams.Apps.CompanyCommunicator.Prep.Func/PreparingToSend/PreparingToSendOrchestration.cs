// <copyright file="PreparingToSendOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.SendTriggersToAzureFunctions;

    /// <summary>
    /// This class is the durable framework orchestration for preparing to send notifications.
    /// </summary>
    public class PreparingToSendOrchestration
    {
        private readonly GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity;
        private readonly GetTeamDataEntitiesByIdsActivity getTeamDataEntitiesByIdsActivity;
        private readonly GetRecipientDataListForRosterActivity getRecipientDataListForRosterActivity;
        private readonly GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity;
        private readonly ProcessRecipientDataListActivity processRecipientDataListActivity;
        private readonly CreateSendingNotificationActivity createSendingNotificationActivity;
        private readonly SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity;
        private readonly SendTriggerToDataFunctionActivity sendTriggerToDataFunctionActivity;
        private readonly HandleFailureActivity handleFailureActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreparingToSendOrchestration"/> class.
        /// </summary>
        /// <param name="getRecipientDataListForAllUsersActivity">Get recipient data for all users activity.</param>
        /// <param name="getTeamDataEntitiesByIdsActivity">Get team data entities by ids activity.</param>
        /// <param name="getRecipientDataListForRosterActivity">Get recipient data for roster activity.</param>
        /// <param name="getRecipientDataListForTeamsActivity">Get recipient data for teams activity.</param>
        /// <param name="processRecipientDataListActivity">Process recipient data list activity.</param>
        /// <param name="createSendingNotificationActivity">Create sending notification activity.</param>
        /// <param name="sendTriggersToSendFunctionActivity">Send triggers to send function sub-orchestration.</param>
        /// <param name="sendTriggerToDataFunctionActivity">Send trigger to data function activity.</param>
        /// <param name="handleFailureActivity">Clean up activity.</param>
        public PreparingToSendOrchestration(
            GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity,
            GetTeamDataEntitiesByIdsActivity getTeamDataEntitiesByIdsActivity,
            GetRecipientDataListForRosterActivity getRecipientDataListForRosterActivity,
            GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity,
            ProcessRecipientDataListActivity processRecipientDataListActivity,
            CreateSendingNotificationActivity createSendingNotificationActivity,
            SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity,
            SendTriggerToDataFunctionActivity sendTriggerToDataFunctionActivity,
            HandleFailureActivity handleFailureActivity)
        {
            this.getRecipientDataListForAllUsersActivity = getRecipientDataListForAllUsersActivity;
            this.getTeamDataEntitiesByIdsActivity = getTeamDataEntitiesByIdsActivity;
            this.getRecipientDataListForRosterActivity = getRecipientDataListForRosterActivity;
            this.getRecipientDataListForTeamsActivity = getRecipientDataListForTeamsActivity;
            this.processRecipientDataListActivity = processRecipientDataListActivity;
            this.createSendingNotificationActivity = createSendingNotificationActivity;
            this.sendTriggersToSendFunctionActivity = sendTriggersToSendFunctionActivity;
            this.sendTriggerToDataFunctionActivity = sendTriggerToDataFunctionActivity;
            this.handleFailureActivity = handleFailureActivity;
        }

        /// <summary>
        /// This is the durable orchestration method,
        /// which kicks of the preparing to send process.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="logger">Logging service.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        [FunctionName(nameof(PrepareToSendOrchestrationAsync))]
        public async Task PrepareToSendOrchestrationAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger logger)
        {
            var notificationDataEntity = context.GetInput<NotificationDataEntity>();

            if (!context.IsReplaying)
            {
                logger.LogCritical($"Start to prepare to send the notification {notificationDataEntity.Id}!");
            }

            try
            {
                if (!context.IsReplaying)
                {
                    logger.LogCritical("Get recipient batches.");
                }

                var recipientDataBatches =
                    await this.GetRecipientDataBatchesAsync(context, notificationDataEntity, logger);

                if (!context.IsReplaying)
                {
                    logger.LogCritical("Prepare adaptive card.");
                }

                await this.createSendingNotificationActivity.RunAsync(context, notificationDataEntity);

                if (!context.IsReplaying)
                {
                    logger.LogCritical("Send triggers to the send function.");
                }

                await this.SendTriggersToSendFunctionAsync(context, notificationDataEntity.Id, recipientDataBatches, logger);

                if (!context.IsReplaying)
                {
                    logger.LogCritical("Send triggers to the data function.");
                }

                if (!context.IsReplaying)
                {
                    await this.sendTriggerToDataFunctionActivity.RunAsync(context, notificationDataEntity.Id, recipientDataBatches);
                }

                logger.LogCritical($"\"PREPARE TO SEND\" IS DONE SUCCESSFULLY FOR NOTIFICATION {notificationDataEntity.Id}!");
            }
            catch (Exception ex)
            {
                await this.handleFailureActivity.RunAsync(context, notificationDataEntity, ex);
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
                await this.GetRecipientDataListForRostersAsync(context, notificationDataEntity, log);
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

        /// <summary>
        /// Get recipient data list for rosters.
        /// It uses Fan-out / Fan-in pattern to get recipient data list (team rosters) in parallel.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task GetRecipientDataListForRostersAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            ILogger log)
        {
            var teamDataEntityList =
                await this.getTeamDataEntitiesByIdsActivity.RunAsync(context, notificationDataEntity);

            var tasks = new List<Task>();
            foreach (var teamDataEntity in teamDataEntityList)
            {
                var task = this.getRecipientDataListForRosterActivity.RunAsync(
                    context,
                    notificationDataEntity.Id,
                    teamDataEntity,
                    log);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Send triggers to Azure send function.
        /// It uses Fan-out / Fan-in pattern to send batch triggers in parallel to Azure send function.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendTriggersToSendFunctionAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            IEnumerable<IEnumerable<UserDataEntity>> recipientDataBatches,
            ILogger logger)
        {
            var totalBatches = recipientDataBatches.Count();
            var processedBatches = 0;

            var tasks = new List<Task>();
            foreach (var batch in recipientDataBatches)
            {
                if (!context.IsReplaying)
                {
                    logger.LogCritical($"{++processedBatches} / {totalBatches}");
                }

                var task = this.sendTriggersToSendFunctionActivity.RunAsync(
                    context,
                    notificationDataEntityId,
                    batch);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private void Log(
            DurableOrchestrationContext context,
            ILogger log,
            string notificationDataEntityId,
            string recipientType,
            IEnumerable<UserDataEntity> recipientDataList = null)
        {
            if (context.IsReplaying)
            {
                return;
            }

            var countMessage = recipientDataList != null ? $"Count: {recipientDataList.Count()}" : string.Empty;
            var message = $"Notification id:{notificationDataEntityId}. Recipient option: {recipientType}. {countMessage}";
            log.LogInformation(message);
        }
    }
}