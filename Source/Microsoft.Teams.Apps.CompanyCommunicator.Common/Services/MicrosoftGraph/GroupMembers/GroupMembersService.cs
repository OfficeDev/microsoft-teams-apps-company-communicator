// <copyright file="GroupMembersService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph.GroupMembers
{
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Group Members Service.
    /// This gets the groups transitive members.
    /// </summary>
    public class GroupMembersService : IGroupMembersService
    {
        private readonly IGraphServiceClient graphServiceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupMembersService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">graph service client.</param>
        public GroupMembersService(IGraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }

        private int MaxResultCount { get; set; } = 999;

        private int MaxRetry { get; set; } = 10;

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
                                    .Top(this.MaxResultCount)
                                    .WithMaxRetry(this.MaxRetry)
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
    }
}
