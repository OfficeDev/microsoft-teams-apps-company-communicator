// <copyright file="OrchestrationStatusResponse.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Data.Func.Services.NotificationDataServices
{
    /// <summary>
    /// Orchestration Status response.
    /// </summary>
    public class OrchestrationStatusResponse
    {
        /// <summary>
        /// Gets or sets the orchestration name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Instance id of the orchestration.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the runtime status of the orchestration.
        /// </summary>
        public string RuntimeStatus { get; set; }
    }
}
