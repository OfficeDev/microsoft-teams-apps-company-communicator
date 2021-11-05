// <copyright file="UserTypeService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Extensions;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph;

    /// <summary>
    /// This service is to set user type for existing users.
    /// </summary>
    public class UserTypeService : IUserTypeService
    {
        private readonly IUserDataRepository userDataRepository;
        private readonly IUsersService usersService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTypeService"/> class.
        /// </summary>
        /// <param name="userDataRepository">User data repository.</param>
        /// <param name="usersService">Users service.</param>
        public UserTypeService(
            IUserDataRepository userDataRepository,
            IUsersService usersService)
        {
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
            this.usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        }

        /// <inheritdoc/>
        public async Task UpdateUserTypeForExistingUserAsync(UserDataEntity userDataEntity, string userType)
        {
            if (userDataEntity == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(userDataEntity.UserType))
            {
                return;
            }

            if (string.IsNullOrEmpty(userType))
            {
                throw new ArgumentNullException(nameof(userType));
            }

            userDataEntity.UserType = userType;
            await this.userDataRepository.InsertOrMergeAsync(userDataEntity);
        }

        /// <inheritdoc/>
        public async Task UpdateUserTypeForExistingUserListAsync(IEnumerable<UserDataEntity> userDataEntities)
        {
            if (userDataEntities.IsNullOrEmpty())
            {
                return;
            }

            var userDataEntitiesWithNoUserType = userDataEntities.Where(userDataEntities => string.IsNullOrEmpty(userDataEntities.UserType));

            if (userDataEntitiesWithNoUserType.IsNullOrEmpty())
            {
                return;
            }

            var users = await this.usersService.GetBatchByUserIds(
                      userDataEntitiesWithNoUserType
                      .Select(user => user.AadId)
                      .AsBatches(Common.Constants.MaximumGraphAPIBatchSize));

            if (!users.IsNullOrEmpty())
            {
                var maxParallelism = Math.Min(users.Count(), 30);
                await users.ForEachAsync(maxParallelism, this.UpdateUserTypeAsync);
            }
        }

        private async Task UpdateUserTypeAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Store user.
            await this.userDataRepository.InsertOrMergeAsync(
                new UserDataEntity()
                {
                    PartitionKey = UserDataTableNames.UserDataPartition,
                    RowKey = user.Id,
                    AadId = user.Id,

                    // At times userType value from Graph response is null, to avoid null value
                    // using fallback logic to derive the userType from UserPrincipalName.
                    UserType = user.GetUserType(),
                });
        }
    }
}
