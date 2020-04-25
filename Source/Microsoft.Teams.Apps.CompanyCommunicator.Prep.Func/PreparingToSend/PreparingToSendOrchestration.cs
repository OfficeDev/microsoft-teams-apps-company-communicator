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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
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
        private readonly SetNotificationIsPrepCompleteActivity setNotificationIsPrepCompleteActivity;
        private readonly SendDataAggregationMessageActivity sendDataAggregationMessageActivity;
        private readonly SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity;
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
        /// <param name="setNotificationIsPrepCompleteActivity">Set notification IsPrep complete activity.</param>
        /// <param name="sendDataAggregationMessageActivity">Send data aggregation message activity.</param>
        /// <param name="sendTriggersToSendFunctionActivity">Send triggers to send function sub-orchestration.</param>
        /// <param name="handleFailureActivity">Clean up activity.</param>
        public PreparingToSendOrchestration(
            GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity,
            GetTeamDataEntitiesByIdsActivity getTeamDataEntitiesByIdsActivity,
            GetRecipientDataListForRosterActivity getRecipientDataListForRosterActivity,
            GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity,
            ProcessRecipientDataListActivity processRecipientDataListActivity,
            CreateSendingNotificationActivity createSendingNotificationActivity,
            SetNotificationIsPrepCompleteActivity setNotificationIsPrepCompleteActivity,
            SendDataAggregationMessageActivity sendDataAggregationMessageActivity,
            SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity,
            HandleFailureActivity handleFailureActivity)
        {
            this.getRecipientDataListForAllUsersActivity = getRecipientDataListForAllUsersActivity;
            this.getTeamDataEntitiesByIdsActivity = getTeamDataEntitiesByIdsActivity;
            this.getRecipientDataListForRosterActivity = getRecipientDataListForRosterActivity;
            this.getRecipientDataListForTeamsActivity = getRecipientDataListForTeamsActivity;
            this.processRecipientDataListActivity = processRecipientDataListActivity;
            this.createSendingNotificationActivity = createSendingNotificationActivity;
            this.setNotificationIsPrepCompleteActivity = setNotificationIsPrepCompleteActivity;
            this.sendDataAggregationMessageActivity = sendDataAggregationMessageActivity;
            this.sendTriggersToSendFunctionActivity = sendTriggersToSendFunctionActivity;
            this.handleFailureActivity = handleFailureActivity;
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

            if (!context.IsReplaying)
            {
                log.LogInformation($"Start to prepare to send the notification {notificationDataEntity.Id}!");
            }

            try
            {
                if (!context.IsReplaying)
                {
                    log.LogInformation("Get recipient batches.");
                }

                var recipientDataBatches =
                    await this.GetRecipientDataBatchesAsync(context, notificationDataEntity, log);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Prepare adaptive card.");
                }

                await this.createSendingNotificationActivity.RunAsync(context, notificationDataEntity);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Mark the notification as no longer preparing.");
                }

                await this.setNotificationIsPrepCompleteActivity.RunAsync(context, notificationDataEntity.Id);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Send a data aggregation trigger queue message to the data queue for the data function to process.");
                }

                await this.sendDataAggregationMessageActivity.RunAsync(context, notificationDataEntity.Id);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Send triggers to the send function.");
                }

                await this.SendTriggersToSendFunctionAsync(context, notificationDataEntity.Id, recipientDataBatches, log);

                log.LogInformation($"\"PREPARE TO SEND\" IS DONE SUCCESSFULLY FOR NOTIFICATION {notificationDataEntity.Id}!");
            }
            catch (Exception ex)
            {
                await this.handleFailureActivity.RunAsync(context, notificationDataEntity, ex);
            }
        }

        /// <summary>
        /// Uses the incoming request to determine which type of recipient list to fetch.
        /// Triggers the correct function in order to fetch the recipient list and fill the
        /// corresponding sent notification data table/partition with unknown/initial statuses.
        /// It then triggers the function to read that entire sent notificaiton data table/partition
        /// in order to get all recipients and then break all those recipients down into batches in order
        /// to be added to the send queue.
        /// </summary>
        /// <param name="context">Orchestration context.</param>
        /// <param name="notificationDataEntity">A notification data entity.</param>
        /// <param name="log">The logging service.</param>
        /// <returns>The batches of recipients to be added to the send queue.</returns>
        private async Task<IEnumerable<IEnumerable<RecipientData>>> GetRecipientDataBatchesAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            ILogger log)
        {
            var recipientTypeForLogging = string.Empty;
            if (notificationDataEntity.AllUsers)
            {
                recipientTypeForLogging = "All users";
                await this.getRecipientDataListForAllUsersActivity.RunAsync(context, notificationDataEntity);
            }
            else if (notificationDataEntity.Rosters.Count() != 0)
            {
                recipientTypeForLogging = "Rosters";
                await this.GetRecipientDataListForRostersAsync(context, notificationDataEntity, log);
            }
            else if (notificationDataEntity.Teams.Count() != 0)
            {
                recipientTypeForLogging = "General channels";
                await this.getRecipientDataListForTeamsActivity.RunAsync(context, notificationDataEntity);
            }
            else
            {
                recipientTypeForLogging = "No recipient type was defined";
                this.Log(context, log, notificationDataEntity.Id, recipientTypeForLogging);

                throw new ArgumentException($"No valid audience selected for the notification, Id: {notificationDataEntity.Id}");
            }

            var recipientDataBatches = await this.processRecipientDataListActivity.RunAsync(context, notificationDataEntity.Id);

            this.Log(context, log, notificationDataEntity.Id, recipientTypeForLogging, recipientDataBatches.SelectMany(p => p));

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
        /// Sends triggers to the Azure send function.
        /// It uses Fan-out / Fan-in pattern to send batch triggers in parallel to the Azure send function.
        /// </summary>
        /// <param name="context">Orchestration context.</param>
        /// <param name="notificationDataEntityId">Notification data entity ID.</param>
        /// <param name="recipientDataBatches">Recipient data batches.</param>
        /// <param name="log">The logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendTriggersToSendFunctionAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            IEnumerable<IEnumerable<RecipientData>> recipientDataBatches,
            ILogger log)
        {
            var totalBatches = recipientDataBatches.Count();
            var processedBatches = 0;

            var tasks = new List<Task>();
            foreach (var batch in recipientDataBatches)
            {
                if (!context.IsReplaying)
                {
                    log.LogInformation($"{++processedBatches} / {totalBatches}");
                }

                var task = this.sendTriggersToSendFunctionActivity.RunAsync(
                    context,
                    notificationDataEntityId,
                    batch);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Log information if the context is not replaying.
        /// </summary>
        /// <param name="context">Orchestration context.</param>
        /// <param name="log">The logging service.</param>
        /// <param name="notificationDataEntityId">A notification data entity's ID.</param>
        /// <param name="recipientType">The recipient type.</param>
        /// <param name="recipientDataList">The recipient data list.</param>
        private void Log(
            DurableOrchestrationContext context,
            ILogger log,
            string notificationDataEntityId,
            string recipientType,
            IEnumerable<RecipientData> recipientDataList = null)
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
