namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration
{
    /// <summary>
    /// App configuration for GCCH environment.
    /// </summary>
    internal class GCCHConfiguration : IAppConfiguration
    {
        private readonly string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="GCCHConfiguration"/> class.
        /// </summary>
        /// <param name="tenantId">TenantId.</param>
        public GCCHConfiguration(string tenantId)
        {
            this.tenantId = tenantId ?? throw new System.ArgumentNullException(nameof(tenantId));
        }

        /// <inheritdoc/>
        public string AzureAd_Instance => "https://login.microsoftonline.us/";

        /// <inheritdoc/>
        public string AzureAd_ValidIssuers => $"https://login.microsoftonline.us/{this.tenantId}/v2.0,https://sts.windows.net/{this.tenantId}/";

        /// <inheritdoc/>
        public string AuthorityUri => $"https://login.microsoftonline.us/{this.tenantId}";

        /// <inheritdoc/>
        public string GraphBaseUrl => "https://graph.microsoft.us/v1.0";

        /// <inheritdoc/>
        public string GraphDefaultScope => "https://graph.microsoft.us/.default";

        /// <inheritdoc/>
        public string GraphUserReadScope => "https://graph.microsoft.us/User.Read openid profile";

        /// <inheritdoc/>
        public string TeamsLicenseId => "9953b155-8aef-4c56-92f3-72b0487fce41";
    }
}
