// <copyright file="CompanyCommunicatorBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Company Communicator Bot.
    /// </summary>
    public class CompanyCommunicatorBot : ActivityHandler
    {
        /// <summary>
        /// The bot framework calls the method when receiving a message from an user.
        /// </summary>
        /// <param name="turnContext">ITurnContext instance.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await base.OnMessageActivityAsync(turnContext, cancellationToken);

            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);
        }

        /// <summary>
        /// The bot framework calls the method when a new member (human user) is added.
        /// </summary>
        /// <param name="membersAdded">A collection of added members.</param>
        /// <param name="turnContext">ITurnContext instance.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);

            foreach (var member in membersAdded)
            {
                // Take action if this event includes user being added
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await this.SendAsync(turnContext, cancellationToken, "Hello and Welcome!");
                    await this.RecordAsync(
                        member.Id,
                        "AddedOnMembersAdded",
                        turnContext.Activity.Recipient.Id,
                        turnContext.Activity.Conversation.ConversationType);
                }
            }
        }

        /// <summary>
        /// The bot framework calls the method when a member (human user) is removed.
        /// </summary>
        /// <param name="membersRemoved">A collection of removed members.</param>
        /// <param name="turnContext">ITurnContext instance.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnMembersRemovedAsync(IList<ChannelAccount> membersRemoved, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await base.OnMembersRemovedAsync(membersRemoved, turnContext, cancellationToken);

            foreach (var member in membersRemoved)
            {
                // Take action if this event includes user being removed
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await this.RecordAsync(
                        member.Id,
                        "RemovedOnMembersRemoved",
                        turnContext.Activity.Recipient.Id,
                        turnContext.Activity.Conversation.ConversationType);
                }
            }
        }

        /// <summary>
        /// Invoked when a conversation update activity is received from the channel.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // base.OnConversationUpdateActivityAsync is useful when it comes to responding to users being added to or removed from the conversation.
            // For example, a bot could respond to a user being added by greeting the user.
            // By default, base.OnConversationUpdateActivityAsync will call <see cref="OnMembersAddedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
            // if any users have been added or <see cref="OnMembersRemovedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
            // if any users have been removed. base.OnConversationUpdateActivityAsync checks the member ID so that it only responds to updates regarding members other than the bot itself.
            await base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);

            var activity = turnContext.Activity;
            var fromId = activity.From.Id;
            var botId = activity.Recipient.Id;

            // Take action if this event includes the bot being added
            if (activity.MembersAdded?.FirstOrDefault(p => p.Id == botId) != null)
            {
                await this.SendAsync(turnContext, cancellationToken, "Hello and Welcome!");
                await this.RecordAsync(
                    fromId,
                    "AddedOnConversationUpdateActivity",
                    botId,
                    activity.Conversation.ConversationType);
            }

            // Take action if this event includes the bot being removed
            if (activity.MembersRemoved?.FirstOrDefault(p => p.Id == botId) != null)
            {
                await this.RecordAsync(
                    fromId,
                    "RemovedOnConversationUpdateActivity",
                    botId,
                    activity.Conversation.ConversationType);
            }
        }

        private async Task RecordAsync(string fromId, string mutation, string botId, string conversationType)
        {
            await Task.Run(() =>
            {
                Console.WriteLine($"Event: {mutation}");
                Console.WriteLine($"FromId: {fromId}");
                Console.WriteLine($"ConversationType: {conversationType}");
                Console.WriteLine($"BotId: {botId}");
                Console.WriteLine();
            });
        }

        private async Task SendAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken, string message)
        {
            var card = new HeroCard
            {
                Title = "Company Communicator",
                Subtitle = "Powered by Microsoft Bot Framework",
                Text = message,
                Images = new List<CardImage> { new CardImage("https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/buildreactionbotframework_960.jpg") },
                Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Get Started", value: "https://docs.microsoft.com/bot-framework") },
            };

            var reply = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
