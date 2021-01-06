// <copyright file="GetMetadataActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend;

    /// <summary>
    /// Activity to create the metadata.
    /// </summary>
    public class GetMetadataActivity
    {
        private readonly IUsersService usersService;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMetadataActivity"/> class.
        /// </summary>
        /// <param name="usersService">the users service.</param>
        /// <param name="localizer">Localization service.</param>
        public GetMetadataActivity(
            IUsersService usersService,
            IStringLocalizer<Strings> localizer)
        {
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Run the activity.
        /// It creates and gets the metadata.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="exportRequiredData">Tuple containing notification data entity and export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>instance of metadata.</returns>
        public async Task<Metadata> RunAsync(
            IDurableOrchestrationContext context,
            (NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity) exportRequiredData,
            ILogger log)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (exportRequiredData.notificationDataEntity == null)
            {
                throw new ArgumentNullException(nameof(exportRequiredData.notificationDataEntity));
            }

            if (exportRequiredData.exportDataEntity == null)
            {
                throw new ArgumentNullException(nameof(exportRequiredData.exportDataEntity));
            }

            var metaData = await context.CallActivityWithRetryAsync<Metadata>(
               nameof(GetMetadataActivity.GetMetadataActivityAsync),
               FunctionSettings.DefaultRetryOptions,
               (exportRequiredData.notificationDataEntity, exportRequiredData.exportDataEntity));
            return metaData;
        }

        /// <summary>
        /// Create and get the metadata.
        /// </summary>
        /// <param name="exportRequiredData">Tuple containing notification data entity and export data entity.</param>
        /// <returns>instance of metadata.</returns>
        [FunctionName(nameof(GetMetadataActivityAsync))]
        public async Task<Metadata> GetMetadataActivityAsync(
            [ActivityTrigger](NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity) exportRequiredData)
        {
            if (exportRequiredData.notificationDataEntity == null)
            {
                throw new ArgumentNullException(nameof(exportRequiredData.notificationDataEntity));
            }

            if (exportRequiredData.exportDataEntity == null)
            {
                throw new ArgumentNullException(nameof(exportRequiredData.exportDataEntity));
            }

            User user = default;
            try
            {
                user = await this.usersService.GetUserAsync(exportRequiredData.exportDataEntity.PartitionKey);
            }
            catch (ServiceException serviceException)
            {
                if (serviceException.StatusCode != HttpStatusCode.Forbidden)
                {
                    throw serviceException;
                }
            }

            var userPrincipalName = (user != null) ?
                user.UserPrincipalName :
                this.localizer.GetString("AdminConsentError");

            return this.Get(
                exportRequiredData.notificationDataEntity,
                exportRequiredData.exportDataEntity,
                userPrincipalName);
        }

        private Metadata Get(
            NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity,
            string userPrinicipalName)
        {
            var metadata = new Metadata
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