using System;
using System.Collections.Generic;
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
        if (!context.InputParameters.Contains("dmi_transcript"))
        {
            throw new InvalidPluginExecutionException("Input parameter 'dmi_transcript' is required.");
        }

        var transcript = context.InputParameters["dmi_transcript"]?.ToString() ?? string.Empty;
        var promptTemplate = EnvironmentVariableService.GetRequiredValue(localContext.OrganizationService, "dmi_FirstContactPromptTemplate");
        var result = InferPriorityAndIntent(promptTemplate, transcript);

        context.OutputParameters["dmi_intent"] = result.Intent;
        context.OutputParameters["dmi_priority"] = result.PriorityOptionValue;
        context.OutputParameters["dmi_actions"] = result.RecommendedActions;

        localContext.TracingService.Trace("ProcessFirstContactSignalPlugin completed. Priority={0}, Intent={1}", result.PriorityLabel, result.Intent);
    }

    public static SignalInferenceResult InferPriorityAndIntent(string promptTemplate, string transcript)
    {
        // Placeholder rule-based logic for Sprint 1. This is replaced with Azure OpenAI managed identity callout in Sprint 2.
        var lowered = transcript.ToLowerInvariant();

        int priorityOptionValue;
        string priorityLabel;
        string intent;

        if (lowered.Contains("hostile") || lowered.Contains("attack"))
        {
            priorityOptionValue = PriorityHigh;
            priorityLabel = "High";
            intent = "Potentially hostile";
        }
        else if (lowered.Contains("distress") || lowered.Contains("emergency"))
        {
            priorityOptionValue = PriorityHigh;
            priorityLabel = "High";
            intent = "Distress signal";
        }
        else if (lowered.Contains("diplomatic") || lowered.Contains("alliance") || lowered.Contains("envoy"))
        {
            priorityOptionValue = PriorityMedium;
            priorityLabel = "Medium";
            intent = "Diplomatic contact";
        }
        else
        {
            priorityOptionValue = PriorityLow;
            priorityLabel = "Low";
            intent = "Unknown intent";
        }

        var actions = new List<string>
        {
            "Notify command immediately",
            "Open mission review board",
            "Attach transcript for analyst verification"
        };

        if (!string.IsNullOrWhiteSpace(promptTemplate))
        {
            actions.Add("Prompt profile applied: " + promptTemplate.Substring(0, Math.Min(promptTemplate.Length, 30)) + "...");
        }

        return new SignalInferenceResult(intent, priorityLabel, priorityOptionValue, string.Join("; ", actions));
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
