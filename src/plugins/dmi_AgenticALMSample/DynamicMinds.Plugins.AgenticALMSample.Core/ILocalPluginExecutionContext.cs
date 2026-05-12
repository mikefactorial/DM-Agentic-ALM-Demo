using Microsoft.Xrm.Sdk;

namespace DynamicMinds.Plugins.AgenticALMSample.Core;

public interface ILocalPluginExecutionContext
{
    IPluginExecutionContext PluginExecutionContext { get; }

    IOrganizationService OrganizationService { get; }

    ITracingService TracingService { get; }

    /// <summary>
    /// Acquires an OAuth2 bearer token for the given resource using the managed identity
    /// configured in the Dataverse environment. Returns null if not available (e.g. unit tests).
    /// </summary>
    IManagedIdentityService ManagedIdentityService { get; }
}
