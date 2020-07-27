// <copyright file="UserDataRepositoryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions
{
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Extensions for the repository of the user data stored in the table storage.
    /// </summary>
    public static class UserDataRepositoryExtensions
    {
        /// <summary>
        /// Add personal data in Table Storage.
        /// </summary>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public static async Task SaveUserDataAsync(
            this UserDataRepository userDataRepository,
            IConversationUpdateActivity activity)
        {
            var userDataEntity = UserDataRepositoryExtensions.ParseUserData(activity);
            if (userDataEntity != null)
            {
                await userDataRepository.InsertOrMergeAsync(userDataEntity);
            }
        }

        /// <summary>
        /// Remove personal data in table storage.
        /// </summary>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public static async Task RemoveUserDataAsync(
            this UserDataRepository userDataRepository,
            IConversationUpdateActivity activity)
        {
            var userDataEntity = UserDataRepositoryExtensions.ParseUserData(activity);
            if (userDataEntity != null)
            {
                var found = await userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, userDataEntity.AadId);
                if (found != null)
                {
                    await userDataRepository.DeleteAsync(found);
                }
            }
        }

        private static UserDataEntity ParseUserData(IConversationUpdateActivity activity)
        {
            var rowKey = activity?.From?.AadObjectId;
            if (rowKey != null)
            {
                var userDataEntity = new UserDataEntity
                {
                    PartitionKey = UserDataTableNames.UserDataPartition,
                    RowKey = activity?.From?.AadObjectId,
                    AadId = activity?.From?.AadObjectId,
                    UserId = activity?.From?.Id,
                    ConversationId = activity?.Conversation?.Id,
                    ServiceUrl = activity?.ServiceUrl,
                    TenantId = activity?.Conversation?.TenantId,
                };

                return userDataEntity;
            }

            return null;
        }
    }
}
