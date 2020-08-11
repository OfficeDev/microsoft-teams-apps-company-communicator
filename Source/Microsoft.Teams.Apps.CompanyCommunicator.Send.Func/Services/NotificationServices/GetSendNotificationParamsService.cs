// <copyright file="GetSendNotificationParamsService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.NotificationServices
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.SendQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.ConversationServices;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Services.DataServices;

    /// <summary>
    /// The service used to fetch or generate the necessary parameters for sending the notification.
    /// </summary>
    public class GetSendNotificationParamsService
    {
        private readonly int maxNumberOfAttempts;
        private readonly double sendRetryDelayNumberOfSeconds;
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly UserDataRepository userDataRepository;
        private readonly CreateUserConversationService createUserConversationService;
        private readonly DelaySendingNotificationService delaySendingNotificationService;
        private readonly ManageResultDataService manageResultDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetSendNotificationParamsService"/> class.
        /// </summary>
        /// <param name="companyCommunicatorSendFunctionOptions">The Company Communicator send function options.</param>
        /// <param name="sendingNotificationDataRepository">The sending notification data repository.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="createUserConversationService">The create user conversation service.</param>
        /// <param name="delaySendingNotificationService">The delay sending notification service.</param>
        /// <param name="manageResultDataService">The manage result data service.</param>
        public GetSendNotificationParamsService(
            IOptions<CompanyCommunicatorSendFunctionOptions> companyCommunicatorSendFunctionOptions,
            SendingNotificationDataRepository sendingNotificationDataRepository,
            UserDataRepository userDataRepository,
            CreateUserConversationService createUserConversationService,
            DelaySendingNotificationService delaySendingNotificationService,
            ManageResultDataService manageResultDataService)
        {
            this.maxNumberOfAttempts = companyCommunicatorSendFunctionOptions.Value.MaxNumberOfAttempts;
            this.sendRetryDelayNumberOfSeconds = companyCommunicatorSendFunctionOptions.Value.SendRetryDelayNumberOfSeconds;
            this.sendingNotificationDataRepository = sendingNotificationDataRepository;
            this.userDataRepository = userDataRepository;
            this.createUserConversationService = createUserConversationService;
            this.delaySendingNotificationService = delaySendingNotificationService;
            this.manageResultDataService = manageResultDataService;
        }

        /// <summary>
        /// The service used to fetch or generate the necessary parameters for sending the notification.
        /// </summary>
        /// <param name="messageContent">The message content of the send queue message.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<GetSendNotificationParamsResponse> GetSendNotificationParamsAsync(
            SendQueueMessageContent messageContent,
            ILogger log)
        {
            // Fetch the current sending notification. This is where the data about what is being sent is stored.
            var activeNotificationEntity = await this.sendingNotificationDataRepository.GetAsync(
                NotificationDataTableNames.SendingNotificationsPartition,
                messageContent.NotificationId);

            // Store the content for the notification that is to be sent.
            var getSendNotificationParamsResponse = new GetSendNotificationParamsResponse
            {
                NotificationContent = activeNotificationEntity.Content,
                ForceCloseAzureFunction = false,
            };

            // Depending on the recipient type and the data in the send queue message,
            // fetch or generate the recipient Id, service URL, and conversation Id to be used for
            // sending the notification.
            switch (messageContent.RecipientData.RecipientType)
            {
                case RecipientDataType.User:
                    await this.SetParamsForUserRecipientAsync(
                        getSendNotificationParamsResponse,
                        messageContent.RecipientData.UserData,
                        messageContent,
                        log);
                    break;

                case RecipientDataType.Team:
                    this.SetParamsForTeamRecipient(
                        getSendNotificationParamsResponse,
                        messageContent.RecipientData.TeamData);
                    break;

                default:
                    throw new ArgumentException($"Invalid recipient type: {messageContent.RecipientData.RecipientType}");
            }

            return getSendNotificationParamsResponse;
        }

        /// <summary>
        /// The main point of this is to set the recipient Id, service URL, and conversation Id for the send parameters.
        /// The recipient Id and service URL is always set in the incoming user data.
        ///
        /// For the conversation Id there are three possibilities:
        ///     Sending to all users - the conversationId is present in the incoming user data.
        ///     Sending to a team's members - the conversation Id was already stored for that user in the User Data table.
        ///         For this scenario, the incoming user data is stored back into the User Data table
        ///         because it may hold more information that was not present in the User Data table originally,
        ///         such as name
        ///     Sending to a team's members - the conversation Id was not stored for that user in the User Data table.
        /// </summary>
        /// <param name="getSendNotificationParamsResponse">The send notification parameters to be updated.</param>
        /// <param name="incomingUserDataEntity">The incoming user data entity.</param>
        /// <param name="messageContent">The queue message content.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SetParamsForUserRecipientAsync(
            GetSendNotificationParamsResponse getSendNotificationParamsResponse,
            UserDataEntity incomingUserDataEntity,
            SendQueueMessageContent messageContent,
            ILogger log)
        {
            try
            {
                // The AAD Id and service URL will always be set in the incoming user data.
                getSendNotificationParamsResponse.RecipientId = incomingUserDataEntity.AadId;
                getSendNotificationParamsResponse.ServiceUrl = incomingUserDataEntity.ServiceUrl;

                /*
                 * The rest of the logic in this method is primarily for fetching/generating the conversation Id.
                 *
                 */

                // Case where the conversation Id is included in the incoming user data.
                if (!string.IsNullOrWhiteSpace(incomingUserDataEntity.ConversationId))
                {
                    getSendNotificationParamsResponse.ConversationId = incomingUserDataEntity.ConversationId;

                    return;
                }

                // Case where the conversation Id is not included in the incoming user data (the conversation Id may or
                // may not be stored in the User Data table).

                // Fetch the data for that user from the User Data table to see if they are present.
                // It is possible that that user's data has not been stored in the User Data table.
                // If this is the case, then the conversation will need to be created for that user.
                var storedUserDataEntity = await this.userDataRepository.GetAsync(
                    UserDataTableNames.UserDataPartition,
                    incomingUserDataEntity.AadId);

                // These blocks are used to determine the conversation Id to be used when sending the notification.
                string conversationId = null;
                if (storedUserDataEntity != null)
                {
                    /*
                     * Case where the user's data was stored in the User Data table so their conversation Id is
                     * known. Update the user's entry in the User Data table, though, with the incoming user data
                     * because the incoming user data may hold more information than what is already stored for
                     * that user, such as their name.
                     */

                    // Set the conversation Id to be used when sending the notification.
                    conversationId = storedUserDataEntity.ConversationId;

                    // Set the conversation Id to ensure it is not removed from the User Data table on the update.
                    incomingUserDataEntity.ConversationId = storedUserDataEntity.ConversationId;

                    incomingUserDataEntity.PartitionKey = UserDataTableNames.UserDataPartition;
                    incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;

                    await this.userDataRepository.InsertOrMergeAsync(incomingUserDataEntity);
                }
                else
                {
                    /*
                     * Falling into this block means that the user data and the conversation Id for this user
                     * has not been stored. Because of this, the conversation needs to be created and that
                     * conversation Id needs to be stored for that user for later use.
                     */

                    var createConversationResponse = await this.createUserConversationService.CreateConversationAsync(
                        userDataEntity: incomingUserDataEntity,
                        maxNumberOfAttempts: this.maxNumberOfAttempts,
                        log: log);

                    if (createConversationResponse.ResultType == CreateUserConversationResultType.Succeeded)
                    {
                        // Set the conversation Id to be used when sending the notification.
                        conversationId = createConversationResponse.ConversationId;

                        // Store the newly created conversation Id so the create conversation
                        // request will not need to be made again for the user for future notifications.
                        incomingUserDataEntity.ConversationId = createConversationResponse.ConversationId;

                        incomingUserDataEntity.PartitionKey = UserDataTableNames.UserDataPartition;
                        incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;

                        await this.userDataRepository.InsertOrMergeAsync(incomingUserDataEntity);
                    }
                    else if (createConversationResponse.ResultType == CreateUserConversationResultType.Throttled)
                    {
                        // If the request was attempted the maximum number of allowed attempts and received
                        // all throttling responses, then set the overall delay time for the system so all
                        // other calls will be delayed and add the message back to the queue with a delay to be
                        // attempted later.
                        await this.delaySendingNotificationService.DelaySendingNotificationAsync(
                            sendRetryDelayNumberOfSeconds: this.sendRetryDelayNumberOfSeconds,
                            sendQueueMessageContent: messageContent,
                            log: log);

                        // Signal that the Azure Function should be completed to be attempted later.
                        getSendNotificationParamsResponse.ForceCloseAzureFunction = true;
                        return;
                    }
                    else if (createConversationResponse.ResultType == CreateUserConversationResultType.Failed)
                    {
                        // If the create conversation call failed, save the results, do not attempt the
                        // request again, and end the function.
                        await this.manageResultDataService.ProcessResultDataAsync(
                            notificationId: messageContent.NotificationId,
                            recipientId: incomingUserDataEntity.AadId,
                            totalNumberOfSendThrottles: 0,
                            isStatusCodeFromCreateConversation: true,
                            statusCode: createConversationResponse.StatusCode,
                            allSendStatusCodes: string.Empty,
                            errorMessage: createConversationResponse.ErrorMessage,
                            log: log);

                        // Signal that the Azure Function should be completed.
                        getSendNotificationParamsResponse.ForceCloseAzureFunction = true;
                        return;
                    }
                }

                // Set the conversation Id to be used when sending the notification.
                getSendNotificationParamsResponse.ConversationId = conversationId;
            }
            catch (Exception e)
            {
                var errorMessage = $"{e.GetType()}: {e.Message}";
                log.LogError(e, $"ERROR: {errorMessage}");

                throw;
            }
        }

        /// <summary>
        /// The main point of this is to set the recipient Id, service URL, and conversation Id for the send parameters.
        /// </summary>
        /// <param name="getSendNotificationParamsResponse">The send notification parameters to be updated.</param>
        /// <param name="incomingTeamDataEntity">The incoming team data entity.</param>
        private void SetParamsForTeamRecipient(
            GetSendNotificationParamsResponse getSendNotificationParamsResponse,
            TeamDataEntity incomingTeamDataEntity)
        {
            // All necessary parameters will always be set in the incoming team data.
            getSendNotificationParamsResponse.RecipientId = incomingTeamDataEntity.TeamId;
            getSendNotificationParamsResponse.ServiceUrl = incomingTeamDataEntity.ServiceUrl;
            getSendNotificationParamsResponse.ConversationId = incomingTeamDataEntity.TeamId;
        }
    }
}
