// <copyright file="NotificationDataExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Notification data extensions.
    /// </summary>
    public static class NotificationDataExtensions
    {
        /// <summary>
        /// Checks if the notification is completed.
        ///
        /// Note: we check the IsCompleted property for backward compatibility. (Data generated for CC v1, v2).
        /// </summary>
        /// <param name="entity">Notification data entity.</param>
        /// <returns>If the notification is completed.</returns>
        public static bool IsCompleted(this NotificationDataEntity entity)
        {
            return NotificationStatus.Failed.ToString().Equals(entity.Status) ||
                NotificationStatus.Sent.ToString().Equals(entity.Status) ||
                entity.IsCompleted;
        }

        /// <summary>
        /// Returns notification status.
        ///
        /// Note: We check for IsCompleted property for backward compatibility. (Data generated for CC v1, v2).
        /// </summary>
        /// <param name="entity">Notification data entity.</param>
        /// <returns>Notification status.</returns>
        public static string GetStatus(this NotificationDataEntity entity)
        {
            // For v1, v2, status field is not set.
            if (entity.Status == null)
            {
                return entity.IsCompleted ? NotificationStatus.Sent.ToString() : NotificationStatus.Unknown.ToString();
            }

            return entity.Status.ToString();
        }
    }
}
