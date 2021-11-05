﻿// <copyright file="Constants.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common
{
    /// <summary>
    /// Constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// get the group read all scope.
        /// </summary>
        public const string ScopeGroupReadAll = "Group.Read.All";

        /// <summary>
        /// AppCatalog Read All scope.
        /// </summary>
        public const string ScopeAppCatalogReadAll = "AppCatalog.Read.All";

        /// <summary>
        /// get the user read scope.
        /// </summary>
        public const string ScopeUserRead = "User.Read";

        /// <summary>
        /// scope claim type.
        /// </summary>
        public const string ClaimTypeScp = "scp";

        /// <summary>
        /// authorization scheme.
        /// </summary>
        public const string BearerAuthorizationScheme = "Bearer";

        /// <summary>
        /// claim type user id.
        /// </summary>
        public const string ClaimTypeUserId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

        /// <summary>
        /// claim type tenant id.
        /// </summary>
        public const string ClaimTypeTenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// blob container name.
        /// </summary>
        public const string BlobContainerName = "exportdatablobs";

        /// <summary>
        /// get the group type Hidden Membership.
        /// </summary>
        public const string HiddenMembership = "HiddenMembership";

        /// <summary>
        /// get the header key for graph permission type.
        /// </summary>
        public const string PermissionTypeKey = "x-api-permission";

        /// <summary>
        /// get the default graph scope.
        /// </summary>
        public const string ScopeDefault = "https://graph.microsoft.com/.default";

        /// <summary>
        /// get the OData next page link.
        /// </summary>
        public const string ODataNextPageLink = "@odata.nextLink";

        /// <summary>
        /// get the maximum number of recipients in a batch.
        /// </summary>
        public const int MaximumNumberOfRecipientsInBatch = 1000;

        /// <summary>
        /// get the Microsoft Graph api batch request size.
        /// https://docs.microsoft.com/en-us/graph/known-issues#limit-on-batch-size.
        /// </summary>
        public const int MaximumGraphAPIBatchSize = 15;
    }
}
