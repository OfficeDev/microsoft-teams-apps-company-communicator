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
        private readonly ProcessRecipientDataListForRosterActivity processRecipientDataListForRosterActivity;
        private readonly GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity;
        private readonly CreateSendingNotificationActivity createSendingNotificationActivity;
        private readonly SetNotificationMetadataActivity setNotificationMetadataActivity;
        private readonly SendDataAggregationMessageActivity sendDataAggregationMessageActivity;
        private readonly SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity;
        private readonly HandleFailureActivity handleFailureActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreparingToSendOrchestration"/> class.
        /// </summary>
        /// <param name="getRecipientDataListForAllUsersActivity">Get recipient data for all users activity.</param>
        /// <param name="getTeamDataEntitiesByIdsActivity">Get team data entities by ids activity.</param>
        /// <param name="getRecipientDataListForRosterActivity">Get recipient data for roster activity.</param>
        /// <param name="processRecipientDataListForRosterActivity">Process recipient data list for roster activity.</param>
        /// <param name="getRecipientDataListForTeamsActivity">Get recipient data for teams activity.</param>
        /// <param name="createSendingNotificationActivity">Create sending notification activity.</param>
        /// <param name="setNotificationMetadataActivity">Set notification metadata activity.</param>
        /// <param name="sendDataAggregationMessageActivity">Send data aggregation message activity.</param>
        /// <param name="sendTriggersToSendFunctionActivity">Send triggers to send function sub-orchestration.</param>
        /// <param name="handleFailureActivity">Clean up activity.</param>
        public PreparingToSendOrchestration(
            GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity,
            GetTeamDataEntitiesByIdsActivity getTeamDataEntitiesByIdsActivity,
            GetRecipientDataListForRosterActivity getRecipientDataListForRosterActivity,
            ProcessRecipientDataListForRosterActivity processRecipientDataListForRosterActivity,
            GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity,
            CreateSendingNotificationActivity createSendingNotificationActivity,
            SetNotificationMetadataActivity setNotificationMetadataActivity,
            SendDataAggregationMessageActivity sendDataAggregationMessageActivity,
            SendTriggersToSendFunctionActivity sendTriggersToSendFunctionActivity,
            HandleFailureActivity handleFailureActivity)
        {
            this.getRecipientDataListForAllUsersActivity = getRecipientDataListForAllUsersActivity;
            this.getTeamDataEntitiesByIdsActivity = getTeamDataEntitiesByIdsActivity;
            this.getRecipientDataListForRosterActivity = getRecipientDataListForRosterActivity;
            this.processRecipientDataListForRosterActivity = processRecipientDataListForRosterActivity;
            this.getRecipientDataListForTeamsActivity = getRecipientDataListForTeamsActivity;
            this.createSendingNotificationActivity = createSendingNotificationActivity;
            this.setNotificationMetadataActivity = setNotificationMetadataActivity;
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
                    log.LogInformation("Get recipient data list and batches.");
                }

                var recipientDataListInformation =
                    await this.GetRecipientDataBatchesAsync(context, notificationDataEntity, log);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Prepare adaptive card.");
                }

                await this.createSendingNotificationActivity.RunAsync(context, notificationDataEntity);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Mark the notification as no longer preparing and with the correct total recipient count.");
                }

                await this.setNotificationMetadataActivity.RunAsync(
                    context,
                    notificationDataEntity.Id,
                    recipientDataListInformation.TotalNumberOfRecipients);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Send a data aggregation trigger queue message to the data queue for the data function to process.");
                }

                await this.sendDataAggregationMessageActivity.RunAsync(context, notificationDataEntity.Id);

                if (!context.IsReplaying)
                {
                    log.LogInformation("Send triggers to the Send queue for the Send function.");
                }

                await this.SendTriggersToSendFunctionAsync(context, notificationDataEntity.Id, recipientDataListInformation, log);

                log.LogInformation($"\"PREPARE TO SEND\" IS DONE SUCCESSFULLY FOR NOTIFICATION {notificationDataEntity.Id}!");
            }
            catch (Exception ex)
            {
                await this.handleFailureActivity.RunAsync(context, notificationDataEntity, ex);
            }
        }

        /// <summary>
        /// It uses the incoming request to determine which type of recipient list to fetch
        /// and initialize.
        /// It triggers the correct functions in order to fetch the recipient
        /// list and fill the corresponding sent notification data table/partition with
        /// unknown/initial statuses.
        /// It then breaks all those recipients down into batches and loads them into
        /// the send batches data table to be added to the send queue.
        /// </summary>
        /// <param name="context">Orchestration context.</param>
        /// <param name="notificationDataEntity">A notification data entity.</param>
        /// <param name="log">The logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<RecipientDataListInformation> GetRecipientDataBatchesAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            ILogger log)
        {
            var recipientTypeForLogging = string.Empty;
            var recipientDataListInformation = new RecipientDataListInformation();
            if (notificationDataEntity.AllUsers)
            {
                recipientTypeForLogging = "All users";
                recipientDataListInformation = await this.getRecipientDataListForAllUsersActivity.RunAsync(context, notificationDataEntity);
            }
            else if (notificationDataEntity.Rosters.Count() != 0)
            {
                recipientTypeForLogging = "Rosters";
                await this.GetRecipientDataListForRostersAsync(context, notificationDataEntity, log);
                recipientDataListInformation = await this.processRecipientDataListForRosterActivity.RunAsync(context, notificationDataEntity.Id);
            }
            else if (notificationDataEntity.Teams.Count() != 0)
            {
                recipientTypeForLogging = "General channels";
                recipientDataListInformation = await this.getRecipientDataListForTeamsActivity.RunAsync(context, notificationDataEntity);
            }
            else
            {
                recipientTypeForLogging = "No recipient type was defined";
                this.Log(context, log, notificationDataEntity.Id, recipientTypeForLogging, recipientDataListInformation);

                throw new ArgumentException($"No valid audience selected for the notification, Id: {notificationDataEntity.Id}");
            }

            this.Log(context, log, notificationDataEntity.Id, recipientTypeForLogging, recipientDataListInformation);

            return recipientDataListInformation;
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
        /// <param name="recipientDataListInformation">The information about the recipient data list.</param>
        /// <param name="log">The logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendTriggersToSendFunctionAsync(
            DurableOrchestrationContext context,
            string notificationDataEntityId,
            RecipientDataListInformation recipientDataListInformation,
            ILogger log)
        {
            var numberOfRecipientDataBatches = recipientDataListInformation.NumberOfRecipientDataBatches;

            var tasks = new List<Task>();
            for (var batchIndex = 1; batchIndex <= numberOfRecipientDataBatches; batchIndex++)
            {
                if (!context.IsReplaying)
                {
                    log.LogInformation($"Processing batch {batchIndex} / {numberOfRecipientDataBatches}");
                }

                var task = this.sendTriggersToSendFunctionActivity.RunAsync(
                    context,
                    notificationDataEntityId,
                    batchIndex);

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
        /// <param name="recipientDataListInformation">The information for the recipient data list.</param>
        private void Log(
            DurableOrchestrationContext context,
            ILogger log,
            string notificationDataEntityId,
            string recipientType,
            RecipientDataListInformation recipientDataListInformation)
        {
            if (context.IsReplaying)
            {
                return;
            }

            var numberOfRecipients = recipientDataListInformation.TotalNumberOfRecipients;
            var numberOfRecipientBatches = recipientDataListInformation.NumberOfRecipientDataBatches;

            var message = $"Notification id:{notificationDataEntityId}. Recipient option: {recipientType}. Number of recipients: {numberOfRecipients}. Number of recipient data batches: {numberOfRecipientBatches}.";
            log.LogInformation(message);
        }
    }
}
