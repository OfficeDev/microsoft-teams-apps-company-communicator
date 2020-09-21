// <copyright file="TeamsConversationOrchestrator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// Teams conversation orchestrator.
    /// Does following:
    /// 1. Gets all the recipients for whom we do not have conversation Id.
    /// 2. Creates conversation.
    ///
    /// Note: The orchestrator only handles "members of specific team" scenario. Support for other scenarios
    /// will be added with proactive app installation changes.
    /// </summary>
    public static class TeamsConversationOrchestrator
    {
        /// <summary>
        /// TeamsConversationOrchestrator function.
        /// Does following:
        /// 1. Creates conversation for members of Teams if the conversationId isn't available.
        /// 2. No-op for other target set of users.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="log">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [FunctionName(FunctionNames.TeamsConversationOrchestrator)]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var notification = context.GetInput<NotificationDataEntity>();

            // Members of specific teams.
            if (notification.Rosters.Any())
            {
                if (!context.IsReplaying)
                {
                    log.LogInformation($"About to get pending recipients. (who do not have a conversation id in the database.");
                }

                var recipients = await context.CallActivityWithRetryAsync<IEnumerable<SentNotificationDataEntity>>(
                    FunctionNames.GetPendingRecipientsActivity,
                    FunctionSettings.DefaultRetryOptions,
                    notification);

                if (!context.IsReplaying)
                {
                    log.LogInformation("About to create conversation.");
                }

                var tasks = new List<Task>();
                foreach (var recipient in recipients)
                {
                    var task = context.CallActivityWithRetryAsync(
                        FunctionNames.TeamsConversationActivity,
                        FunctionSettings.DefaultRetryOptions,
                        recipient);
                    tasks.Add(task);
                }

                // Fan-out Fan-in.
                await Task.WhenAll(tasks);
            }
        }
    }
}
