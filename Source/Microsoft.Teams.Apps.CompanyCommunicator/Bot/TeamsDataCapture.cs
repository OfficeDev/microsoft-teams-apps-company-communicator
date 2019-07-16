// <copyright file="TeamsDataCapture.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Capture teams data.
    /// </summary>
    public class TeamsDataCapture
    {
        private const string PersonalType = "personal";
        private const string ChannelType = "channel";

        private readonly TeamsDataRepository teamsDataRepository;
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsDataCapture"/> class.
        /// </summary>
        /// <param name="teamsDataRepository">Team data repository instance.</param>
        /// <param name="userDataRepository">User data repository instance.</param>
        public TeamsDataCapture(
            TeamsDataRepository teamsDataRepository,
            UserDataRepository userDataRepository)
        {
            this.teamsDataRepository = teamsDataRepository;
            this.userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Add channel or personal data in Table Storage.
        /// </summary>
        /// <param name="activity">Activity instance.</param>
        public void OnAdded(IConversationUpdateActivity activity)
        {
            switch (activity.Conversation.ConversationType)
            {
                case TeamsDataCapture.ChannelType:
                    this.SaveChannelTypeData(activity);
                    break;
                case TeamsDataCapture.PersonalType:
                    this.SavePersonalTypeData(activity);
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Remove channel or personal data in table storage.
        /// </summary>
        /// <param name="activity">Activity instance.</param>
        public void OnRemoved(IConversationUpdateActivity activity)
        {
            switch (activity.Conversation.ConversationType)
            {
                case TeamsDataCapture.ChannelType:
                    this.RemoveChannelTypeData(activity);
                    break;
                case TeamsDataCapture.PersonalType:
                    this.RemovePersonalTypeData(activity);
                    break;
                default: break;
            }
        }

        private void SaveChannelTypeData(IConversationUpdateActivity activity)
        {
            var teamDataEntity = this.ParseChannelTypeData(activity);
            if (teamDataEntity != null)
            {
                this.teamsDataRepository.CreateOrUpdate(teamDataEntity);
            }
        }

        private void RemoveChannelTypeData(IConversationUpdateActivity activity)
        {
            var teamDataEntity = this.ParseChannelTypeData(activity);
            if (teamDataEntity != null)
            {
                var found = this.teamsDataRepository.Get(PartitionKeyNames.TeamsData, teamDataEntity.TeamId);
                if (found != null)
                {
                    this.teamsDataRepository.Delete(found);
                }
            }
        }

        private TeamsDataEntity ParseChannelTypeData(IConversationUpdateActivity activity)
        {
            var jObject = activity?.ChannelData as JObject;
            if (jObject != null)
            {
                var teamsDataEntity = new TeamsDataEntity
                {
                    PartitionKey = PartitionKeyNames.TeamsData,
                    RowKey = jObject["team"]["id"].ToString(),
                    TeamId = jObject["team"]["id"].ToString(),
                    Name = jObject["team"]["name"].ToString(),
                    ServiceUrl = activity.ServiceUrl,
                    TenantId = jObject["tenant"]["id"].ToString(),
                };

                return teamsDataEntity;
            }

            return null;
        }

        private void SavePersonalTypeData(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParsePersonalTypeData(activity);
            this.userDataRepository.CreateOrUpdate(userDataEntity);
        }

        private void RemovePersonalTypeData(IConversationUpdateActivity activity)
        {
            var userDataEntity = this.ParsePersonalTypeData(activity);
            var found = this.userDataRepository.Get(PartitionKeyNames.UserData, userDataEntity.UserId);
            if (found != null)
            {
                this.userDataRepository.Delete(found);
            }
        }

        private UserDataEntity ParsePersonalTypeData(IConversationUpdateActivity activity)
        {
            var userDataEntity = new UserDataEntity
            {
                PartitionKey = PartitionKeyNames.UserData,
                RowKey = activity?.From?.Id,
                AadId = activity?.From?.AadObjectId,
                UserId = activity?.From?.Id,
                ConversationId = activity?.Conversation?.Id,
                ServiceUrl = activity?.ServiceUrl,
                TenantId = activity?.Conversation?.TenantId,
            };

            return userDataEntity;
        }
    }
}
