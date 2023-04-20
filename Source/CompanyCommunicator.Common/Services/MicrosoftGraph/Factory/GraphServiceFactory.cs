// <copyright file="GraphServiceFactory.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    using System;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration;

    /// <summary>
    /// Graph Service Factory.
    /// </summary>
    public class GraphServiceFactory : IGraphServiceFactory
    {
        private readonly IGraphServiceClient serviceClient;
        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphServiceFactory"/> class.
        /// </summary>
        /// <param name="serviceClient">V1 Graph service client.</param>
        /// <param name="appConfiguration">App configuration.</param>
        public GraphServiceFactory(
            IGraphServiceClient serviceClient,
            IAppConfiguration appConfiguration)
        {
            this.serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
        }

        /// <inheritdoc/>
        public IUsersService GetUsersService()
        {
            return new UsersService(this.serviceClient, this.appConfiguration);
        }

        /// <inheritdoc/>
        public IGroupsService GetGroupsService()
        {
            return new GroupsService(this.serviceClient);
        }

        /// <inheritdoc/>
        public IGroupMembersService GetGroupMembersService()
        {
            return new GroupMembersService(this.serviceClient);
        }

        /// <inheritdoc/>
        public IChatsService GetChatsService()
        {
            return new ChatsService(this.serviceClient, this.GetAppManagerService());
        }

        /// <inheritdoc/>
        public IAppManagerService GetAppManagerService()
        {
            return new AppManagerService(this.serviceClient, this.appConfiguration);
        }

        /// <inheritdoc/>
        public IAppCatalogService GetAppCatalogService()
        {
            return new AppCatalogService(this.serviceClient);
        }
    }
}
