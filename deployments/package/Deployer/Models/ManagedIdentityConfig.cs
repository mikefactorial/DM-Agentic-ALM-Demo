namespace PlatformPackage.Models
{
    /// <summary>
    /// Represents a managed identity configuration for a solution.
    /// </summary>
    public class ManagedIdentityConfig
    {
        /// <summary>
        /// The name of the managed identity as it appears in the solution (e.g., "ServiceBus Plugin").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The application (client) ID of the managed identity.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// The tenant ID where the managed identity is registered.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// The unique name of the solution this managed identity belongs to.
        /// </summary>
        public string SolutionName { get; set; }
    }
}
