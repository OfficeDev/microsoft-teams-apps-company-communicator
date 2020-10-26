// <copyright file="GraphServiceFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MicrosoftGraph
{
    extern alias BetaLib;

    using System;
    using Microsoft.Graph;
    using Beta = BetaLib::Microsoft.Graph;

    /// <summary>
    /// Graph Service Factory.
    /// </summary>
    public class GraphServiceFactory : IGraphServiceFactory
    {
        private readonly Beta.IGraphServiceClient betaServiceClient;
        private readonly IGraphServiceClient serviceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphServiceFactory"/> class.
        /// </summary>
        /// <param name="betaServiceClient">Beta Graph service client.</param>
        /// <param name="serviceClient">V1 Graph service client.</param>
        public GraphServiceFactory(
            Beta.IGraphServiceClient betaServiceClient,
            IGraphServiceClient serviceClient)
        {
            this.betaServiceClient = betaServiceClient ?? throw new ArgumentNullException(nameof(betaServiceClient));
            this.serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        }

        /// <inheritdoc/>
        public IUsersService GetUsersService()
        {
            return new UsersService(this.serviceClient);
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
            return new ChatsService(this.betaServiceClient, this.GetAppManagerService());
        }

        /// <inheritdoc/>
        public IAppManagerService GetAppManagerService()
        {
            return new AppManagerService(this.betaServiceClient, this.serviceClient);
        }

        /// <inheritdoc/>
        public IAppCatalogService GetAppCatalogService()
        {
            return new AppCatalogService(this.betaServiceClient);
        }
    }
}
