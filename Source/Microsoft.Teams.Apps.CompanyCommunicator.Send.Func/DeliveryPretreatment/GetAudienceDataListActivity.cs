// <copyright file="GetAudienceDataListActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get audience data list activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class GetAudienceDataListActivity
    {
        private readonly MetadataProvider metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAudienceDataListActivity"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        public GetAudienceDataListActivity(MetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Get a notification's audience data list.
        /// </summary>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <param name="log">Logger instance.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetAudienceDataListAsync))]
        public async Task<IList<UserDataEntity>> GetAudienceDataListAsync(
            [ActivityTrigger] NotificationDataEntity draftNotificationEntity,
            ILogger log)
        {
            var deduplicatedReceiverEntities = new List<UserDataEntity>();

            if (draftNotificationEntity.AllUsers)
            {
                var usersUserDataEntityDictionary = await this.metadataProvider.GetUserDataDictionaryAsync();
                deduplicatedReceiverEntities.AddRange(usersUserDataEntityDictionary.Select(kvp => kvp.Value));
                this.Log(log, draftNotificationEntity.Id, "All users");
            }
            else if (draftNotificationEntity.Rosters.Count() != 0)
            {
                var rosterUserDataEntityDictionary = await this.metadataProvider.GetTeamsRostersAsync(draftNotificationEntity.Rosters);
                deduplicatedReceiverEntities.AddRange(rosterUserDataEntityDictionary.Select(kvp => kvp.Value));
                this.Log(log, draftNotificationEntity.Id, "Rosters", deduplicatedReceiverEntities.Count);
            }
            else if (draftNotificationEntity.Teams.Count() != 0)
            {
                var teamsReceiverEntities = await this.metadataProvider.GetTeamsReceiverEntities(draftNotificationEntity.Teams);
                deduplicatedReceiverEntities.AddRange(teamsReceiverEntities);
                this.Log(log, draftNotificationEntity.Id, "General channels", deduplicatedReceiverEntities.Count);
            }
            else
            {
                this.Log(log, draftNotificationEntity.Id, "No audience was selected");
            }

            return deduplicatedReceiverEntities;
        }

        private void Log(ILogger log, string draftNotificationEntityId, string audienceOption)
        {
            log.LogInformation(
                "Notification id:{0}. Audience option: {1}",
                draftNotificationEntityId,
                audienceOption);
        }

        private void Log(ILogger log, string draftNotificationEntityId, string audienceOption, int count)
        {
            log.LogInformation(
                "Notification id:{0}. Audience option: {1}. Count: {2}",
                draftNotificationEntityId,
                audienceOption,
                count);
        }
    }
}