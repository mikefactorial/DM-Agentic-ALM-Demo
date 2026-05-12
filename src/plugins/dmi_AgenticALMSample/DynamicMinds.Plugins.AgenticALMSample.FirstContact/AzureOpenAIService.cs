using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DynamicMinds.Plugins.AgenticALMSample.FirstContact;

/// <summary>
/// Calls Azure OpenAI Chat Completions API using a bearer token acquired externally
/// (e.g. via Dataverse IManagedIdentityService — no client secret required).
/// </summary>
public sealed class AzureOpenAIService
{
    private readonly string _endpoint;
    private readonly string _deploymentName;
    private readonly string _bearerToken;

    private const string ApiVersion = "2024-02-01";

    /// <summary>The Azure Cognitive Services scope used when acquiring a token via IManagedIdentityService.</summary>
    public const string CognitiveServicesResource = "https://cognitiveservices.azure.com/.default";

    public AzureOpenAIService(string endpoint, string deploymentName, string bearerToken)
    {
        _endpoint = endpoint?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(endpoint));
        _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));
        _bearerToken = bearerToken ?? throw new ArgumentNullException(nameof(bearerToken));
    }

    /// <summary>
    /// Analyses a signal transcript using Azure OpenAI and returns a structured inference result.
    /// </summary>
    public ProcessFirstContactSignalPlugin.SignalInferenceResult AnalyseTranscript(string systemPrompt, string transcript)
    {
        var response = CallChatCompletions(systemPrompt, transcript);
        return ParseResponse(response);
    }

    private string CallChatCompletions(string systemPrompt, string transcript)
    {
        var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version={ApiVersion}";

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = BuildSystemPrompt(systemPrompt) },
                new { role = "user", content = transcript }
            },
            temperature = 0.2,
            max_tokens = 500,
            response_format = new { type = "json_object" }
        };

        var requestJson = JsonConvert.SerializeObject(requestBody);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.ContentLength = requestBytes.Length;
        request.Headers.Add("Authorization", $"Bearer {_bearerToken}");

        using (var stream = request.GetRequestStream())
        {
            stream.Write(requestBytes, 0, requestBytes.Length);
        }

        using var response = (HttpWebResponse)request.GetResponse();
        using var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string BuildSystemPrompt(string customPrompt)
    {
        const string basePrompt =
            "You are a space mission signal analyst. Analyse the incoming transmission transcript and respond ONLY with valid JSON in this exact format:\n" +
            "{\n" +
            "  \"intent\": \"<brief intent description>\",\n" +
            "  \"priority\": \"High|Medium|Low\",\n" +
            "  \"actions\": [\"action1\", \"action2\", \"action3\"]\n" +
            "}\n" +
            "Priority must be exactly one of: High, Medium, or Low.\n" +
            "Do not include any text outside the JSON object.";

        return string.IsNullOrWhiteSpace(customPrompt)
            ? basePrompt
            : basePrompt + "\n\nAdditional instructions: " + customPrompt;
    }

    private ProcessFirstContactSignalPlugin.SignalInferenceResult ParseResponse(string completionsJson)
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
