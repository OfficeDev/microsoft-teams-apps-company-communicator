// <copyright file="UsersService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Users service.
    /// </summary>
    internal class UsersService : IUsersService
    {
        private const string TeamsLicenseId = "57ff2da0-773e-42df-b2af-ffb7a2317929";

        private readonly IGraphServiceClient graphServiceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">graph service client.</param>
        internal UsersService(IGraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetBatchByUserIds(IEnumerable<IEnumerable<string>> userIdsByGroups)
        {
            if (userIdsByGroups == null)
            {
                throw new ArgumentNullException(nameof(userIdsByGroups));
            }

            var users = new List<User>();
            var batches = this.GetBatchRequest(userIdsByGroups);
            foreach (var batchRequestContent in batches)
            {
                var response = await this.graphServiceClient
                    .Batch
                    .Request()
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .PostAsync(batchRequestContent);

                Dictionary<string, HttpResponseMessage> responses = await response.GetResponsesAsync();

                foreach (string key in responses.Keys)
                {
                    HttpResponseMessage httpResponse = default;
                    try
                    {
                        httpResponse = await response.GetResponseByIdAsync(key);
                        if (httpResponse == null)
                        {
                            throw new ArgumentNullException(nameof(httpResponse));
                        }

                        httpResponse.EnsureSuccessStatusCode();
                        var responseContent = await httpResponse.Content.ReadAsStringAsync();
                        JObject content = JObject.Parse(responseContent);
                        var userstemp = content["value"]
                            .Children()
                            .OfType<JObject>()
                            .Select(obj => obj.ToObject<User>());
                        if (userstemp == null)
                        {
                            continue;
                        }

                        users.AddRange(userstemp);
                    }
                    catch (HttpRequestException httpRequestException)
                    {
                        var error = new Error
                        {
                            Code = httpResponse.StatusCode.ToString(),
                            Message = httpResponse.ReasonPhrase,
                        };
                        throw new ServiceException(error, httpResponse.Headers, httpResponse.StatusCode, httpRequestException.InnerException);
                    }
                    finally
                    {
                        if (httpResponse != null)
                        {
                            httpResponse.Dispose();
                        }
                    }
                }
            }

            return users;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<IEnumerable<User>> GetUsersAsync(string filter = null)
        {
            var graphResult = await this.graphServiceClient
                    .Users
                    .Request()
                    .WithMaxRetry(GraphConstants.MaxRetry)
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

        /// <inheritdoc/>
        public async Task<User> GetUserAsync(string userId)
        {
            var graphResult = await this.graphServiceClient
                    .Users[userId]
                    .Request()
                    .Select(user => new
                    {
                        user.Id,
                        user.DisplayName,
                        user.UserPrincipalName,
                    })
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync();
            return graphResult;
        }

        /// <inheritdoc/>
        public async Task<(IEnumerable<User>, string)> GetAllUsersAsync(string deltaLink = null)
        {
            var users = new List<User>();
            IUserDeltaCollectionPage collectionPage;
            if (string.IsNullOrEmpty(deltaLink))
            {
                collectionPage = await this.graphServiceClient
                    .Users
                    .Delta()
                    .Request()
                    .Select("id, displayName, userPrincipalName, userType")
                    .Top(GraphConstants.MaxPageSize)
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync();
            }
            else
            {
                collectionPage = new UserDeltaCollectionPage();
                collectionPage.InitializeNextPageRequest(this.graphServiceClient, deltaLink);
                collectionPage = await collectionPage
                    .NextPageRequest
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync();
            }

            users.AddRange(collectionPage);

            while (collectionPage.NextPageRequest != null)
            {
                collectionPage = await collectionPage
                    .NextPageRequest
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync();

                users.AddRange(collectionPage);
            }

            collectionPage.AdditionalData.TryGetValue("@odata.deltaLink", out object delta);
            return (users, delta as string);
        }

        /// <inheritdoc/>
        public async Task<bool> HasTeamsLicenseAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var licenseCollection = await this.graphServiceClient
                .Users[userId]
                .LicenseDetails
                .Request()
                .Top(GraphConstants.MaxPageSize)
                .WithMaxRetry(GraphConstants.MaxRetry)
                .GetAsync();

            if (this.HasTeamsLicense(licenseCollection))
            {
                return true;
            }

            while (licenseCollection.NextPageRequest != null)
            {
                licenseCollection = await licenseCollection
                    .NextPageRequest
                    .WithMaxRetry(GraphConstants.MaxRetry)
                    .GetAsync();

                if (this.HasTeamsLicense(licenseCollection))
                {
                    return true;
                }
            }

            return false;
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
                if (userIds.Count() == 0)
                {
                    continue;
                }

                if (userIds.Count() > 15)
                {
                    throw new InvalidOperationException("The id count should be less than or equal to 15");
                }

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

            if (batchRequestContent.BatchRequestSteps.Count > 0 && batchRequestContent.BatchRequestSteps.Count < maxNoBatchItems)
            {
                batches.Add(batchRequestContent);
            }

            return batches;
        }

        private bool HasTeamsLicense(IUserLicenseDetailsCollectionPage licenseCollection)
        {
            foreach (var license in licenseCollection)
            {
                if (license.ServicePlans == null)
                {
                    continue;
                }

                if (license.ServicePlans.Any(sp => string.Equals(sp.ServicePlanId?.ToString(), TeamsLicenseId)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}