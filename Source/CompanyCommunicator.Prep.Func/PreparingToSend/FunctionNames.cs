﻿// <copyright file="FunctionNames.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    /// <summary>
    /// Defines constants for function names.
    /// </summary>
    public static class FunctionNames
    {
        /// <summary>
        /// Prepare to send function.
        /// </summary>
        public const string PrepareToSendFunction = nameof(PrepareToSendFunction);

        /// <summary>
        /// Prepare to send orchestrator function.
        /// </summary>
        public const string PrepareToSendOrchestrator = nameof(PrepareToSendOrchestrator);

        /// <summary>
        /// Sync recipients orchestrator function.
        /// </summary>
        public const string SyncRecipientsOrchestrator = nameof(SyncRecipientsOrchestrator);

        /// <summary>
        /// Teams conversation orchestrator.
        /// </summary>
        public const string TeamsConversationOrchestrator = nameof(TeamsConversationOrchestrator);

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
        /// Sync Team members activity function.
        /// </summary>
        public const string SyncTeamMembersActivity = nameof(SyncTeamMembersActivity);

        /// <summary>
        /// Sync group members activity function.
        /// </summary>
        public const string SyncGroupMembersActivity = nameof(SyncGroupMembersActivity);

        /// <summary>
        /// Sync Teams activity function.
        /// </summary>
        public const string SyncTeamsActivity = nameof(SyncTeamsActivity);

        /// <summary>
        /// Get recipients activity function.
        /// </summary>
        public const string GetRecipientsActivity = nameof(GetRecipientsActivity);

        /// <summary>
        /// Batch recipients activity function.
        /// </summary>
        public const string BatchRecipientsActivity = nameof(BatchRecipientsActivity);

        /// <summary>
        /// Get pending recipients (ie recipients with no conversation id in the database) activity function.
        /// </summary>
        public const string GetPendingRecipientsActivity = nameof(GetPendingRecipientsActivity);

        /// <summary>
        /// Teams conversation activity function.
        /// </summary>
        public const string TeamsConversationActivity = nameof(TeamsConversationActivity);

        /// <summary>
        /// Data aggregation activity function.
        /// </summary>
        public const string DataAggregationTriggerActivity = nameof(DataAggregationTriggerActivity);

        /// <summary>
        /// Update notification activity function.
        /// </summary>
        public const string UpdateNotificationStatusActivity = nameof(UpdateNotificationStatusActivity);

        /// <summary>
        /// Send batch messages to send queue activity function.
        /// </summary>
        public const string SendBatchMessagesActivity = nameof(SendBatchMessagesActivity);

        /// <summary>
        /// Handle failure activity function.
        /// </summary>
        public const string HandleFailureActivity = nameof(HandleFailureActivity);

        /// <summary>
        /// Upload activity function.
        /// </summary>
        public const string UploadActivity = nameof(UploadActivity);

        /// <summary>
        /// Send file card activity function.
        /// </summary>
        public const string SendFileCardActivity = nameof(SendFileCardActivity);

        /// <summary>
        /// Get metadata activity function.
        /// </summary>
        public const string GetMetadataActivity = nameof(GetMetadataActivity);

        /// <summary>
        /// Update export data activity function.
        /// </summary>
        public const string UpdateExportDataActivity = nameof(UpdateExportDataActivity);

        /// <summary>
        /// Handle export failure activity function.
        /// </summary>
        public const string HandleExportFailureActivity = nameof(HandleExportFailureActivity);

        /// <summary>
        /// Export orchestration function.
        /// </summary>
        public const string ExportOrchestration = nameof(ExportOrchestration);
    }
}
