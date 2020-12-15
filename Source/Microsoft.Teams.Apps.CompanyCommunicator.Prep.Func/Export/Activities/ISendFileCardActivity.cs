// <copyright file="ISendFileCardActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.Export.Activities
{
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Interface for SendFileCardActivity.
    /// </summary>
    public interface ISendFileCardActivity
    {
        /// <summary>
        /// Run the activity.
        /// It sends the file card.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="sendData">Tuple containing user id, notification data entity and export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>responsse of send file card acitivity.</returns>
        public Task<string> RunAsync(
            IDurableOrchestrationContext context,
            (string userId, string notificationId, string fileName) sendData,
            ILogger log);

        /// <summary>
        /// Sends the file card to the user.
        /// </summary>
        /// <param name="sendData">Tuple containing user id, notification id and filename.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>file card response id.</returns>
        [FunctionName(nameof(SendFileCardActivityAsync))]
        public Task<string> SendFileCardActivityAsync(
            [ActivityTrigger](string userId, string notificationId, string fileName) sendData,
            ILogger log);
    }
}
