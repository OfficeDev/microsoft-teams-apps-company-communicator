// <copyright file="TeamsDataCapture.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Team;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.User;

    /// <summary>
    /// Service to capture teams data.
    /// </summary>
    public class TeamsDataCapture
    {
        private const string PersonalType = "personal";
        private const string ChannelType = "channel";

        private readonly TeamDataRepository teamsDataRepository;
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsDataCapture"/> class.
        /// </summary>
        /// <param name="teamsDataRepository">Team data repository instance.</param>
        /// <param name="userDataRepository">User data repository instance.</param>
        public TeamsDataCapture(
            TeamDataRepository teamsDataRepository,
            UserDataRepository userDataRepository)
        {
            this.teamsDataRepository = teamsDataRepository;
            this.userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Add channel or personal data in Table Storage.
        /// </summary>
        /// <param name="activity">Teams activity instance.</param>
        public void OnBotAdded(IConversationUpdateActivity activity)
        {
            switch (activity.Conversation.ConversationType)
            {
                case TeamsDataCapture.ChannelType:
                    this.teamsDataRepository.SaveChannelTypeData(activity);
                    break;
                case TeamsDataCapture.PersonalType:
                    this.userDataRepository.SavePersonalTypeData(activity);
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Remove channel or personal data in table storage.
        /// </summary>
        /// <param name="activity">Teams activity instance.</param>
        public void OnBotRemoved(IConversationUpdateActivity activity)
        {
            switch (activity.Conversation.ConversationType)
            {
                case TeamsDataCapture.ChannelType:
                    this.teamsDataRepository.RemoveChannelTypeData(activity);
                    break;
                case TeamsDataCapture.PersonalType:
                    this.userDataRepository.RemovePersonalTypeData(activity);
                    break;
                default: break;
            }
        }
    }
}
