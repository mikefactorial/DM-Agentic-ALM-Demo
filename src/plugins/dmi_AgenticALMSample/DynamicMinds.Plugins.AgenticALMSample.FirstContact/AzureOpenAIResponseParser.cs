using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DynamicMinds.Plugins.AgenticALMSample.FirstContact;

/// <summary>
/// Parses Azure OpenAI Chat Completions API responses into a <see cref="ProcessFirstContactSignalPlugin.SignalInferenceResult"/>.
/// Extracted from <see cref="AzureOpenAIService"/> to allow unit testing without HTTP calls.
/// </summary>
public static class AzureOpenAIResponseParser
{
    /// <summary>
    /// Parses a Chat Completions API JSON response and returns a structured inference result.
    /// </summary>
    /// <param name="completionsJson">Raw JSON string returned by the Chat Completions endpoint.</param>
    /// <exception cref="InvalidOperationException">Thrown when the response lacks the expected content structure or the content is not valid JSON.</exception>
    public static ProcessFirstContactSignalPlugin.SignalInferenceResult Parse(string completionsJson)
    {
        var root = JObject.Parse(completionsJson);
        var content = root["choices"]?[0]?["message"]?["content"]?.ToString()
            ?? throw new InvalidOperationException("Azure OpenAI response did not contain expected content.");

        JObject parsed;
        try
        {
            parsed = JObject.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Azure OpenAI returned non-JSON content: {content}", ex);
        }

        var intent = parsed["intent"]?.ToString() ?? "Unknown intent";
        var priorityStr = parsed["priority"]?.ToString() ?? "Low";
        var actionsArray = parsed["actions"] as JArray;
        var actions = actionsArray != null
            ? string.Join("; ", actionsArray.Values<string>())
            : "No actions specified";

        var (priorityOptionValue, priorityLabel) = priorityStr.ToUpperInvariant() switch
        {
            "HIGH" => (ProcessFirstContactSignalPlugin.PriorityHigh, "High"),
            "MEDIUM" => (ProcessFirstContactSignalPlugin.PriorityMedium, "Medium"),
            _ => (ProcessFirstContactSignalPlugin.PriorityLow, "Low")
        };

        return new ProcessFirstContactSignalPlugin.SignalInferenceResult(intent, priorityLabel, priorityOptionValue, actions);
    }
}
