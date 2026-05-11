using Microsoft.Xrm.Sdk;

namespace DynamicMinds.Plugins.AgenticALMSample.Core;

public interface ILocalPluginExecutionContext
{
    IPluginExecutionContext PluginExecutionContext { get; }

    IOrganizationService OrganizationService { get; }

    ITracingService TracingService { get; }
}
