// <copyright file="DataStreamFacade.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Streams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Localization;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model;
    using Newtonsoft.Json;

    /// <summary>
    /// Facade to get the data stream.
    /// </summary>
    public class DataStreamFacade : IDataStreamFacade
    {
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly ITeamDataRepository teamDataRepository;
        private readonly IUserDataRepository userDataRepository;
        private readonly IUserTypeService userTypeService;
        private readonly IUsersService usersService;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStreamFacade"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">the sent notification data repository.</param>
        /// <param name="teamDataRepository">the team data repository.</param>
        /// <param name="userDataRepository">the user data repository.</param>
        /// <param name="userTypeService">the user type service.</param>
        /// <param name="usersService">the users service.</param>
        /// <param name="localizer">Localization service.</param>
        public DataStreamFacade(
            ISentNotificationDataRepository sentNotificationDataRepository,
            ITeamDataRepository teamDataRepository,
            IUserDataRepository userDataRepository,
            IUserTypeService userTypeService,
            IUsersService usersService,
            IStringLocalizer<Strings> localizer)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.teamDataRepository = teamDataRepository ?? throw new ArgumentNullException(nameof(teamDataRepository));
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.userTypeService = userTypeService ?? throw new ArgumentNullException(nameof(userTypeService));
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// get the users data streams.
        /// </summary>
        /// <param name="notificationId">the notification id.</param>
        /// <returns>the streams of user data.</returns>
        public async IAsyncEnumerable<IEnumerable<UserData>> GetUserDataStreamAsync(string notificationId)
        {
            if (notificationId == null)
            {
                throw new ArgumentNullException(nameof(notificationId));
            }

            var sentNotificationDataEntitiesStream = this.sentNotificationDataRepository.GetStreamsAsync(notificationId);
            await foreach (var sentNotifcations in sentNotificationDataEntitiesStream)
            {
                List<User> userList = new List<User>();
                try
                {
                    // filter the recipient not found users.
                    var users = await this.usersService.GetBatchByUserIds(
                        sentNotifcations
                        .Where(sentNotifcation => !sentNotifcation.DeliveryStatus.Equals(SentNotificationDataEntity.RecipientNotFound, StringComparison.CurrentCultureIgnoreCase))
                        .Select(notitification => notitification.RowKey)
                        .ToList()
                        .AsGroups());
                    userList = users.ToList();
                }
                catch (ServiceException serviceException)
                {
                    if (serviceException.StatusCode != HttpStatusCode.Forbidden)
                    {
                        throw serviceException;
                    }
                }

                yield return await this.CreateUserDataAsync(sentNotifcations, userList);
            }
        }

        /// <summary>
        /// get the team data streams.
        /// </summary>
        /// <param name="notificationId">the notification id.</param>
        /// <returns>the streams of team data.</returns>
        public async IAsyncEnumerable<IEnumerable<TeamData>> GetTeamDataStreamAsync(string notificationId)
        {
            if (notificationId == null)
            {
                throw new ArgumentNullException(nameof(notificationId));
            }

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
                        Name = team?.Name,
                        DeliveryStatus = this.localizer.GetString(sentNotificationDataEntity.DeliveryStatus),
                        StatusReason = this.GetStatusReason(sentNotificationDataEntity.ErrorMessage, sentNotificationDataEntity.StatusCode.ToString()),
                    };
                    teamDataList.Add(teamData);
                }

                yield return teamDataList;
            }
        }

        /// <summary>
        /// Create user data.
        /// </summary>
        /// <param name="sentNotificationDataEntities">the list of sent notification data entities.</param>
        /// <param name="users">the user list.</param>
        /// <returns>list of created user data.</returns>
        private async Task<IEnumerable<UserData>> CreateUserDataAsync(
            IEnumerable<SentNotificationDataEntity> sentNotificationDataEntities,
            IEnumerable<User> users)
        {
            var userdatalist = new List<UserData>();
            foreach (var sentNotification in sentNotificationDataEntities)
            {
                var user = users.
                    FirstOrDefault(user => user != null && user.Id.Equals(sentNotification.RowKey));
                string userType = sentNotification.UserType;

                // For version less than CC v4.1.2 fetch from graph or user data table.
                if (string.IsNullOrEmpty(userType))
                {
                    var userDataEntity = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, sentNotification.RowKey);
                    userType = userDataEntity.UserType;
                    if (user != null && string.IsNullOrEmpty(userType))
                    {
                        // This is to set the UserType of the user.
                        await this.userTypeService.UpdateUserTypeForExistingUserAsync(userDataEntity, user.GetUserType());
                    }
                }

                var userData = new UserData
                {
                    Id = sentNotification.RowKey,
                    Name = user?.DisplayName ?? this.localizer.GetString("AdminConsentError"),
                    Upn = user?.UserPrincipalName ?? this.localizer.GetString("AdminConsentError"),
                    UserType = this.localizer.GetString(userType ?? (user?.GetUserType() ?? "AdminConsentError")),
                    DeliveryStatus = this.localizer.GetString(sentNotification.DeliveryStatus),
                    StatusReason = this.GetStatusReason(sentNotification.ErrorMessage, sentNotification.StatusCode.ToString()),
                };
                userdatalist.Add(userData);
            }

            return userdatalist;
        }

        /// <summary>
        /// adds the status code to error message.
        /// </summary>
        /// <param name="errorMessage">the error message.</param>
        /// <param name="statusCode">the status code.</param>
        /// <returns>status code appended error message.</returns>
        private string GetStatusReason(string errorMessage, string statusCode)
        {
            string result;
            if (string.IsNullOrEmpty(errorMessage))
            {
                result = this.localizer.GetString("OK");
            }
            else if (errorMessage.Contains("error"))
            {
                var rootMessage = JsonConvert.DeserializeObject<RootErrorMessage>(errorMessage);
                result = rootMessage.Error.Message;
            }
            else
            {
                result = errorMessage;
            }

            return $"{statusCode} : {result}";
        }
    }
}