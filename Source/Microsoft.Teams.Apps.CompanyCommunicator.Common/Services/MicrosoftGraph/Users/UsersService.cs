// <copyright file="UsersService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph.Users
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Get the User data.
    /// </summary>
    public class UsersService : IUsersService
    {
        private readonly IGraphServiceClient graphServiceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">graph service client.</param>
        public UsersService(IGraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }

        private int MaxRetry { get; set; } = 10;

        /// <summary>
        /// get list of users by ids.
        /// </summary>
        /// <param name="userIds">list of user ids.</param>
        /// <returns>list of users.</returns>
        public async Task<IEnumerable<User>> FilterByUserIdsAsync(IEnumerable<string> userIds)
        {
            try
            {
                if (userIds.Count() < 1)
                {
                    return default;
                }

                var filterUserIds = this.GetUserIdFilter(userIds);
                var userList = new List<User>();
                var usersStream = this.GetUsersAsync(filterUserIds.ToString());
                await foreach (var users in usersStream)
                {
                    userList.AddRange(users);
                }

                return userList;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// get the list of users by group of userids.
        /// </summary>
        /// <param name="userIdsByGroups">list of grouped user ids.</param>
        /// <returns>list of users.</returns>
        public async Task<IEnumerable<User>> GetBatchByUserIds(IEnumerable<IEnumerable<string>> userIdsByGroups)
        {
            if (userIdsByGroups.Count() < 1)
            {
                return default;
            }

            try
            {
                var users = new List<User>();
                var batches = this.GetBatchRequest(userIdsByGroups);
                foreach (var batchRequestContent in batches)
                {
                    var response = await this.graphServiceClient
                        .Batch
                        .Request()
                        .WithMaxRetry(this.MaxRetry)
                        .PostAsync(batchRequestContent);

                    Dictionary<string, HttpResponseMessage> responses = await response.GetResponsesAsync();

                    foreach (string key in responses.Keys)
                    {
                        HttpResponseMessage httpResponse = await response.GetResponseByIdAsync(key);
                        httpResponse.EnsureSuccessStatusCode();

                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        JObject content = JObject.Parse(responseContent);
                        var userstemp = content["value"]
                            .Children()
                            .OfType<JObject>()
                            .Select(obj => obj.ToObject<User>());
                        users.AddRange(userstemp);
                    }
                }

                return users;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// get the stream of users.
        /// </summary>
        /// <param name="filter">the filter condition.</param>
        /// <returns>stream of users.</returns>
        public async IAsyncEnumerable<IEnumerable<User>> GetUsersAsync(string filter = null)
        {
            var graphResult = await this.graphServiceClient
                    .Users
                    .Request()
                    .WithMaxRetry(this.MaxRetry)
                    .Filter(filter)
                    .Select(user => new
                    {
                        user.Id,
                        user.DisplayName,
                        user.UserPrincipalName,
                    })
                    .GetAsync();
            yield return graphResult.CurrentPage;
            while (graphResult.NextPageRequest != null)
            {
                graphResult = await graphResult.NextPageRequest.GetAsync();
                yield return graphResult.CurrentPage;
            }
        }

        /// <summary>
        /// get user by id.
        /// </summary>
        /// <param name="userId">the user id.</param>
        /// <returns>user data.</returns>
        public async Task<User> GetUserAsync(string userId)
        {
            try
            {
                var graphResult = await this.graphServiceClient
                        .Users[userId]
                        .Request()
                        .WithMaxRetry(this.MaxRetry)
                        .Select(user => new
                        {
                            user.Id,
                            user.DisplayName,
                            user.UserPrincipalName,
                        })
                        .GetAsync();
                return graphResult;
            }
            catch
            {
                return default;
            }
        }

        private string GetUserIdFilter(IEnumerable<string> userIds)
        {
            StringBuilder filterUserIds = new StringBuilder();
            foreach (var id in userIds)
            {
                if (!string.IsNullOrEmpty(filterUserIds.ToString()))
                {
                    filterUserIds.Append(" or ");
                }

                filterUserIds.Append($"id eq '{id}'");
            }

            return filterUserIds.ToString();
        }

        private IEnumerable<BatchRequestContent> GetBatchRequest(IEnumerable<IEnumerable<string>> userIdsByGroups)
        {
            var batches = new List<BatchRequestContent>();
            int maxNoBatchItems = 20;

            var batchRequestContent = new BatchRequestContent();
            int requestId = 1;

            foreach (var userIds in userIdsByGroups)
            {
                var filterUserIds = this.GetUserIdFilter(userIds);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/users?$filter={filterUserIds}&$select=id,displayName,userPrincipalName");
                batchRequestContent.AddBatchRequestStep(new BatchRequestStep(requestId.ToString(), httpRequestMessage));

                if (batchRequestContent.BatchRequestSteps.Count() % maxNoBatchItems == 0)
                {
                    batches.Add(batchRequestContent);
                    batchRequestContent = new BatchRequestContent();
                }

                requestId++;
            }

            if (batchRequestContent.BatchRequestSteps.Count < maxNoBatchItems)
            {
                batches.Add(batchRequestContent);
            }

            return batches;
        }
    }
}