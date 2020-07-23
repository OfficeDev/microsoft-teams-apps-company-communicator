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
        public async Task<IEnumerable<User>> GetBatchByUserIds(IEnumerable<List<string>> userIdsByGroups)
        {
            if (userIdsByGroups.Count() < 1)
            {
                return default;
            }

            try
            {
                var batchRequestContent = this.GetBatchRequest(userIdsByGroups);
                var response = await this.graphServiceClient.Batch.Request().PostAsync(batchRequestContent);
                Dictionary<string, HttpResponseMessage> responses = await response.GetResponsesAsync();
                var users = new List<User>();
                foreach (string key in responses.Keys)
                {
                    HttpResponseMessage httpResponse = await response.GetResponseByIdAsync(key);
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<User>(responseContent);
                    JObject content = JObject.Parse(responseContent);
                    var test = content["value"]
                        .Children()
                        .OfType<JObject>()
                        .Select(obj => obj.ToObject<User>());
                    users.AddRange(test);
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
                    .WithMaxRetry(10)
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

        private BatchRequestContent GetBatchRequest(IEnumerable<List<string>> userIdsByGroups)
        {
            var batchRequestContent = new BatchRequestContent();
            int requestId = 1;
            foreach (var userIds in userIdsByGroups)
            {
                var filterUserIds = this.GetUserIdFilter(userIds);
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://graph.microsoft.com/v1.0/users?$filters={filterUserIds}");
                batchRequestContent.AddBatchRequestStep(new BatchRequestStep(requestId.ToString(), httpRequestMessage));
                requestId++;
            }

            return batchRequestContent;
        }
    }
}