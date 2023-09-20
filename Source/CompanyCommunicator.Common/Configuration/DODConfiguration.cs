namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration
{
    /// <summary>
    /// App configuration for DOD environment.
    /// </summary>
    internal class DODConfiguration : IAppConfiguration
    {
        private readonly string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="DODConfiguration"/> class.
        /// </summary>
        /// <param name="tenantId">TenantId.</param>
        public DODConfiguration(string tenantId)
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
        public string GraphBaseUrl => "https://dod-graph.microsoft.us/v1.0";

        /// <inheritdoc/>
        public string GraphDefaultScope => "https://dod-graph.microsoft.us/.default";

        /// <inheritdoc/>
        public string GraphUserReadScope => "https://dod-graph.microsoft.us/User.Read openid profile";

        /// <inheritdoc/>
        public string TeamsLicenseId => "fd500458-c24c-478e-856c-a6067a8376cd";
    }
}
