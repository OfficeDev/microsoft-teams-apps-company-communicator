// <copyright file="ProcessRecipientsOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Orchestrators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches;

    /// <summary>
    /// Processes recipients from the notification data entity.
    /// Steps:
    /// 1. Fetch and store recipient data. (example - all members in a group or all users in a tenant)
    /// 2. Batch the recipients for processing.
    /// </summary>
    public class ProcessRecipientsOrchestrator
    {
        private readonly GetTeamDataEntitiesByIdsActivity getTeamDataEntitiesByIdsActivity;
        private readonly GetRecipientDataListForRosterActivity getRecipientDataListForRosterActivity;
        private readonly ProcessRecipientDataListActivity processRecipientDataListActivity;
        private readonly GetRecipientDataListForGroupActivity getRecipientDataListForGroupActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessRecipientsOrchestrator"/> class.
        /// </summary>
        /// <param name="getTeamDataEntitiesByIdsActivity">Get team data entities by ids activity.</param>
        /// <param name="getRecipientDataListForRosterActivity">Get recipient data for roster activity.</param>
        /// <param name="getRecipientDataListForGroupActivity">Get recipient data for group activity.</param>
        /// <param name="processRecipientDataListActivity">Process recipient data list for roster activity.</param>
        public ProcessRecipientsOrchestrator(
            GetTeamDataEntitiesByIdsActivity getTeamDataEntitiesByIdsActivity,
            GetRecipientDataListForRosterActivity getRecipientDataListForRosterActivity,
            ProcessRecipientDataListActivity processRecipientDataListActivity,
            GetRecipientDataListForGroupActivity getRecipientDataListForGroupActivity)
        {
            this.getTeamDataEntitiesByIdsActivity = getTeamDataEntitiesByIdsActivity;
            this.getRecipientDataListForRosterActivity = getRecipientDataListForRosterActivity;
            this.processRecipientDataListActivity = processRecipientDataListActivity;
            this.getRecipientDataListForGroupActivity = getRecipientDataListForGroupActivity;
        }

        /// <summary>
        /// Fetch recipients and store them in Azure storage.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>Recipient Data.</returns>
        [FunctionName(FunctionNames.ProcessRecipientsOrchestrator)]
        public async Task<RecipientDataListInformation> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var notificationDataEntity = context.GetInput<NotificationDataEntity>();

            var recipientDataListInformation = new RecipientDataListInformation();
            var recipientTypeForLogging = string.Empty;
            if (notificationDataEntity.AllUsers)
            {
                recipientTypeForLogging = "All users";
                var userDataEntities = await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                    nameof(GetAllUsersDataEntitiesActivity.GetAllUsersAsync),
                    FunctionSettings.DefaultRetryOptions,
                    notificationDataEntity.Id);

                recipientDataListInformation = await context.CallActivityWithRetryAsync<RecipientDataListInformation>(
                    nameof(GetRecipientDataListForAllUsersActivity.GetAllUsersRecipientDataListAsync),
                    FunctionSettings.DefaultRetryOptions,
                    (notificationDataEntity.Id, userDataEntities));
            }
            else if (notificationDataEntity.Rosters.Any())
            {
                recipientTypeForLogging = "Rosters";
                var teamDataEntityList =
                await this.getTeamDataEntitiesByIdsActivity.RunAsync(context, notificationDataEntity.Id, notificationDataEntity.Rosters);

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

                recipientDataListInformation = await this.processRecipientDataListActivity.RunAsync(context, notificationDataEntity.Id);
            }
            else if (notificationDataEntity.Groups.Count() != 0)
            {
                recipientTypeForLogging = "Groups";
                var tasks = new List<Task>();
                foreach (var groupId in notificationDataEntity.Groups)
                {
                    var task = this.getRecipientDataListForGroupActivity.RunAsync(
                        context,
                        notificationDataEntity.Id,
                        groupId,
                        log);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                recipientDataListInformation = await this.processRecipientDataListActivity.RunAsync(context, notificationDataEntity.Id);
            }
            else if (notificationDataEntity.Teams.Any())
            {
                recipientTypeForLogging = "General channels";
                var teamDataEntities = await this.getTeamDataEntitiesByIdsActivity.RunAsync(context, notificationDataEntity.Id, notificationDataEntity.Teams);

                recipientDataListInformation = await context.CallActivityWithRetryAsync<RecipientDataListInformation>(
                    FunctionNames.GetTeamRecipientDataListActivity,
                    FunctionSettings.DefaultRetryOptions,
                    (notificationDataEntity.Id, teamDataEntities));
            }
            else
            {
                recipientTypeForLogging = "No recipient type was defined";
                Log(context, log, notificationDataEntity.Id, recipientTypeForLogging, recipientDataListInformation);

                throw new ArgumentException($"No valid audience selected for the notification, Id: {notificationDataEntity.Id}");
            }

            Log(context, log, notificationDataEntity.Id, recipientTypeForLogging, recipientDataListInformation);

            return recipientDataListInformation;
        }

        /// <summary>
        /// Log information if the context is not replaying.
        /// </summary>
        /// <param name="context">Orchestration context.</param>
        /// <param name="log">The logging service.</param>
        /// <param name="notificationDataEntityId">A notification data entity's ID.</param>
        /// <param name="recipientType">The recipient type.</param>
        /// <param name="recipientDataListInformation">The information for the recipient data list.</param>
        private static void Log(
            IDurableOrchestrationContext context,
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