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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
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

        /// <inheritdoc/>
        public async IAsyncEnumerable<IEnumerable<UserData>> GetUserDataStreamAsync(string notificationId, string notificationStatus)
        {
            _ = notificationId ?? throw new ArgumentNullException(nameof(notificationId));
            _ = notificationStatus ?? throw new ArgumentNullException(nameof(notificationStatus));

            var sentNotificationDataEntitiesStream = this.sentNotificationDataRepository.GetStreamsAsync(notificationId);
            var isForbidden = false;

            await foreach (var sentNotifications in sentNotificationDataEntitiesStream)
            {
                var users = new List<User>();

                // filter the recipient not found users.
                var recipients = new List<SentNotificationDataEntity>();
                foreach (var sentNotification in sentNotifications)
                {
                    if (sentNotification.DeliveryStatus != null && sentNotification.DeliveryStatus.Equals(SentNotificationDataEntity.RecipientNotFound, StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    recipients.Add(sentNotification);
                }

                try
                {
                    if (!isForbidden)
                    {
                        // Group the recipients as per the Graph batch api.
                        var groupRecipientsByAadId = recipients?
                           .Select(notitification => notitification.RowKey)
                           .AsBatches(Common.Constants.MaximumGraphAPIBatchSize);

                        if (!groupRecipientsByAadId.IsNullOrEmpty())
                        {
                            users = (await this.usersService.GetBatchByUserIds(groupRecipientsByAadId))?.ToList();
                        }
                    }
                }
                catch (ServiceException serviceException)
                {
                    if (serviceException.StatusCode != HttpStatusCode.Forbidden)
                    {
                        throw;
                    }

                    // Set isForbidden to true in case of Forbidden exception.
                    isForbidden = true;
                }

                if (isForbidden)
                {
                    yield return this.CreatePartialUserData(recipients, notificationStatus);
                }
                else
                {
                    yield return await this.CreateUserDataAsync(recipients, users, notificationStatus);
                }
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IEnumerable<TeamData>> GetTeamDataStreamAsync(string notificationId, string notificationStatus)
        {
            _ = notificationId ?? throw new ArgumentNullException(nameof(notificationId));
            _ = notificationStatus ?? throw new ArgumentNullException(nameof(notificationStatus));

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
                        DeliveryStatus = sentNotificationDataEntity.DeliveryStatus is null ? sentNotificationDataEntity.DeliveryStatus : this.localizer.GetString(sentNotificationDataEntity.DeliveryStatus),
                        StatusReason = this.GetStatusReason(sentNotificationDataEntity.ErrorMessage, sentNotificationDataEntity.StatusCode, notificationStatus),
                        Error = sentNotificationDataEntity.Exception,
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
        /// <param name="notificationStatus">the notification status.</param>
        /// <returns>list of created user data.</returns>
        private async Task<IEnumerable<UserData>> CreateUserDataAsync(
            IEnumerable<SentNotificationDataEntity> sentNotificationDataEntities,
            IEnumerable<User> users,
            string notificationStatus)
        {
            var userdatalist = new List<UserData>();
            foreach (var sentNotification in sentNotificationDataEntities)
            {
                var user = users?.FirstOrDefault(user => user != null && user.Id.Equals(sentNotification.RowKey));
                string userType = sentNotification.UserType;

                // For version less than CC v4.1.2 fetch from user data table or Graph.
                if (string.IsNullOrEmpty(userType))
                {
                    var userDataEntity = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, sentNotification.RowKey);
                    userType = userDataEntity?.UserType;
                    if (user != null && string.IsNullOrEmpty(userType))
                    {
                        userType = user.GetUserType();

                        // This is to set the UserType of the user.
                        await this.userTypeService.UpdateUserTypeForExistingUserAsync(userDataEntity, userType);
                    }
                }

                userdatalist.Add(new UserData()
                {
                    Id = sentNotification.RowKey,
                    Name = user?.DisplayName,
                    Upn = user?.UserPrincipalName,
                    UserType = userType is null ? userType : this.localizer.GetString(userType),
                    DeliveryStatus = sentNotification.DeliveryStatus is null ? sentNotification.DeliveryStatus : this.localizer.GetString(sentNotification.DeliveryStatus),
                    StatusReason = this.GetStatusReason(sentNotification.ErrorMessage, sentNotification.StatusCode, notificationStatus),
                    Error = sentNotification.Exception,
                });
            }

            return userdatalist;
        }

        /// <summary>
        /// Create partial user data.
        /// </summary>
        /// <param name="sentNotificationDataEntities">the list of sent notification data entities.</param>
        /// <param name="notificationStatus">the notification status.</param>
        /// <returns>user data list.</returns>
        private IEnumerable<UserData> CreatePartialUserData(IEnumerable<SentNotificationDataEntity> sentNotificationDataEntities, string notificationStatus)
        {
            return sentNotificationDataEntities
                .Select(sentNotification =>
                new UserData()
                {
                    Id = sentNotification.RowKey,
                    Name = this.localizer.GetString("AdminConsentError"),
                    Upn = this.localizer.GetString("AdminConsentError"),
                    UserType = this.localizer.GetString(sentNotification.UserType ?? "AdminConsentError"),
                    DeliveryStatus = sentNotification.DeliveryStatus is null ? sentNotification.DeliveryStatus : this.localizer.GetString(sentNotification.DeliveryStatus),
                    StatusReason = this.GetStatusReason(sentNotification.ErrorMessage, sentNotification.StatusCode, notificationStatus),
                    Error = sentNotification.Exception,
                }).ToList();
        }

        /// <summary>
        /// adds the status code to error message.
        /// </summary>
        /// <param name="errorMessage">the error message.</param>
        /// <param name="statusCode">the status code.</param>
        /// <param name="notificationStatus">the notification status.</param>
        /// <returns>status code appended error message.</returns>
        private string GetStatusReason(string errorMessage, int statusCode, string notificationStatus)
        {
            string result;
            if (string.IsNullOrEmpty(errorMessage))
            {
                // If the statusCode is initialized (i.e. the notification was picked to process for the recipient) and the notification status is Canceled,
                // then show the status reason as Canceled.
                if (statusCode == SentNotificationDataEntity.InitializationStatusCode && notificationStatus.Equals(NotificationStatus.Canceled.ToString()))
                {
                    result = this.localizer.GetString("Canceled");
                }
                else
                {
                    result = this.localizer.GetString("OK");
                }
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