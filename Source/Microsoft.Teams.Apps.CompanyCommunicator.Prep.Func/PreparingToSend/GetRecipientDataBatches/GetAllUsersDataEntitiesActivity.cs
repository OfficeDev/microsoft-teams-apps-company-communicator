// <copyright file="GetAllUsersDataEntitiesActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This Activity represents the "get all users' data entity list" durable activity.
    /// It prepares the user data entity list for all users, which is used by the GetRecipientDataListForAllUsersActivity.
    ///
    /// The durable activity intends to fix the following issue:
    /// When the system is creating batches for the batch table, if something fails and that Azure Function retries itself
    /// AND in that amount of time the batches have changed (e.g. a new user is added to the data table), then the batches
    /// will fail to send because they will have more than 100 recipients in them.
    ///
    /// The durable activity gets user data entities for all users stored in the user data table.
    /// The Durable Function persists the activity's result, which is the users' data entity list, after the activity being
    /// executed successfully first time (for a specific notification).
    /// When retries happen, the activity will reuse the persisted data instead of retrieving it again from DB.
    ///
    /// We maintain idem-potency between retries by using the activity. So that the issue described above is solved.
    /// </summary>
    public class GetAllUsersDataEntitiesActivity
    {
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllUsersDataEntitiesActivity"/> class.
        /// </summary>
        /// <param name="userDataRepository">User Data repository.</param>
        public GetAllUsersDataEntitiesActivity(UserDataRepository userDataRepository)
        {
            this.userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="notificationDataEntityId">The notification data entity id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IEnumerable<UserDataEntity>> RunAsync(
            IDurableOrchestrationContext context,
            string notificationDataEntityId)
        {
            var recipientDataListInformation = await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                nameof(GetAllUsersDataEntitiesActivity.GetAllUsersAsync),
                ActivitySettings.CommonActivityRetryOptions,
                notificationDataEntityId);

            return recipientDataListInformation;
        }

        /// <summary>
        /// This method represents the "get all users' data entity list" durable activity.
        /// It gets the user data entities for all users stored in the user data table.
        /// </summary>
        /// <param name="notificationDataEntityId">The notification data entity id. It's used as the activity trigger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [FunctionName(nameof(GetAllUsersAsync))]
        public async Task<IEnumerable<UserDataEntity>> GetAllUsersAsync(
            [ActivityTrigger]
            string notificationDataEntityId)
        {
            return await this.userDataRepository.GetAllAsync();
        }
    }
}