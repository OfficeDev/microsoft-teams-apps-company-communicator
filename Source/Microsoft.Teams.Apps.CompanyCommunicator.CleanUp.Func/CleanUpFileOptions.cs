// <copyright file="CleanUpFileOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.CleanUp.Func
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Options used for holding clean up file number in days.
    /// </summary>
    public class CleanUpFileOptions
    {
        /// <summary>
        /// Gets or sets the clean up file number in days.
        /// </summary>
        public string CleanUpFile { get; set; }
    }
}
