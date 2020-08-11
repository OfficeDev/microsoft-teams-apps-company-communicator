// <copyright file="ManageResultDataService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.DataServices
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;

    /// <summary>
    /// The manage result data service.
    /// </summary>
    public class ManageResultDataService
    {
        private readonly SentNotificationDataRepository sentNotificationDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManageResultDataService"/> class.
        /// </summary>
        /// <param name="sentNotificationDataRepository">The sent notification data repository.</param>
        public ManageResultDataService(
            SentNotificationDataRepository sentNotificationDataRepository)
        {
            this.sentNotificationDataRepository = sentNotificationDataRepository;
        }

        /// <summary>
        /// Processes the notification's result data.
        /// </summary>
        /// <param name="notificationId">The notification Id.</param>
        /// <param name="recipientId">The recipient's unique identifier.
        ///     If the recipient is a user, this should be the AAD Id.
        ///     If the recipient is a team, this should be the team Id.</param>
        /// <param name="totalNumberOfSendThrottles">The total number of throttled requests to send the notification.</param>
        /// <param name="isStatusCodeFromCreateConversation">A flag indicating if the status code is from a create conversation request.</param>
        /// <param name="statusCode">The final status code.</param>
        /// <param name="allSendStatusCodes">A comma separated list representing all of the status code responses received when trying
        /// to send the notification to the recipient.</param>
        /// <param name="errorMessage">The error message to store in the database.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ProcessResultDataAsync(
            string notificationId,
            string recipientId,
            int totalNumberOfSendThrottles,
            bool isStatusCodeFromCreateConversation,
            int statusCode,
            string allSendStatusCodes,
            string errorMessage,
            ILogger log)
        {
            try
            {
                // Storing this time before making the database call to have a time-stamp closer to when the notification
                // was sent.
                var currentDateTimeUtc = DateTime.UtcNow;

                var existingSentNotificationDataEntity = await this.sentNotificationDataRepository
                    .GetAsync(partitionKey: notificationId, rowKey: recipientId);

                // Set initial values.
                var allSendStatusCodesToStore = allSendStatusCodes;
                var numberOfFunctionAttemptsToSend = 1;

                // Replace the initial values if, for some reason, the message has already been sent/attempted.
                // When the initial row is set up, the status code is set to the InitializationStatusCode (likely 0).
                // Thus, if the status code is no longer the InitializationStatusCode (likely 0), then a notification
                // has already been sent/attempted for this recipient and a result has been stored. If this is the case,
                // then append the current result to the existing results.
                if (existingSentNotificationDataEntity != null
                    && existingSentNotificationDataEntity.StatusCode != SentNotificationDataEntity.InitializationStatusCode)
                {
                    allSendStatusCodesToStore
                        = $"{existingSentNotificationDataEntity.AllSendStatusCodes ?? string.Empty}{allSendStatusCodes}";
                    numberOfFunctionAttemptsToSend = existingSentNotificationDataEntity.NumberOfFunctionAttemptsToSend + 1;
                }

                var updatedSentNotificationDataEntity = new SentNotificationDataEntity
                {
                    PartitionKey = notificationId,
                    RowKey = recipientId,
                    RecipientId = recipientId,
                    TotalNumberOfSendThrottles = totalNumberOfSendThrottles,
                    SentDate = currentDateTimeUtc,
                    IsStatusCodeFromCreateConversation = isStatusCodeFromCreateConversation,
                    StatusCode = (int)statusCode,
                    ErrorMessage = errorMessage,
                    AllSendStatusCodes = allSendStatusCodesToStore,
                    NumberOfFunctionAttemptsToSend = numberOfFunctionAttemptsToSend,
                };

                if (statusCode == (int)HttpStatusCode.Created)
                {
                    updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Succeeded;
                }
                else if (statusCode == (int)HttpStatusCode.TooManyRequests)
                {
                    updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Throttled;
                }
                else if (statusCode == (int)HttpStatusCode.NotFound)
                {
                    updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.RecipientNotFound;
                }
                else if (statusCode == SentNotificationDataEntity.FaultedAndRetryingStatusCode)
                {
                    // This is a special case where an exception was thrown in the function.
                    // The system will try to add the queue message back to the queue and will try to
                    // send the notification again. For now, we will store the current state as retrying in
                    // the repository. If the system tries to send the notification repeatedly and reaches
                    // the dead letter maximum number of attempts, then the system should store a status of
                    // "Failed". In that case, the status code will not be faulted and retrying.
                    updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Retrying;
                }
                else
                {
                    updatedSentNotificationDataEntity.DeliveryStatus = SentNotificationDataEntity.Failed;
                }

                await this.sentNotificationDataRepository.InsertOrMergeAsync(updatedSentNotificationDataEntity);
            }
            catch (Exception e)
            {
                log.LogError(e, $"ERROR: {e.GetType()}: {e.Message}");
                throw;
            }
        }
    }
}
