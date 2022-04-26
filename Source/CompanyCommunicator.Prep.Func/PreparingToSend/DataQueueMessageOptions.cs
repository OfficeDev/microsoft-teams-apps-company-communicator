﻿// <copyright file="DataQueueMessageOptions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend
{
    /// <summary>
    /// Options for data queue messages.
    /// </summary>
    public class DataQueueMessageOptions
    {
        /// <summary>
        /// Gets or sets the value for the delay to be applied to the data queue message.
        /// </summary>
        public int MessageDelayInSeconds { get; set; }
    }
}
