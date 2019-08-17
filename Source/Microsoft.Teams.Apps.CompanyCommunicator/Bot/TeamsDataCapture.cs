// <copyright file="TeamsDataCapture.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Repositories.Extensions;

    /// <summary>
    /// Service to capture teams data.
    /// </summary>
    public class TeamsDataCapture
    {
        private const string PersonalType = "personal";
        private const string ChannelType = "channel";

        private readonly TeamDataRepository teamDataRepository;
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsDataCapture"/> class.
        /// </summary>
        /// <param name="teamDataRepository">Team data repository instance.</param>
        /// <param name="userDataRepository">User data repository instance.</param>
        public TeamsDataCapture(
            TeamDataRepository teamDataRepository,
            UserDataRepository userDataRepository)
        {
            this.teamDataRepository = teamDataRepository;
            this.userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Add channel or personal data in Table Storage.
        /// </summary>
        /// <param name="activity">Teams activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task OnBotAddedAsync(IConversationUpdateActivity activity)
        {
            switch (activity.Conversation.ConversationType)
            {
                case TeamsDataCapture.ChannelType:
                    await this.teamDataRepository.SaveTeamDataAsync(activity);
                    break;
                case TeamsDataCapture.PersonalType:
                    await this.userDataRepository.SaveUserDataAsync(activity);
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Remove channel or personal data in table storage.
        /// </summary>
        /// <param name="activity">Teams activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task OnBotRemovedAsync(IConversationUpdateActivity activity)
        {
            switch (activity.Conversation.ConversationType)
            {
                case TeamsDataCapture.ChannelType:
                    await this.teamDataRepository.RemoveTeamDataAsync(activity);
                    break;
                case TeamsDataCapture.PersonalType:
                    await this.userDataRepository.RemoveUserDataAsync(activity);
                    break;
                default: break;
            }
        }

        /// <summary>
        /// Update team information in the table storage.
        /// </summary>
        /// <param name="activity">Teams activity instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task OnTeamInformationUpdatedAsync(IConversationUpdateActivity activity)
        {
            await this.teamDataRepository.SaveTeamDataAsync(activity);
        }
    }
}
