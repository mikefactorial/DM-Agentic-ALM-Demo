using Microsoft.Xrm.Sdk;

namespace DynamicMinds.Plugins.AgenticALMSample.Core;

public sealed class LocalPluginExecutionContext : ILocalPluginExecutionContext
{
    public LocalPluginExecutionContext(
        IPluginExecutionContext pluginExecutionContext,
        IOrganizationService organizationService,
        ITracingService tracingService,
        IManagedIdentityService managedIdentityService)
    {
        PluginExecutionContext = pluginExecutionContext;
        OrganizationService = organizationService;
        TracingService = tracingService;
        ManagedIdentityService = managedIdentityService;
    }

    public IPluginExecutionContext PluginExecutionContext { get; }

    public IOrganizationService OrganizationService { get; }

    public ITracingService TracingService { get; }

    public IManagedIdentityService ManagedIdentityService { get; }
}
