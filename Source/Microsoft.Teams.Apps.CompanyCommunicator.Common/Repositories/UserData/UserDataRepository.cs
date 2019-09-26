// <copyright file="UserDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Repository of the user data stored in the table storage.
    /// </summary>
    public class UserDataRepository : BaseRepository<UserDataEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataRepository"/> class.
        /// </summary>
        /// <param name="configuration">Represents the application configuration.</param>
        /// <param name="isFromAzureFunction">Flag to show if created from Azure Function.</param>
        public UserDataRepository(IConfiguration configuration, bool isFromAzureFunction = false)
            : base(
                configuration,
                PartitionKeyNames.UserDataTable.TableName,
                PartitionKeyNames.UserDataTable.UserDataPartition,
                isFromAzureFunction)
        {
        }
    }
}
