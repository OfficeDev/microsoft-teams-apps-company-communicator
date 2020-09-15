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
        public const string SyncRecipientsOrchestrator = nameof(SyncRecipientsOrchestrator);

        /// <summary>
        /// Send queue orchestrator function.
        /// </summary>
        public const string SendQueueOrchestrator = nameof(SendQueueOrchestrator);

        /// <summary>
        /// Process and store message activity function.
        /// </summary>
        public const string StoreMessageActivity = nameof(StoreMessageActivity);

        /// <summary>
        /// Sync all users activity function.
        /// </summary>
        public const string SyncAllUsersActivity = nameof(SyncAllUsersActivity);

        /// <summary>
        /// Sync Team members acitivity function.
        /// </summary>
        public const string SyncTeamMembersActivity = nameof(SyncTeamMembersActivity);

        /// <summary>
        /// Sync group members acitivity function.
        /// </summary>
        public const string SyncGroupMembersActivity = nameof(SyncGroupMembersActivity);

        /// <summary>
        /// Sync Teams acitivity function.
        /// </summary>
        public const string SyncTeamsActivity = nameof(SyncTeamsActivity);

        /// <summary>
        /// Get recipients acitvity function.
        /// </summary>
        public const string GetRecipientsActivity = nameof(GetRecipientsActivity);

        /// <summary>
        /// Data aggregation activity function.
        /// </summary>
        public const string DataAggregationTriggerActivity = nameof(DataAggregationTriggerActivity);

        /// <summary>
        /// Send batch messages to send queue activity function.
        /// </summary>
        public const string SendBatchMessagesActivity = nameof(SendBatchMessagesActivity);

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
