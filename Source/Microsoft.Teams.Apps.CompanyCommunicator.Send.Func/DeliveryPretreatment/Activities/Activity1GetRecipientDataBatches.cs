// <copyright file="Activity1GetRecipientDataBatches.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get the message batches to be sent to Azure service bus queue activity.
    /// It's used by the durable function framework.
    /// </summary>
    public partial class Activity1GetRecipientDataBatches
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity1GetRecipientDataBatches"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        public Activity1GetRecipientDataBatches(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<List<UserDataEntity>>> RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity notificationDataEntity,
            ILogger log)
        {
            List<List<UserDataEntity>> recipientDataBatches = null;

            if (notificationDataEntity.AllUsers)
            {
                recipientDataBatches = await this.GetAllUsersRecipientDataBatchesAsync(context, notificationDataEntity);
                this.Log(context, log, notificationDataEntity.Id, "All users");
            }
            else if (notificationDataEntity.Rosters.Count() != 0)
            {
                recipientDataBatches = await this.GetRosterRecipeintDataBatchesAsync(context, notificationDataEntity);
                this.Log(context, log, notificationDataEntity.Id, "Rosters", recipientDataBatches);
            }
            else if (notificationDataEntity.Teams.Count() != 0)
            {
                recipientDataBatches = await this.GetTeamRecipientDataBatchesAsync(context, notificationDataEntity);
                this.Log(context, log, notificationDataEntity.Id, "General channels", recipientDataBatches);
            }
            else
            {
                this.Log(context, log, notificationDataEntity.Id, "No audience was selected");
            }

            return recipientDataBatches;
        }

        /// <summary>
        /// Initialize sent notification data entities in Azure table storage.
        /// </summary>
        /// <param name="input">Input value.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(nameof(InitializeSentNotificationDataAsync))]
        public async Task InitializeSentNotificationDataAsync(
            [ActivityTrigger] Activity1GetRecipientDataBatchesDTO input)
        {
            await this.metadataProvider.InitializeSentNotificationDataAsync(
                input.NotificationDataEntityId,
                input.RecipientDataBatches);
        }

        private List<List<UserDataEntity>> CreateRecipientDataBatches(
            List<UserDataEntity> recipientDataList)
        {
            var recipientDataBatches = new List<List<UserDataEntity>>();

            var totalNumberOfRecipients = recipientDataList.Count;
            var batchSize = 100;
            var numberOfCompleteBatches = totalNumberOfRecipients / batchSize;
            var numberRecipientsInIncompleteBatch = totalNumberOfRecipients % batchSize;

            for (var i = 0; i < numberOfCompleteBatches; i++)
            {
                var startingIndex = i * batchSize;
                var batch = recipientDataList.GetRange(startingIndex, batchSize);
                recipientDataBatches.Add(batch);
            }

            if (numberRecipientsInIncompleteBatch != 0)
            {
                var incompleteBatchStartingIndex = numberOfCompleteBatches * batchSize;
                var incompleteBatch = recipientDataList.GetRange(
                    incompleteBatchStartingIndex,
                    numberRecipientsInIncompleteBatch);
                recipientDataBatches.Add(incompleteBatch);
            }

            return recipientDataBatches;
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
            List<List<UserDataEntity>> recipientDataBatches)
        {
            if (context.IsReplaying)
            {
                return;
            }

            log.LogInformation(
                "Notification id:{0}. Audience option: {1}. Count: {2}",
                notificationDataEntityId,
                audienceOption,
                recipientDataBatches.SelectMany(p => p).Count());
        }
    }
}