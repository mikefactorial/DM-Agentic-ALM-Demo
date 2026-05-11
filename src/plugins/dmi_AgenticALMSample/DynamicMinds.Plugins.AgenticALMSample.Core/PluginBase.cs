using System;
using Microsoft.Xrm.Sdk;

namespace DynamicMinds.Plugins.AgenticALMSample.Core;

public abstract class PluginBase : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        var executionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
        var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
        var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

        if (executionContext == null || serviceFactory == null || tracingService == null)
        {
            throw new InvalidPluginExecutionException("Failed to resolve plugin services.");
        }

        var organizationService = serviceFactory.CreateOrganizationService(executionContext.UserId);
        var localContext = new LocalPluginExecutionContext(executionContext, organizationService, tracingService);

        ExecutePlugin(localContext);
    }

    protected abstract void ExecutePlugin(ILocalPluginExecutionContext localContext);
}
