// <copyright file="UserDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Notification;

    /// <summary>
    /// Respository for the user data in the table storage.
    /// </summary>
    public class UserDataRepository : BaseRepository<UserDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public UserDataRepository(IConfiguration configuration)
            : base(configuration, "UserData")
        {
        }
    }
}
