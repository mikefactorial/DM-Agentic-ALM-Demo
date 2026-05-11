using System;
using System.Collections.Generic;
using DynamicMinds.Plugins.AgenticALMSample.Core;
using Microsoft.Xrm.Sdk;

namespace DynamicMinds.Plugins.AgenticALMSample.FirstContact;

public sealed class ProcessFirstContactSignalPlugin : PluginBase
{
    protected override void ExecutePlugin(ILocalPluginExecutionContext localContext)
    {
        var context = localContext.PluginExecutionContext;
        if (!context.InputParameters.Contains("dmi_transcript"))
        {
            throw new InvalidPluginExecutionException("Input parameter 'dmi_transcript' is required.");
        }

        var transcript = context.InputParameters["dmi_transcript"]?.ToString() ?? string.Empty;
        var promptTemplate = EnvironmentVariableService.GetRequiredValue(localContext.OrganizationService, "dmi_FirstContactPromptTemplate");
        var result = InferPriorityAndIntent(promptTemplate, transcript);

        context.OutputParameters["dmi_intent"] = result.Intent;
        context.OutputParameters["dmi_priority"] = result.Priority;
        context.OutputParameters["dmi_actions"] = result.RecommendedActions;

        localContext.TracingService.Trace("ProcessFirstContactSignalPlugin completed. Priority={0}, Intent={1}", result.Priority, result.Intent);
    }

    private static SignalInferenceResult InferPriorityAndIntent(string promptTemplate, string transcript)
    {
        // Placeholder rule-based logic for Sprint 1. This is replaced with Azure OpenAI managed identity callout in Sprint 2.
        var lowered = transcript.ToLowerInvariant();
        var priority = lowered.Contains("distress") || lowered.Contains("hostile") ? "Critical" : "Normal";
        var intent = lowered.Contains("diplomatic") ? "Diplomatic contact" : "Unknown intent";

        var actions = new List<string>
        {
            "Open mission review board",
            "Attach transcript for analyst verification",
            "Escalate to command tier based on computed priority"
        };

        if (!string.IsNullOrWhiteSpace(promptTemplate))
        {
            actions.Add("Prompt profile applied: " + promptTemplate.Substring(0, Math.Min(promptTemplate.Length, 30)) + "...");
        }

        return new SignalInferenceResult(intent, priority, string.Join("; ", actions));
    }

    private sealed class SignalInferenceResult
    {
        public SignalInferenceResult(string intent, string priority, string recommendedActions)
        {
            Intent = intent;
            Priority = priority;
            RecommendedActions = recommendedActions;
        }

        public string Intent { get; }

        public string Priority { get; }

        public string RecommendedActions { get; }
    }
}
