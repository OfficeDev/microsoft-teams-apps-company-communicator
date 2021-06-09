// <copyright file="ICertificateProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.CommonBot
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// This instance is used to retrieve certificates.
    /// </summary>
    public interface ICertificateProvider
    {
        /// <summary>
        /// Gets the certificate for the given app id.
        /// </summary>
        /// <param name="appId">The Azuer active directory Identifier.</param>
        /// <returns>Certificate.</returns>
        X509Certificate2 GetCertificate(string appId);

        /// <summary>
        /// Checks if authentication is to be done using certificate.
        /// </summary>
        /// <returns>Boolean indicating if authentication type is certifcate.</returns>
        bool IsCertificateAuthenticationEnabled();
    }
}
