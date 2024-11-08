namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Configuration
{
    /// <summary>
    /// Configuration factory returns relevant configuration for a given environment.
    /// </summary>
    public class ConfigurationFactory
    {
        private readonly string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationFactory"/> class.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        public ConfigurationFactory(string tenantId)
        {
            this.tenantId = tenantId ?? throw new System.ArgumentNullException(nameof(tenantId));
        }

        /// <summary>
        /// Configuration factory returns relevant configuration for a given environment.
        /// </summary>
        /// <param name="env">Teams environment.</param>
        /// <returns>App configurstion.</returns>
        public IAppConfiguration GetAppConfiguration(TeamsEnvironment env)
        {
            switch (env)
            {
                case TeamsEnvironment.Commercial:
                case TeamsEnvironment.GCC:
                    return new CommericalConfiguration(this.tenantId);
                case TeamsEnvironment.GCCH:
                    return new GCCHConfiguration(this.tenantId);
                case TeamsEnvironment.DOD:
                    return new DODConfiguration(this.tenantId);
                default:
                    return new CommericalConfiguration(this.tenantId);
            }
        }
    }
}
