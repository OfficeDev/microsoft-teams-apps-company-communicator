// <copyright file="SyncCSVActivity.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Recipients;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User;
    using Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.Extensions;
    using Newtonsoft.Json;

    /// <summary>
    /// Syncs users of the CSV file to Sent notification table.
    /// </summary>
    public class SyncCSVActivity
    {
        private readonly IUserDataRepository userDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IUsersService usersService;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly IUserTypeService userTypeService;
        private readonly IRecipientsService recipientsService;
        private readonly IStringLocalizer<Strings> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncCSVActivity"/> class.
        /// </summary>
        /// <param name="userDataRepository">User Data repository.</param>
        /// <param name="sentNotificationDataRepository">Sent notification data repository.</param>
        /// <param name="usersService">Users service.</param>
        /// <param name="notificationDataRepository">Notification data entity repository.</param>
        /// <param name="userTypeService">User type service.</param>
        /// <param name="recipientsService">The recipients service.</param>
        /// <param name="localizer">Localization service.</param>
        public SyncCSVActivity(
            IUserDataRepository userDataRepository,
            ISentNotificationDataRepository sentNotificationDataRepository,
            IUsersService usersService,
            INotificationDataRepository notificationDataRepository,
            IUserTypeService userTypeService,
            IRecipientsService recipientsService,
            IStringLocalizer<Strings> localizer)
        {
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.userTypeService = userTypeService ?? throw new ArgumentNullException(nameof(userTypeService));
            this.recipientsService = recipientsService ?? throw new ArgumentNullException(nameof(recipientsService));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Syncs CSV users to Sent notification table.
        /// </summary>
        /// <param name="notification">Input.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>It returns the group transitive members first page and next page url.</returns>
        [FunctionName(FunctionNames.SyncCSVActivity)]
        public async Task<RecipientsInfo> RunAsync([ActivityTrigger] NotificationDataEntity notification, ILogger log)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            log.LogDebug("About to process the list of CSV users.");
            log.LogDebug("notification.CsvUsers: " + notification.CsvUsers);

            // sync users from the csv file
            List<User> users = new List<User>();
            string strtp = notification.CsvUsers.Substring(2, notification.CsvUsers.Length - 4);
            var csvUsersArray = strtp.Split(",");

            string usertp;
            User usr;
            foreach (string userst in csvUsersArray)
            {
                if (!userst.IsNullOrEmpty())
                {
                    usertp = userst.Substring(1, userst.Length - 2);
                    log.LogDebug("Processing: " + usertp);

                    try
                    {
                        usr = await this.usersService.GetUserAsync(usertp);
                        users.Add(usr);
                        log.LogDebug("User " + usertp + " added to the collection.");
                    }
                    catch (Exception ex)
                    {
                        log.LogError("User not added to the collection.");
                        log.LogError("User " + usertp + " is invalid. " + ex.Message);
                    }
                }
            }

            log.LogDebug("About to convert to recipients.");

            // Convert to Recipients
            var recipients = await this.GetRecipientsAsync(notification.Id, users);
            log.LogDebug("CSV Users converted to recipients.");

            log.LogDebug("About to store the list of recipients on the database.");

            // Store.
            await this.sentNotificationDataRepository.BatchInsertOrMergeAsync(recipients);
            log.LogDebug("Sent messages stored on the database for future updates.");

            log.LogDebug("Finished. Batching recipients and moving to the next pipeline step.");

            // Store in batches and return batch info.
            return await this.recipientsService.BatchRecipients(recipients);
        }

        /// <summary>
        /// Reads corresponding user entity from User table and creates a recipient for every user.
        /// </summary>
        /// <param name="notificationId">Notification Id.</param>
        /// <param name="users">Users.</param>
        /// <returns>List of recipients.</returns>
        private async Task<IEnumerable<SentNotificationDataEntity>> GetRecipientsAsync(string notificationId, IEnumerable<User> users)
        {
            var recipients = new ConcurrentBag<SentNotificationDataEntity>();

            // Get User Entities.
            var maxParallelism = Math.Min(100, users.Count());
            await Task.WhenAll(users.ForEachAsync(maxParallelism, async user =>
            {
                var userEntity = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, user.Id);

                // This is to set the type of user(existing only, new ones will be skipped) to identify later if it is member or guest.
                var userType = user.UserPrincipalName.GetUserType();
                if (userEntity == null && userType.Equals(UserType.Guest, StringComparison.OrdinalIgnoreCase))
                {
                    // Skip processing new Guest users.
                    return;
                }

                await this.userTypeService.UpdateUserTypeForExistingUserAsync(userEntity, userType);
                if (userEntity == null)
                {
                    userEntity = new UserDataEntity()
                    {
                        AadId = user.Id,
                        UserType = userType,
                    };
                }

                recipients.Add(userEntity.CreateInitialSentNotificationDataEntity(partitionKey: notificationId));
            }));

            return recipients;
        }
    }
}