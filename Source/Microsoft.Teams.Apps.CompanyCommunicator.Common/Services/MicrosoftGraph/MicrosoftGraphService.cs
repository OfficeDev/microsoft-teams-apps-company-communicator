// <copyright file="MicrosoftGraphService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Microsoft Graph Service.
    /// </summary>
    public class MicrosoftGraphService : IMicrosoftGraphService
    {
        private readonly IGraphServiceClient graphServiceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftGraphService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">graph service client.</param>
        public MicrosoftGraphService(IGraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }

        /// <summary>
        /// get the group by ids.
        /// </summary>
        /// <param name="groupIds">list of group ids.</param>
        /// <returns>list of groups.</returns>
        public async Task<IEnumerable<Group>> GetGroupByIds(List<string> groupIds)
        {
            var groups = new List<Group>();
            foreach (var id in groupIds)
            {
                var group = await this.graphServiceClient
                                .Groups[id]
                                .Request()
                                .Select(gr => new { gr.Id, gr.Mail })
                                .GetAsync();
                groups.Add(group);
            }

            return groups;
        }

        /// <summary>
        /// Search groups based on query.
        /// </summary>
        /// <param name="query">query param.</param>
        /// <returns>list of group.</returns>
        public async Task<IEnumerable<Group>> SearchGroups(string query)
        {
            string filter = $"mailEnabled eq true and securityEnabled eq false and startsWith(mail,'{query}')";
            var groupsPaged = await this.graphServiceClient
                                  .Groups
                                  .Request()
                                  .Filter(filter)
                                  .Select(group => new
                                  {
                                      group.Id,
                                      group.Mail,
                                      group.GroupTypes,
                                      group.MailEnabled,
                                      group.SecurityEnabled,
                                  }).
                                  Top(4)
                                  .GetAsync();

            return groupsPaged.CurrentPage;
        }
    }
}
