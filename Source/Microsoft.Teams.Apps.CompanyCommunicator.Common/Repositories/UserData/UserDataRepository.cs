// <copyright file="UserDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Repository of the user data stored in the table storage.
    /// </summary>
    public class UserDataRepository : BaseRepository<UserDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="repositoryOptions">Options used to create the repository.</param>
        public UserDataRepository(IConfiguration configuration, IOptions<RepositoryOptions> repositoryOptions)
            : base(
                configuration,
                PartitionKeyNames.UserDataTable.TableName,
                PartitionKeyNames.UserDataTable.UserDataPartition,
                repositoryOptions.Value.IsAzureFunction)
        {
        }
    }
}
