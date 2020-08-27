// <copyright file="GetUserDataEntitiesByIdsActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// This class contains the "get user data entities by ids" durable activity.
    /// It retrieves user data entities by ids passed in parameters.
    /// </summary>
    public class GetUserDataEntitiesByIdsActivity
    {
        private readonly UserDataRepository userDataRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetUserDataEntitiesByIdsActivity"/> class.
        /// </summary>
        /// <param name="userDataRepository">User Data repository.</param>
        public GetUserDataEntitiesByIdsActivity(UserDataRepository userDataRepository)
        {
            this.userDataRepository = userDataRepository;
        }

        /// <summary>
        /// Run the activity.
        /// It retrieves user data entities by id in parallel using Fan-out / Fan-in pattern.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="userAadIds">User Aad Ids.</param>
        /// <param name="log">Logging service.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IEnumerable<UserDataEntity>> RunAsync(
            IDurableOrchestrationContext context,
            IEnumerable<string> userAadIds,
            ILogger log)
        {
            try
            {
                return await context.CallActivityWithRetryAsync<IEnumerable<UserDataEntity>>(
                         nameof(GetUserDataEntitiesByIdsActivity.GetUserDataEntitiesAsync),
                         ActivitySettings.CommonActivityRetryOptions,
                         userAadIds);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to load installed users {ex.Message}";

                log.LogError(ex, errorMessage);
                return null;
            }
        }

        /// <summary>
        /// This method represents the "get user data entity" durable activity.
        /// It gets installed user data.
        /// </summary>
        /// <param name="userAadIds">list of user Aad Id.</param>
        /// <returns>It returns the installed user data entity.</returns>
        [FunctionName(nameof(GetUserDataEntitiesAsync))]
        public async Task<IEnumerable<UserDataEntity>> GetUserDataEntitiesAsync(
           [ActivityTrigger] IEnumerable<string> userAadIds)
        {
            var userDataEntities = new List<UserDataEntity>();
            foreach (var aadId in userAadIds)
            {
                var userDataEntity = await this.userDataRepository.
                    GetAsync(UserDataTableNames.UserDataPartition, aadId);
                userDataEntities.Add(userDataEntity);
            }

            return userDataEntities;
        }
    }
}
