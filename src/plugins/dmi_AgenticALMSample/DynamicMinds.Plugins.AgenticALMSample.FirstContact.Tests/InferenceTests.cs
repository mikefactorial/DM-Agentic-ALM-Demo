using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicMinds.Plugins.AgenticALMSample.FirstContact.Tests;

/// <summary>
/// Unit tests for the SignalInferenceResult model and priority constants.
/// Integration tests for the full Azure OpenAI call path require a live environment.
/// </summary>
[TestClass]
public class InferenceTests
{
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
}
