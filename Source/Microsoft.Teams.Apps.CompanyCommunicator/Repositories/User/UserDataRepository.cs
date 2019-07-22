// <copyright file="UserDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User
{
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema;
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
            : base(configuration, "UserData")
        {
        }

        /// <summary>
        /// Add personal data in Table Storage.
        /// </summary>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task SavePersonalTypeData(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParsePersonalTypeData(activity);
            if (userDataEntity != null)
            {
                await this.CreateOrUpdate(userDataEntity);
            }
        }

        /// <summary>
        /// Remove personal data in table storage.
        /// </summary>
        /// <param name="activity">Bot conversation update activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task RemovePersonalTypeData(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParsePersonalTypeData(activity);
            if (userDataEntity != null)
            {
                var found = await this.Get(PartitionKeyNames.Metadata.UserData, userDataEntity.UserId);
                if (found != null)
                {
                    await this.Delete(found);
                }
            }
        }

        private UserDataEntity ParsePersonalTypeData(IConversationUpdateActivity activity)
        {
            var rowKey = activity?.From?.AadObjectId;
            if (rowKey != null)
            {
                var userDataEntity = new UserDataEntity
                {
                    PartitionKey = PartitionKeyNames.Metadata.UserData,
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
