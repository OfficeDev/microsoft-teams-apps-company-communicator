// <copyright file="DataStreamFacade.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Streams
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Model;

    /// <summary>
    /// facade to get the data stream.
    /// </summary>
    public class DataStreamFacade : IDataStreamFacade
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;
        private readonly TeamDataRepository teamDataRepository;
        private readonly IUsersService usersService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStreamFacade"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">the sent notification data repository.</param>
        /// <param name="teamDataRepository">the team data repository.</param>
        /// <param name="usersService">the users service.</param>
        public DataStreamFacade(
            SentNotificationDataRepository sentNotificationDataRepository,
            TeamDataRepository teamDataRepository,
            IUsersService usersService)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
            this.teamDataRepository = teamDataRepository;
            this.usersService = usersService;
        }

        /// <summary>
        /// get the users data streams.
        /// </summary>
        /// <param name="notificationId">the notification id.</param>
        /// <returns>the streams of user data.</returns>
        public async IAsyncEnumerable<IEnumerable<UserData>> GetUserDataStreamAsync(string notificationId)
        {
            var sentNotificationDataEntitiesStream = this.sentNotificationDataRepository.GetStreamsAsync(notificationId);
            await foreach (var sentNotifcations in sentNotificationDataEntitiesStream)
            {
                var userIds = sentNotifcations.Select(x => x.RowKey).ToList();
                var userIdsList = userIds.ToList();
                var userIdGroups = userIdsList.AsGroups();
                var users = await this.usersService.GetBatchByUserIds(userIdGroups);
                var userData = sentNotifcations.CreateUserData(users);
                yield return userData;
            }
        }

        /// <summary>
        /// get the team data streams.
        /// </summary>
        /// <param name="notificationId">the notification id.</param>
        /// <returns>the streams of team data.</returns>
        public async IAsyncEnumerable<IEnumerable<TeamData>> GetTeamDataStreamAsync(string notificationId)
        {
            var sentNotificationDataEntitiesStream = this.sentNotificationDataRepository.GetStreamsAsync(notificationId);
            await foreach (var sentNotificationDataEntities in sentNotificationDataEntitiesStream)
            {
                var teamDataList = new List<TeamData>();
                foreach (var sentNotificationDataEntity in sentNotificationDataEntities)
                {
                    var team = await this.teamDataRepository.GetAsync(TeamDataTableNames.TeamDataPartition, sentNotificationDataEntity.RowKey);
                    var teamData = new TeamData
                    {
                        Id = sentNotificationDataEntity.RowKey,
                        Name = team.Name,
                        DeliveryStatus = sentNotificationDataEntity.DeliveryStatus,
                        StatusReason = sentNotificationDataEntity.ErrorMessage.AddStatusCode(
                        sentNotificationDataEntity.StatusCode.ToString()),
                    };
                    teamDataList.Add(teamData);
                }

                yield return teamDataList;
            }
        }
    }
}
