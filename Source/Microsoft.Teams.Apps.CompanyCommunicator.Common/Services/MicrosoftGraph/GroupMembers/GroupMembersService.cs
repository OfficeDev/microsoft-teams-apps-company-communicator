// <copyright file="GroupMembersService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Group Members Service.
    /// This gets the groups transitive members.
    /// </summary>
    internal class GroupMembersService : IGroupMembersService
    {
        private readonly IGraphServiceClient graphServiceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupMembersService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">graph service client.</param>
        internal GroupMembersService(IGraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient ?? throw new ArgumentNullException(nameof(graphServiceClient));
        }

        /// <summary>
        /// get group members page by id.
        /// </summary>
        /// <param name="groupId">group id.</param>
        /// <returns>group members page.</returns>
        public async Task<IGroupTransitiveMembersCollectionWithReferencesPage> GetGroupMembersPageByIdAsync(string groupId)
        {
            return await this.graphServiceClient
                                    .Groups[groupId]
                                    .TransitiveMembers
                                    .Request()
                                    .Top(GraphConstants.MaxPageSize)
                                    .WithMaxRetry(GraphConstants.MaxRetry)
                                    .GetAsync();
        }

        /// <summary>
        /// get group members page by next page ur;.
        /// </summary>
        /// <param name="groupMembersRef">group members page reference.</param>
        /// <param name="nextPageUrl">group members next page data link url.</param>
        /// <returns>group members page.</returns>
        public async Task<IGroupTransitiveMembersCollectionWithReferencesPage> GetGroupMembersNextPageAsnyc(
            IGroupTransitiveMembersCollectionWithReferencesPage groupMembersRef,
            string nextPageUrl)
        {
            groupMembersRef.InitializeNextPageRequest(this.graphServiceClient, nextPageUrl);
            return await groupMembersRef
                .NextPageRequest
                .GetAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetGroupMembersAsync(string groupId)
        {
            var response = await this.graphServiceClient
                                    .Groups[groupId]
                                    .TransitiveMembers
                                    .Request()
                                    .Top(GraphConstants.MaxPageSize)
                                    .WithMaxRetry(GraphConstants.MaxRetry)
                                    .GetAsync();

            var users = response.OfType<User>().ToList();
            while (response.NextPageRequest != null)
            {
                response = await response.NextPageRequest.GetAsync();
                users?.AddRange(response.OfType<User>() ?? new List<User>());
            }

            return users;
        }
    }
}
