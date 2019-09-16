// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.Functions.Extensions.DependencyInjection.FunctionsStartup(
    typeof(Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.Startup))]

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.TeamData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment;
    using Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities;

    /// <summary>
    /// Register services in DI container of the Azure functions system.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<DeliveryPretreatmentOrchestration>();
            builder.Services.AddTransient<MetadataProvider>();
            builder.Services.AddTransient<AdaptiveCardCreator>();
            builder.Services.AddTransient<NotificationDataRepository>();
            builder.Services.AddTransient<SendingNotificationDataRepository>();
            builder.Services.AddTransient<UserDataRepository>();
            builder.Services.AddTransient<TeamDataRepository>();
            builder.Services.AddTransient<TableRowKeyGenerator>();
            builder.Services.AddTransient<SendQueue>();
            builder.Services.AddTransient<DataQueue>();
            builder.Services.AddTransient<Activity1GetReceiverBatches>();
            builder.Services.AddTransient<Activity2MoveDraftToSentNotificationPartition>();
            builder.Services.AddTransient<Activity3CreateSendingNotification>();
            builder.Services.AddTransient<Activity4SendTriggersToSendFunction>();
            builder.Services.AddTransient<Activity5SendTriggerToDataFunction>();
            builder.Services.AddTransient<Activity6CleanUp>();
        }
    }
}