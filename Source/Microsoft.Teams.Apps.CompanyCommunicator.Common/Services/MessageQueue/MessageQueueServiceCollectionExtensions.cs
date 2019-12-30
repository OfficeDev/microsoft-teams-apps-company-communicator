// <copyright file="MessageQueueServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension class for registering Azure service bus queue services in DI container.
    /// </summary>
    public static class MessageQueueServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register message queue services in DI container.
        /// </summary>
        /// <param name="services">IServiceCollection instance.</param>
        public static void AddMessageQueue(this IServiceCollection services)
        {
            services.AddSingleton<DataQueue>();

            services.AddSingleton<SendQueue>();

            services.AddSingleton<PrepareToSendQueue>();
        }
    }
}