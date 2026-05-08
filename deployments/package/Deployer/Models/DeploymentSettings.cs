using Newtonsoft.Json;
using System.Collections.Generic;

namespace PlatformPackage.Models
{
    /// <summary>
    /// Represents deployment settings for a solution, including environment variables and connection references.
    /// </summary>
    public class DeploymentSettings
    {
        [JsonProperty("EnvironmentVariables")]
        public List<EnvironmentVariable> EnvironmentVariables { get; set; }

        [JsonProperty("ConnectionReferences")]
        public List<ConnectionReference> ConnectionReferences { get; set; }
    }

    /// <summary>
    /// Represents a Dataverse environment variable to be configured during deployment.
    /// </summary>
    public class EnvironmentVariable
    {
        [JsonProperty("SchemaName")]
        public string SchemaName { get; set; }

        [JsonProperty("Value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Represents a connection reference to be configured during deployment.
    /// </summary>
    public class ConnectionReference
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("ConnectionId")]
        public string ConnectionId { get; set; }
    }
}
