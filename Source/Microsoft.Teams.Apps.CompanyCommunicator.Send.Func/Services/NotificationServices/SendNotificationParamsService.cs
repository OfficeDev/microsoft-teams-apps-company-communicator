// <copyright file="SendNotificationParamsService.cs" company="Microsoft">
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
    public class SendNotificationParamsService
    {
        private readonly int maxNumberOfAttempts;
        private readonly double sendRetryDelayNumberOfSeconds;
        private readonly SendingNotificationDataRepository sendingNotificationDataRepository;
        private readonly UserDataRepository userDataRepository;
        private readonly ConversationService userConversationService;
        private readonly DelaySendingNotificationService delaySendingNotificationService;
        private readonly ManageResultDataService manageResultDataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendNotificationParamsService"/> class.
        /// </summary>
        /// <param name="options">The Company Communicator send function options.</param>
        /// <param name="sendingNotificationDataRepository">The sending notification data repository.</param>
        /// <param name="userDataRepository">The user data repository.</param>
        /// <param name="createUserConversationService">The create user conversation service.</param>
        /// <param name="delaySendingNotificationService">The delay sending notification service.</param>
        /// <param name="manageResultDataService">The manage result data service.</param>
        public SendNotificationParamsService(
            IOptions<SendFunctionOptions> options,
            SendingNotificationDataRepository sendingNotificationDataRepository,
            UserDataRepository userDataRepository,
            ConversationService createUserConversationService,
            DelaySendingNotificationService delaySendingNotificationService,
            ManageResultDataService manageResultDataService)
        {
            this.maxNumberOfAttempts = options.Value.MaxNumberOfAttempts;
            this.sendRetryDelayNumberOfSeconds = options.Value.SendRetryDelayNumberOfSeconds;
            this.sendingNotificationDataRepository = sendingNotificationDataRepository;
            this.userDataRepository = userDataRepository;
            this.userConversationService = createUserConversationService;
            this.delaySendingNotificationService = delaySendingNotificationService;
            this.manageResultDataService = manageResultDataService;
        }

        /// <summary>
        /// The service used to fetch or generate the necessary parameters for sending the notification.
        /// </summary>
        /// <param name="messageContent">The message content of the send queue message.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<SendNotificationParams> GetSendNotificationParamsAsync(
            SendQueueMessageContent messageContent,
            ILogger log)
        {
            // Fetch the current sending notification. This is where the data about what is being sent is stored.
            var activeNotificationEntity = await this.sendingNotificationDataRepository.GetAsync(
                NotificationDataTableNames.SendingNotificationsPartition,
                messageContent.NotificationId);

            // Store the content for the notification that is to be sent.
            var sendNotificationParams = new SendNotificationParams
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
                        sendNotificationParams,
                        messageContent.RecipientData.UserData,
                        messageContent,
                        log);
                    break;

                case RecipientDataType.Team:
                    this.SetParamsForTeamRecipient(
                        sendNotificationParams,
                        messageContent.RecipientData.TeamData);
                    break;

                default:
                    throw new ArgumentException($"Invalid recipient type: {messageContent.RecipientData.RecipientType}");
            }

            return sendNotificationParams;
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
        /// <param name="serviceNotificationParams">The send notification parameters to be updated.</param>
        /// <param name="incomingUserDataEntity">The incoming user data entity.</param>
        /// <param name="messageContent">The queue message content.</param>
        /// <param name="log">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SetParamsForUserRecipientAsync(
            SendNotificationParams serviceNotificationParams,
            UserDataEntity incomingUserDataEntity,
            SendQueueMessageContent messageContent,
            ILogger log)
        {
            // The AAD Id and service URL should always be set in the incoming user data.
            serviceNotificationParams.RecipientId = incomingUserDataEntity.AadId;
            serviceNotificationParams.ServiceUrl = incomingUserDataEntity.ServiceUrl;

            // Case where the conversation Id is included in the incoming user data.
            if (!string.IsNullOrWhiteSpace(incomingUserDataEntity.ConversationId))
            {
                serviceNotificationParams.ConversationId = incomingUserDataEntity.ConversationId;
                return;
            }

            // Check if user conversationId is stored.
            var storedUserDataEntity = await this.userDataRepository.GetAsync(
                UserDataTableNames.UserDataPartition,
                incomingUserDataEntity.AadId);

            if (storedUserDataEntity != null)
            {
                // Set the conversation Id to be used when sending the notification.
                serviceNotificationParams.ConversationId = storedUserDataEntity.ConversationId;

                // Merge incoming user data. (few properties may not be stored in the table - user name etc).
                incomingUserDataEntity.PartitionKey = UserDataTableNames.UserDataPartition;
                incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;
                await this.userDataRepository.InsertOrMergeAsync(incomingUserDataEntity);
                return;
            }

            try
            {
                // Create a conversation.
                var createConversationResponse = await this.userConversationService.CreateConversationAsync(
                    userDataEntity: incomingUserDataEntity,
                    maxNumberOfAttempts: this.maxNumberOfAttempts,
                    log: log);

                if (createConversationResponse.Result == Result.Succeeded)
                {
                    // Set the conversation Id to be used when sending the notification.
                    serviceNotificationParams.ConversationId = createConversationResponse.ConversationId;

                    // Store the newly created conversation Id so the create conversation
                    // request will not need to be made again for the user for future notifications.
                    incomingUserDataEntity.ConversationId = createConversationResponse.ConversationId;
                    incomingUserDataEntity.PartitionKey = UserDataTableNames.UserDataPartition;
                    incomingUserDataEntity.RowKey = incomingUserDataEntity.AadId;
                    await this.userDataRepository.InsertOrMergeAsync(incomingUserDataEntity);
                }
                else if (createConversationResponse.Result == Result.Throttled)
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
                    serviceNotificationParams.ForceCloseAzureFunction = true;
                    return;
                }
                else if (createConversationResponse.Result == Result.Failed)
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
                    serviceNotificationParams.ForceCloseAzureFunction = true;
                    return;
                }
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
        /// <param name="sendNotificationParams">The send notification parameters to be updated.</param>
        /// <param name="incomingTeamDataEntity">The incoming team data entity.</param>
        private void SetParamsForTeamRecipient(
            SendNotificationParams sendNotificationParams,
            TeamDataEntity incomingTeamDataEntity)
        {
            // All necessary parameters should always be set in the incoming team data.
            sendNotificationParams.RecipientId = incomingTeamDataEntity.TeamId;
            sendNotificationParams.ServiceUrl = incomingTeamDataEntity.ServiceUrl;
            sendNotificationParams.ConversationId = incomingTeamDataEntity.TeamId;
        }
    }
}
