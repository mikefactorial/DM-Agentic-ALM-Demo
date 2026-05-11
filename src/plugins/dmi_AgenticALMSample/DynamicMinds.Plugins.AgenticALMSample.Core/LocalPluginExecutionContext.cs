using Microsoft.Xrm.Sdk;

namespace DynamicMinds.Plugins.AgenticALMSample.Core;

public sealed class LocalPluginExecutionContext : ILocalPluginExecutionContext
{
    public LocalPluginExecutionContext(
        IPluginExecutionContext pluginExecutionContext,
        IOrganizationService organizationService,
        ITracingService tracingService)
    {
        PluginExecutionContext = pluginExecutionContext;
        OrganizationService = organizationService;
        TracingService = tracingService;
    }

    public IPluginExecutionContext PluginExecutionContext { get; }

    public IOrganizationService OrganizationService { get; }

    public ITracingService TracingService { get; }
}
