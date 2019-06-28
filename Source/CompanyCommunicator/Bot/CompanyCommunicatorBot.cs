using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompanyCommunicator.Bot
{
    public class CompanyCommunicatorBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Text($"Echo: {turnContext.Activity.Text}"), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);

            foreach (var member in membersAdded)
            {
                // Take action if this event includes user being added
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await SendAsync(turnContext, cancellationToken, "Hello and Welcome!");
                    await RecordAsync(
                        member.Id, 
                        "AddedOnMembersAdded", 
                        turnContext.Activity.Recipient.Id,
                        turnContext.Activity.Conversation.ConversationType);
                }
            }
        }

        protected override async Task OnMembersRemovedAsync(IList<ChannelAccount> membersRemoved, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await base.OnMembersRemovedAsync(membersRemoved, turnContext, cancellationToken);

            foreach (var member in membersRemoved)
            {
                // Take action if this event includes user being removed
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await RecordAsync(
                        member.Id, 
                        "RemovedOnMembersRemoved", 
                        turnContext.Activity.Recipient.Id,
                        turnContext.Activity.Conversation.ConversationType);
                }
            }
        }

        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // base.OnConversationUpdateActivityAsync calls OnMembersAddedAsync/OnMembersRemovedAsync if this event includes user being added/removed
            // So the base method has to be called in here. 
            await base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);

            var activity = turnContext.Activity;
            var fromId = activity.From.Id;
            var botId = activity.Recipient.Id;

            // Take action if this event includes the bot being added
            if (activity.MembersAdded?.FirstOrDefault(p => p.Id == botId) != null)
            {
                await SendAsync(turnContext, cancellationToken, "Hello and Welcome!");
                await RecordAsync(
                    fromId, 
                    "AddedOnConversationUpdateActivity", 
                    botId,
                    activity.Conversation.ConversationType);
            }

            // Take action if this event includes the bot being removed
            if (activity.MembersRemoved?.FirstOrDefault(p => p.Id == botId) != null)
            {
                await RecordAsync(
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
