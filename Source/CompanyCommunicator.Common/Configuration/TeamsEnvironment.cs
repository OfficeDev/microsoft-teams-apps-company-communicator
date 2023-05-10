namespace Microsoft.Teams.Apps.CompanyCommunicator.Common
{
    /// <summary>
    /// Teams environment.
    /// </summary>
    public enum TeamsEnvironment
    {
        /// <summary>
        /// Commmercial environment.
        /// </summary>
        Commercial,

        /// <summary>
        /// GCC - https://learn.microsoft.com/en-us/office365/servicedescriptions/office-365-platform-service-description/office-365-us-government/gcc
        /// </summary>
        GCC,

        /// <summary>
        /// GCCH - https://learn.microsoft.com/en-us/office365/servicedescriptions/office-365-platform-service-description/office-365-us-government/gcc-high-and-dod
        /// </summary>
        GCCH
    }
}
