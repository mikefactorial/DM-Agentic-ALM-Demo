using System;
using DynamicMinds.Plugins.AgenticALMSample.Core;
using Microsoft.Xrm.Sdk;

namespace DynamicMinds.Plugins.AgenticALMSample.FirstContact;

public sealed class ProcessFirstContactSignalPlugin : PluginBase
{
    public const int PriorityHigh = 100000000;
    public const int PriorityMedium = 100000001;
    public const int PriorityLow = 100000002;

    protected override void ExecutePlugin(ILocalPluginExecutionContext localContext)
    {
        var context = localContext.PluginExecutionContext;
        var target = (Entity)context.InputParameters["Target"];

        if (!target.Attributes.TryGetValue("dmi_signaltranscript", out var transcriptAttr))
        {
            localContext.TracingService.Trace("dmi_signaltranscript not present on Target — skipping.");
            return;
        }

        var transcript = transcriptAttr?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(transcript))
        {
            localContext.TracingService.Trace("dmi_signaltranscript is empty — skipping.");
            return;
        }

        localContext.TracingService.Trace("Reading environment variables for Azure OpenAI.");
        var endpoint = EnvironmentVariableService.GetRequiredValue(localContext.OrganizationService, "dmi_AzureOpenAIEndpoint");
        var deployment = EnvironmentVariableService.GetRequiredValue(localContext.OrganizationService, "dmi_AzureOpenAIDeployment");
        var tenantId = EnvironmentVariableService.GetRequiredValue(localContext.OrganizationService, "dmi_OpenAITenantId");
        var clientId = EnvironmentVariableService.GetRequiredValue(localContext.OrganizationService, "dmi_OpenAIClientId");
        var clientSecret = EnvironmentVariableService.GetRequiredValue(localContext.OrganizationService, "dmi_OpenAIClientSecret");
        var promptTemplate = EnvironmentVariableService.GetValue(localContext.OrganizationService, "dmi_FirstContactPromptTemplate") ?? string.Empty;

        localContext.TracingService.Trace("Calling Azure OpenAI deployment '{0}' at '{1}'.", deployment, endpoint);
        var openAI = new AzureOpenAIService(endpoint, deployment, tenantId, clientId, clientSecret);
        var result = openAI.AnalyseTranscript(promptTemplate, transcript);

        localContext.TracingService.Trace("Analysis complete. Intent={0}, Priority={1}.", result.Intent, result.PriorityLabel);

        var update = new Entity(target.LogicalName, target.Id)
        {
            ["dmi_intent"] = result.Intent,
            ["dmi_priority"] = new OptionSetValue(result.PriorityOptionValue),
            ["dmi_actions"] = result.RecommendedActions
        };
        localContext.OrganizationService.Update(update);
    }

    public sealed class SignalInferenceResult
    {
        public SignalInferenceResult(string intent, string priorityLabel, int priorityOptionValue, string recommendedActions)
        {
            Intent = intent;
            PriorityLabel = priorityLabel;
            PriorityOptionValue = priorityOptionValue;
            RecommendedActions = recommendedActions;
        }

        public string Intent { get; }

        public string PriorityLabel { get; }

        public int PriorityOptionValue { get; }

        public string RecommendedActions { get; }
    }
}
