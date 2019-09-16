// <copyright file="Activity1GetReceiverBatches.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func.DeliveryPretreatment.Activities
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.UserData;

    /// <summary>
    /// Get the message batches to be sent to Azure service bus queue activity.
    /// It's used by the durable function framework.
    /// </summary>
    public class Activity1GetReceiverBatches
    {
        private readonly MetadataProvider metadataProvider;
        private readonly TableRowKeyGenerator tableRowKeyGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Activity1GetReceiverBatches"/> class.
        /// </summary>
        /// <param name="metadataProvider">Metadata Provider instance.</param>
        /// <param name="tableRowKeyGenerator">Table row key generator service.</param>
        public Activity1GetReceiverBatches(
            MetadataProvider metadataProvider,
            TableRowKeyGenerator tableRowKeyGenerator)
        {
            this.metadataProvider = metadataProvider;
            this.tableRowKeyGenerator = tableRowKeyGenerator;
        }

        /// <summary>
        /// Run the activity.
        /// </summary>
        /// <param name="context">Durable orchestration context.</param>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<List<UserDataEntity>>> RunAsync(
            DurableOrchestrationContext context,
            NotificationDataEntity draftNotificationEntity)
        {
            var messageBatches = await context.CallActivityAsync<List<List<UserDataEntity>>>(
                nameof(Activity1GetReceiverBatches.GetReceiverBatchesAsync),
                draftNotificationEntity);

            context.SetCustomStatus(nameof(Activity1GetReceiverBatches.GetReceiverBatchesAsync));

            return messageBatches;
        }

        /// <summary>
        /// Get a notification's receiver data list.
        /// </summary>
        /// <param name="draftNotificationEntity">Draft notification entity.</param>
        /// <param name="log">Log service.</param>
        /// <returns>It returns the notification's audience data list.</returns>
        [FunctionName(nameof(GetReceiverBatchesAsync))]
        public async Task<List<List<UserDataEntity>>> GetReceiverBatchesAsync(
            [ActivityTrigger] NotificationDataEntity draftNotificationEntity,
            ILogger log)
        {
            var deduplicatedReceiverEntities =
                await this.metadataProvider.GetDeduplicatedReceiverEntitiesAsync(draftNotificationEntity, log);

            return this.CreateReceiverBatches(deduplicatedReceiverEntities);
        }

        private List<List<UserDataEntity>> CreateReceiverBatches(
            List<UserDataEntity> deduplicatedReceiverEntities)
        {
            var receiverBatches = new List<List<UserDataEntity>>();

            var totalNumberMessages = deduplicatedReceiverEntities.Count;
            var batchSize = 100;
            var numberOfCompleteBatches = totalNumberMessages / batchSize;
            var numberMessagesInIncompleteBatch = totalNumberMessages % batchSize;

            for (var i = 0; i < numberOfCompleteBatches; i++)
            {
                var startingIndex = i * batchSize;
                var batch = deduplicatedReceiverEntities.GetRange(startingIndex, batchSize);
                receiverBatches.Add(batch);
            }

            if (numberMessagesInIncompleteBatch != 0)
            {
                var incompleteBatchStartingIndex = numberOfCompleteBatches * batchSize;
                var incompleteBatch = deduplicatedReceiverEntities.GetRange(
                    incompleteBatchStartingIndex,
                    numberMessagesInIncompleteBatch);
                receiverBatches.Add(incompleteBatch);
            }

            return receiverBatches;
        }
    }
}