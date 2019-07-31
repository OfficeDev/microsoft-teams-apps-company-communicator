// <copyright file="NotificationRepositoryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.Notification;
    using Microsoft.Teams.Apps.CompanyCommunicator.Models;

    /// <summary>
    /// Extensions for the respository of the notification data.
    /// </summary>
    public static class NotificationRepositoryExtensions
    {
        /// <summary>
        /// Create a new draft notification.
        /// </summary>
        /// <param name="notificationRepository">The notification respository.</param>
        /// <param name="notification">Draft Notification model class instance passed in from Web API.</param>
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        /// <param name="userName">Name of the user who is running the application.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public static async Task CreateDraftNotificationAsync(
            this NotificationRepository notificationRepository,
            DraftNotification notification,
            TableRowKeyGenerator tableRowKeyGenerator,
            string userName)
        {
            var newId = tableRowKeyGenerator.NewKeyInLogHeadPattern();

            var notificationEntity = new NotificationEntity
            {
                PartitionKey = PartitionKeyNames.Notification.DraftNotifications,
                RowKey = newId,
                Id = newId,
                Title = notification.Title,
                ImageLink = notification.ImageLink,
                Summary = notification.Summary,
                Author = notification.Author,
                ButtonTitle = notification.ButtonTitle,
                ButtonLink = notification.ButtonLink,
                CreatedBy = userName,
                CreatedTime = DateTime.UtcNow,
                IsDraft = true,
                Teams = notification.Teams,
                Rosters = notification.Rosters,
                AllUsers = notification.AllUsers,
            };

            await notificationRepository.CreateOrUpdateAsync(notificationEntity);
        }
    }
}
