// <copyright file="GetRecipientDataBatchesOrchestration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get the recipient data batches for sending a notification.
    /// It's a durable framework sub-orchestration.
    /// </summary>
    public partial class GetRecipientDataBatchesOrchestration
    {
        private readonly GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity;
        private readonly GetRecipientDataListForRostersActivity getRecipientDataListForRostersActivity;
        private readonly GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity;
        private readonly ProcessRecipientDataListActivity processRecipientDataListActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRecipientDataBatchesOrchestration"/> class.
        /// </summary>
        /// <param name="getRecipientDataListForAllUsersActivity">Get all users recipient data batches activity.</param>
        /// <param name="getRecipientDataListForRostersActivity">Get rosters recipient data batches activity.</param>
        /// <param name="getRecipientDataListForTeamsActivity">Get teams recipient data batches activity.</param>
        /// <param name="processRecipientDataListActivity">Process recipient data list activity.</param>
        public GetRecipientDataBatchesOrchestration(
            GetRecipientDataListForAllUsersActivity getRecipientDataListForAllUsersActivity,
            GetRecipientDataListForRostersActivity getRecipientDataListForRostersActivity,
            GetRecipientDataListForTeamsActivity getRecipientDataListForTeamsActivity,
            ProcessRecipientDataListActivity processRecipientDataListActivity)
        {
            this.getRecipientDataListForAllUsersActivity = getRecipientDataListForAllUsersActivity;
            this.getRecipientDataListForRostersActivity = getRecipientDataListForRostersActivity;
            this.getRecipientDataListForTeamsActivity = getRecipientDataListForTeamsActivity;
            this.processRecipientDataListActivity = processRecipientDataListActivity;
        }

        /// <summary>
        /// Run the orchestration.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <returns>It returns recipient data list.</returns>
        public async Task<IEnumerable<IEnumerable<UserDataEntity>>> RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity)
        {
            var recipientDataBatches =
                await context.CallSubOrchestratorAsync<IEnumerable<IEnumerable<UserDataEntity>>>(
                    nameof(GetRecipientDataBatchesOrchestration.GetRecipientDataBatchesAsync),
                    notificationDataEntity);

            return recipientDataBatches;
        }

        /// <summary>
        /// Start the get recipient data batches sub-orchestration.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>Recipient data batches.</returns>
        [FunctionName(nameof(GetRecipientDataBatchesOrchestration.GetRecipientDataBatchesAsync))]
        public async Task<IEnumerable<IEnumerable<UserDataEntity>>> GetRecipientDataBatchesAsync(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var notificationDataEntity = context.GetInput<NotificationDataEntity>();

            IEnumerable<UserDataEntity> recipientDataList;
            if (notificationDataEntity.AllUsers)
            {
                recipientDataList = await this.getRecipientDataListForAllUsersActivity.RunAsync(context, notificationDataEntity);
                this.Log(context, log, notificationDataEntity.Id, "All users");
            }
            else if (notificationDataEntity.Rosters.Count() != 0)
            {
                recipientDataList = await this.getRecipientDataListForRostersActivity.RunAsync(context, notificationDataEntity);
                this.Log(context, log, notificationDataEntity.Id, "Rosters", recipientDataList);
            }
            else if (notificationDataEntity.Teams.Count() != 0)
            {
                recipientDataList = await this.getRecipientDataListForTeamsActivity.RunAsync(context, notificationDataEntity);
                this.Log(context, log, notificationDataEntity.Id, "General channels", recipientDataList);
            }
            else
            {
                this.Log(context, log, notificationDataEntity.Id, "No audience was selected");
                return null;
            }

            return await this.processRecipientDataListActivity.RunAsync(context, notificationDataEntity.Id, recipientDataList);
        }

        private void Log(
            DurableOrchestrationContext context,
            ILogger log,
            string notificationDataEntityId,
            string audienceOption)
        {
            if (context.IsReplaying)
            {
                return;
            }

            log.LogInformation(
                "Notification id:{0}. Audience option: {1}",
                notificationDataEntityId,
                audienceOption);
        }

        private void Log(
            DurableOrchestrationContext context,
            ILogger log,
            string notificationDataEntityId,
            string audienceOption,
            IEnumerable<UserDataEntity> recipientDataList)
        {
            if (context.IsReplaying)
            {
                return;
            }

            log.LogInformation(
                "Notification id:{0}. Audience option: {1}. Count: {2}",
                notificationDataEntityId,
                audienceOption,
                recipientDataList.Count());
        }
    }
}