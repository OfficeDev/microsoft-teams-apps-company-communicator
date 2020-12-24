// <copyright file="UserDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.User
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// User Data service.
    /// </summary>
    public class UserDataService : IUserDataService
    {
        private readonly IUserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDataService"/> class.
        /// </summary>
        /// <param name="userDataRepository">User data repository.</param>
        public UserDataService(IUserDataRepository userDataRepository)
        {
            this.userDataRepository = userDataRepository ?? throw new ArgumentNullException(nameof(userDataRepository));
        }

        /// <inheritdoc/>
        public async Task SaveUserDataAsync(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParseData(activity, UserDataTableNames.UserDataPartition);
            if (userDataEntity != null)
            {
                await this.userDataRepository.InsertOrMergeAsync(userDataEntity);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveUserDataAsync(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParseData(activity, UserDataTableNames.UserDataPartition);
            if (userDataEntity != null)
            {
                var found = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, userDataEntity.AadId);
                if (found != null)
                {
                    await this.userDataRepository.DeleteAsync(found);
                }
            }
        }

        /// <inheritdoc/>
        public async Task SaveAuthorDataAsync(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParseData(activity, UserDataTableNames.AuthorDataPartition);
            if (userDataEntity != null)
            {
                await this.userDataRepository.InsertOrMergeAsync(userDataEntity);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAuthorDataAsync(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParseData(activity, UserDataTableNames.AuthorDataPartition);
            if (userDataEntity != null)
            {
                var found = await this.userDataRepository.GetAsync(UserDataTableNames.AuthorDataPartition, userDataEntity.AadId);
                if (found != null)
                {
                    await this.userDataRepository.DeleteAsync(found);
                }
            }
        }

        private UserDataEntity ParseData(IConversationUpdateActivity activity, string partitionKey)
        {
            var rowKey = activity?.From?.AadObjectId;
            if (rowKey == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return new UserDataEntity
            {
                PartitionKey = partitionKey,
                RowKey = activity?.From?.AadObjectId,
                AadId = activity?.From?.AadObjectId,
                UserId = activity?.From?.Id,
                ConversationId = partitionKey.Equals(UserDataTableNames.UserDataPartition) ? activity?.Conversation?.Id : null,
                ServiceUrl = activity?.ServiceUrl,
                TenantId = activity?.Conversation?.TenantId,
            };
        }
    }
}
