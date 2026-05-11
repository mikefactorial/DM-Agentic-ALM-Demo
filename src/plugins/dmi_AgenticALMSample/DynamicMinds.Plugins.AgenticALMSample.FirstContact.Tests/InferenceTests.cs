using DynamicMinds.Plugins.AgenticALMSample.FirstContact;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicMinds.Plugins.AgenticALMSample.FirstContact.Tests;

[TestClass]
public class InferenceTests
{
    [TestMethod]
    public void InferPriorityAndIntent_HostileTranscript_ReturnsHighPriorityAndHostileIntent()
    {
        var result = ProcessFirstContactSignalPlugin.InferPriorityAndIntent("", "Hostile vessel initiated attack pattern.");

        Assert.AreEqual("High", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityHigh, result.PriorityOptionValue);
        Assert.AreEqual("Potentially hostile", result.Intent);
        StringAssert.Contains(result.RecommendedActions, "Notify command immediately");
    }

    [TestMethod]
    public void InferPriorityAndIntent_DiplomaticTranscript_ReturnsDiplomaticIntent()
    {
        var result = ProcessFirstContactSignalPlugin.InferPriorityAndIntent("", "Diplomatic envoy requests alliance terms.");

        Assert.AreEqual("Diplomatic contact", result.Intent);
        Assert.AreEqual("Medium", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityMedium, result.PriorityOptionValue);
    }

    [TestMethod]
    public void InferPriorityAndIntent_RoutineTranscript_ReturnsLowPriority()
    {
        var result = ProcessFirstContactSignalPlugin.InferPriorityAndIntent("", "Routine trading vessel broadcasting status report.");

        Assert.AreEqual("Low", result.PriorityLabel);
        Assert.AreEqual(ProcessFirstContactSignalPlugin.PriorityLow, result.PriorityOptionValue);
    }

    [TestMethod]
    public void InferPriorityAndIntent_WithPromptTemplate_AppendsPromptTrace()
    {
        var result = ProcessFirstContactSignalPlugin.InferPriorityAndIntent("Use concise mission analyst format", "Unknown signal.");

        StringAssert.Contains(result.RecommendedActions, "Prompt profile applied:");
    }
}
