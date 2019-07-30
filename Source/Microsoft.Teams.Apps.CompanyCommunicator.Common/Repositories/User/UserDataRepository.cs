// <copyright file="UserDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.User
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Respository of the user data stored in the table storage.
    /// </summary>
    public class UserDataRepository : BaseRepository<UserDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        public UserDataRepository(IConfiguration configuration)
            : base(configuration, "UserData", PartitionKeyNames.Metadata.UserData)
        {
        }
    }
}
