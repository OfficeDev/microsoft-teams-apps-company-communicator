// <copyright file="RecipientDataListInformation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Prep.Func.PreparingToSend.GetRecipientDataBatches
{
    /// <summary>
    /// Class to hold the recipient data list information.
    /// </summary>
    public class RecipientDataListInformation
    {
        /// <summary>
        /// Gets or sets the total number of recipients.
        /// </summary>
        public int TotalNumberOfRecipients { get; set; }

        /// <summary>
        /// Gets or sets the number of recipient data batches.
        /// </summary>
        public int NumberOfRecipientDataBatches { get; set; }
    }
}
