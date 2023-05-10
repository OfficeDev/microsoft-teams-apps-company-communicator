namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration
{
    /// <summary>
    /// App configuration for commercial environment.
    /// </summary>
    public class CommericalConfiguration : IAppConfiguration
    {
        private readonly string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommericalConfiguration"/> class.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        public CommericalConfiguration(string tenantId)
        {
            this.tenantId = tenantId ?? throw new System.ArgumentNullException(nameof(tenantId));
        }

        /// <inheritdoc/>
        public string AzureAd_Instance => "https://login.microsoftonline.com";

        /// <inheritdoc/>
        public string AzureAd_ValidIssuers => "https://login.microsoftonline.com/TENANT_ID/v2.0,https://sts.windows.net/TENANT_ID/";

        /// <inheritdoc/>
        public string AuthorityUri => $"https://login.microsoftonline.com/{this.tenantId}";

        /// <inheritdoc/>
        public string GraphBaseUrl => "https://graph.microsoft.com/v1.0";

        /// <inheritdoc/>
        public string GraphDefaultScope => "https://graph.microsoft.com/.default";

        /// <inheritdoc/>
        public string GraphUserReadScope => "https://graph.microsoft.com/User.Read openid profile";

        /// <inheritdoc/>
        public string TeamsLicenseId => "57ff2da0-773e-42df-b2af-ffb7a2317929";
    }
}
