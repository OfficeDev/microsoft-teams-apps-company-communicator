// <copyright file="ExportDataRequirement.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Model
{
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.ExportData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;

    /// <summary>
    /// Export data requirement model class.
    /// </summary>
    public class ExportDataRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDataRequirement"/> class.
        /// </summary>
        /// <param name="notificationDataEntity">the notification data entity.</param>
        /// <param name="exportDataEntity">the esport data entity.</param>
        /// <param name="userId">user id.</param>
        public ExportDataRequirement(
            NotificationDataEntity notificationDataEntity,
            ExportDataEntity exportDataEntity,
            string userId)
        {
            this.NotificationDataEntity = notificationDataEntity;
            this.ExportDataEntity = exportDataEntity;
            this.UserId = userId;
        }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public string UserId { get; private set; }

        /// <summary>
        /// Gets the notification data entity.
        /// </summary>
        public NotificationDataEntity NotificationDataEntity { get; private set; }

        /// <summary>
        /// Gets the export data entity.
        /// </summary>
        public ExportDataEntity ExportDataEntity { get; private set; }

        /// <summary>
        /// Check if requirement is met.
        /// </summary>
        /// <returns>value to determine if reuirement is met.</returns>
        public bool IsValid()
        {
            return this.NotificationDataEntity != null && this.ExportDataEntity != null;
        }
    }
}
