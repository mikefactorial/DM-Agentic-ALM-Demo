using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicMinds.Plugins.AgenticALMSample.FirstContact.Tests;

/// <summary>
/// Unit tests for the SignalInferenceResult model, priority constants, and AzureOpenAIResponseParser.
/// Integration tests for the full Azure OpenAI HTTP call path require a live environment.
/// </summary>
[TestClass]
public class InferenceTests
{
    // ── SignalInferenceResult model ───────────────────────────────────────────

    [TestMethod]
    public void SignalInferenceResult_HighPriority_PropertiesCorrect()
    {
        var result = new ProcessFirstContactSignalPlugin.SignalInferenceResult(
            "Potentially hostile",
            "High",
            ProcessFirstContactSignalPlugin.PriorityHigh,
            "Notify command immediately; Raise shields");

        Assert.AreEqual("Potentially hostile", result.Intent);
        Assert.AreEqual("High", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityHigh, result.PriorityOptionValue);
        StringAssert.Contains(result.RecommendedActions, "Notify command immediately");
    }

    [TestMethod]
    public void SignalInferenceResult_MediumPriority_PropertiesCorrect()
    {
        var result = new ProcessFirstContactSignalPlugin.SignalInferenceResult(
            "Diplomatic contact",
            "Medium",
            ProcessFirstContactSignalPlugin.PriorityMedium,
            "Open diplomatic channel; Alert ambassador");

        Assert.AreEqual("Diplomatic contact", result.Intent);
        Assert.AreEqual("Medium", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityMedium, result.PriorityOptionValue);
    }

    [TestMethod]
    public void SignalInferenceResult_LowPriority_PropertiesCorrect()
    {
        var result = new ProcessFirstContactSignalPlugin.SignalInferenceResult(
            "Routine transmission",
            "Low",
            ProcessFirstContactSignalPlugin.PriorityLow,
            "Log for review");

        Assert.AreEqual("Routine transmission", result.Intent);
        Assert.AreEqual("Low", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityLow, result.PriorityOptionValue);
    }

    [TestMethod]
    public void PriorityConstants_AreDistinct()
    {
        Assert.AreNotEqual(ProcessFirstContactSignalPlugin.PriorityHigh, ProcessFirstContactSignalPlugin.PriorityMedium);
        Assert.AreNotEqual(ProcessFirstContactSignalPlugin.PriorityMedium, ProcessFirstContactSignalPlugin.PriorityLow);
        Assert.AreNotEqual(ProcessFirstContactSignalPlugin.PriorityHigh, ProcessFirstContactSignalPlugin.PriorityLow);
    }

    // ── AzureOpenAIResponseParser (stubs out real HTTP calls) ─────────────────

    private static string BuildCompletionsJson(string intent, string priority, string[] actions)
    {
        var actionsJson = string.Join(", ", System.Array.ConvertAll(actions, a => $"\"{a}\""));
        var content = $"{{\"intent\":\"{intent}\",\"priority\":\"{priority}\",\"actions\":[{actionsJson}]}}";
        return $"{{\"choices\":[{{\"message\":{{\"content\":\"{content.Replace("\"", "\\\"")}\"}}}}]}}";
    }

    [TestMethod]
    public void ResponseParser_HighPriority_MapsCorrectly()
    {
        var json = BuildCompletionsJson("Hostile approach vector", "High", new[] { "Raise shields", "Alert command" });

        var result = AzureOpenAIResponseParser.Parse(json);

        Assert.AreEqual("Hostile approach vector", result.Intent);
        Assert.AreEqual("High", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityHigh, result.PriorityOptionValue);
        StringAssert.Contains(result.RecommendedActions, "Raise shields");
        StringAssert.Contains(result.RecommendedActions, "Alert command");
    }

    [TestMethod]
    public void ResponseParser_MediumPriority_MapsCorrectly()
    {
        var json = BuildCompletionsJson("Diplomatic hail", "Medium", new[] { "Open channel", "Alert ambassador" });

        var result = AzureOpenAIResponseParser.Parse(json);

        Assert.AreEqual("Diplomatic hail", result.Intent);
        Assert.AreEqual("Medium", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityMedium, result.PriorityOptionValue);
    }

    [TestMethod]
    public void ResponseParser_LowPriority_MapsCorrectly()
    {
        var json = BuildCompletionsJson("Background noise", "Low", new[] { "Log for review" });

        var result = AzureOpenAIResponseParser.Parse(json);

        Assert.AreEqual("Background noise", result.Intent);
        Assert.AreEqual("Low", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityLow, result.PriorityOptionValue);
    }

    [TestMethod]
    public void ResponseParser_UnknownPriority_DefaultsToLow()
    {
        var json = BuildCompletionsJson("Ambiguous signal", "Critical", new[] { "Standby" });

        var result = AzureOpenAIResponseParser.Parse(json);

        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityLow, result.PriorityOptionValue);
        Assert.AreEqual("Low", result.PriorityLabel);
    }

    [TestMethod]
    public void ResponseParser_MultipleActions_JoinedWithSemicolon()
    {
        var json = BuildCompletionsJson("Attack pattern", "High", new[] { "Action A", "Action B", "Action C" });

        var result = AzureOpenAIResponseParser.Parse(json);

        Assert.AreEqual("Action A; Action B; Action C", result.RecommendedActions);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ResponseParser_MissingChoices_ThrowsInvalidOperationException()
    {
        AzureOpenAIResponseParser.Parse("{\"id\":\"chatcmpl-123\"}");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ResponseParser_NonJsonContent_ThrowsInvalidOperationException()
    {
        var json = "{\"choices\":[{\"message\":{\"content\":\"This is not JSON\"}}]}";
        AzureOpenAIResponseParser.Parse(json);
    }
}
