// <copyright file="GetMetaDataActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;

    /// <summary>
    /// Activity to create the metadata.
    /// </summary>
    public class GetMetaDataActivity
    {
        private readonly IUsersService usersService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMetaDataActivity"/> class.
        /// </summary>
        /// <param name="usersService">the users service.</param>
        public GetMetaDataActivity(IUsersService usersService)
        {
            this.usersService = usersService;
        }

        /// <summary>
        /// Run the activity.
        /// It creates and gets the metadata.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="exportRequiredData">Tuple containing notification data entity and export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>instance of metadata.</returns>
        public async Task<MetaData> RunAsync(
            IDurableOrchestrationContext context,
            (NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity) exportRequiredData,
            ILogger log)
        {
            var metaData = await context.CallActivityWithRetryAsync<MetaData>(
               nameof(GetMetaDataActivity.GetMetaDataActivityAsync),
               ActivitySettings.CommonActivityRetryOptions,
               (exportRequiredData.notificationDataEntity, exportRequiredData.exportDataEntity));
            return metaData;
        }

        /// <summary>
        /// Create and get the metadata.
        /// </summary>
        /// <param name="exportRequiredData">Tuple containing notification data entity and export data entity.</param>
        /// <returns>instance of metadata.</returns>
        [FunctionName(nameof(GetMetaDataActivityAsync))]
        public async Task<MetaData> GetMetaDataActivityAsync(
            [ActivityTrigger](
            NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity) exportRequiredData)
        {
            var user = await this.usersService.GetUserAsync(exportRequiredData.exportDataEntity.PartitionKey);
            var userPrincipalName = (user != null) ?
                user.UserPrincipalName :
                Common.Constants.AdminConsentError;

            return this.Get(
                exportRequiredData.notificationDataEntity,
                exportRequiredData.exportDataEntity,
                userPrincipalName);
        }

        private MetaData Get(
            NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity,
            string userPrinicipalName)
        {
            var metadata = new MetaData
            {
                MessageTitle = notificationDataEntity.Title,
                SentTimeStamp = notificationDataEntity.SentDate,
                ExportedBy = userPrinicipalName,
                ExportTimeStamp = exportDataEntity.SentDate,
            };
            return metadata;
        }
    }
}