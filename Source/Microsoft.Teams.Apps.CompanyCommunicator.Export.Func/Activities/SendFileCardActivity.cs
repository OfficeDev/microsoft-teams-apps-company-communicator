// <copyright file="SendFileCardActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Export.Func.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot;

    /// <summary>
    /// sends the file card.
    /// </summary>
    public class SendFileCardActivity
    {
        private readonly string microsoftAppId;
        private readonly BotFrameworkHttpAdapter botAdapter;
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendFileCardActivity"/> class.
        /// </summary>
        /// <param name="botOptions">the bot options.</param>
        /// <param name="botAdapter">the users service.</param>
        /// <param name="userDataRepository">the user data repository.</param>
        public SendFileCardActivity(
            IOptions<BotOptions> botOptions,
            BotFrameworkHttpAdapter botAdapter,
            UserDataRepository userDataRepository)
        {
            this.botAdapter = botAdapter;
            this.microsoftAppId = botOptions.Value.MicrosoftAppId;
            this.userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// It sends the file card.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="sendData">Tuple containing user id, notification data entity and export data entity.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>responsse of send file card acitivy.</returns>
        public async Task<SendNotificationResponse> RunAsync(
        IDurableOrchestrationContext context,
        (string userId, string notificationId, string fileName) sendData,
        ILogger log)
        {
            var response = await context.CallActivityWithRetryAsync<SendNotificationResponse>(
              nameof(SendFileCardActivity.SendFileCardActivityAsync),
              ActivitySettings.CommonActivityRetryOptions,
              sendData);
            return response;
        }

        /// <summary>
        /// send the file card to the user.
        /// </summary>
        /// <param name="sendData">Tuple containing user id, notification id and file name.</param>
        /// <returns>file card response.</returns>
        [FunctionName(nameof(SendFileCardActivityAsync))]
        public async Task<SendNotificationResponse> SendFileCardActivityAsync(
        [ActivityTrigger](string userId, string notificationId, string fileName) sendData)
        {
            var sendNotificationResponse = new SendNotificationResponse
            {
                TotalNumberOfSendThrottles = 0,
                AllSendStatusCodes = string.Empty,
            };

            var user = await this.userDataRepository.GetAsync(UserDataTableNames.UserDataPartition, sendData.userId);

            // Set the service URL in the trusted list to ensure the SDK includes the token in the request.
            MicrosoftAppCredentials.TrustServiceUrl(user.ServiceUrl);

            var conversationReference = new ConversationReference
            {
                ServiceUrl = user.ServiceUrl,
                Conversation = new ConversationAccount
                {
                    Id = user.ConversationId,
                },
            };

            int maxNumberOfAttempts = 100;
            await this.botAdapter.ContinueConversationAsync(
               botAppId: this.microsoftAppId,
               reference: conversationReference,
               callback: async (turnContext, cancellationToken) =>
               {
                   var consentContext = new Dictionary<string, string>
                   {
                       { "filename", sendData.fileName },
                       { "notificationId", sendData.notificationId },
                   };

                   var fileCard = new FileConsentCard
                   {
                       Description = "This is the file Bot wants to send you",
                       AcceptContext = consentContext,
                       DeclineContext = consentContext,
                   };

                   var asAttachment = new Attachment
                   {
                       Content = fileCard,
                       ContentType = FileConsentCard.ContentType,
                       Name = sendData.fileName,
                   };

                   var message = MessageFactory.Attachment(asAttachment);

                   // Loop through attempts to try and send the notification.
                   for (int i = 1; i <= maxNumberOfAttempts; i++)
                   {
                       try
                       {
                           var response = await turnContext.SendActivityAsync(message, cancellationToken);

                           // If made it passed the sending step, then the notification was sent successfully.
                           // Store the data about the successful request.
                           sendNotificationResponse.ResponseId = response.Id;
                           sendNotificationResponse.ResultType = SendNotificationResultType.Succeeded;
                           sendNotificationResponse.StatusCode = (int)HttpStatusCode.Created;
                           sendNotificationResponse.AllSendStatusCodes += $"{(int)HttpStatusCode.Created},";

                           break;
                       }
                       catch (ErrorResponseException e)
                       {
                           var responseStatusCode = e.Response.StatusCode;
                           sendNotificationResponse.StatusCode = (int)responseStatusCode;
                           sendNotificationResponse.AllSendStatusCodes += $"{(int)responseStatusCode},";

                           // If the response was a throttled status code or a 5xx status code,
                           // then delay and retry the request.
                           if (responseStatusCode == HttpStatusCode.TooManyRequests
                              || ((int)responseStatusCode >= 500 && (int)responseStatusCode < 600))
                           {
                               if (responseStatusCode == HttpStatusCode.TooManyRequests)
                               {
                                   // If the request was throttled, set the flag for indicating the throttled state and
                                   // increment the count of the number of throttles to be stored later.
                                   sendNotificationResponse.ResultType = SendNotificationResultType.Throttled;
                                   sendNotificationResponse.TotalNumberOfSendThrottles++;
                               }
                               else
                               {
                                   // If the request failed with a 5xx status code, set the flag for indicating the failure
                                   // and store the content of the error message.
                                   sendNotificationResponse.ResultType = SendNotificationResultType.Failed;
                                   sendNotificationResponse.ErrorMessage = e.Response.Content;
                               }

                               // If the maximum number of attempts has not been reached, delay
                               // for a bit of time to attempt the request again.
                               // Do not delay if already attempted the maximum number of attempts.
                               if (i < maxNumberOfAttempts)
                               {
                                   var random = new Random();
                                   await Task.Delay(random.Next(500, 1500));
                               }
                           }
                           else
                           {
                               // If in this block, then an error has occurred with the service.
                               // Return the failure and do not attempt the request again.
                               sendNotificationResponse.ResultType = SendNotificationResultType.Failed;
                               sendNotificationResponse.ErrorMessage = e.Response.Content;

                               break;
                           }
                       }
                   }
               },
               cancellationToken: CancellationToken.None);
            return sendNotificationResponse;
        }
    }
}
