// <copyright file="FunctionNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    /// <summary>
    /// Defines constants for function names.
    /// </summary>
    public static class FunctionNames
    {
        /// <summary>
        /// Process recipients orchestrator function.
        /// </summary>
        public const string ProcessRecipientsOrchestrator = nameof(ProcessRecipientsOrchestrator);

        /// <summary>
        /// Send queue orchestrator function.
        /// </summary>
        public const string SendQueueOrchestrator = nameof(SendQueueOrchestrator);

        /// <summary>
        /// Process and store message activity function.
        /// </summary>
        public const string ProcessAndStoreMessageActivity = nameof(ProcessAndStoreMessageActivity);

        /// <summary>
        /// Update notification activity function.
        /// </summary>
        public const string UpdateNotificationActivity = nameof(UpdateNotificationActivity);

        /// <summary>
        /// Data aggregation activity function.
        /// </summary>
        public const string DataAggregationActivity = nameof(DataAggregationActivity);

        /// <summary>
        /// Send batch messages to send queue activity function.
        /// </summary>
        public const string SendBatchMessagesActivity = nameof(SendBatchMessagesActivity);

        /// <summary>
        /// Get Teams Entities by Ids activity function.
        /// </summary>
        public const string GetTeamDataEntitiesByIdsActivity = nameof(GetTeamDataEntitiesByIdsActivity);

        /// <summary>
        /// Get Team recipient data list activity function.
        /// </summary>
        public const string GetTeamRecipientDataListActivity = nameof(GetTeamRecipientDataListActivity);

        /// <summary>
        /// Handle failure activity function.
        /// </summary>
        public const string HandleFailureActivity = nameof(HandleFailureActivity);

        /// <summary>
        /// Handle warning activity function.
        /// </summary>
        public const string HandleWarningActivity = nameof(HandleWarningActivity);
    }
}
