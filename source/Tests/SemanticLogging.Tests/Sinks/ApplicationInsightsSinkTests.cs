using EnterpriseLibrary.SemanticLogging.Schema;
using EnterpriseLibrary.SemanticLogging.Sinks;
using EnterpriseLibrary.SemanticLogging.Tests.TestSupport;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;

namespace EnterpriseLibrary.SemanticLogging.Tests.Sinks
{
    [TestClass]
    public class ApplicationInsightsSinkTests
    {
        const string InstanceName = "InstanceName";
        const string InstrumentationKey = "InstrumentationKey";

        MockApplicationInsightsTelemetryChannel TelemetryChannel;

        [TestInitialize]
        public void Setup()
        {
            // Intercept AI telemetry client
            TelemetryChannel = new MockApplicationInsightsTelemetryChannel();
            TelemetryConfiguration.Active.TelemetryChannel = TelemetryChannel;
        }

        [TestMethod]
        public void ExtensionMethodsCreateSinkProperly()
        {
            TelemetryChannel.ResetTelemetry();

            using (var listener = WindowsAzureApplicationInsightsLog.CreateListener(InstanceName, InstrumentationKey))
            {
                listener.EnableEvents(TestEventSource.Log, EventLevel.Informational);
                TestEventSource.Log.LogWarning("Test message");
            }

            var trace = TelemetryChannel.Traces.SingleOrDefault() as TraceTelemetry;

            Assert.IsNotNull(trace);
            Assert.AreEqual("Warning Event", trace.Message);
            Assert.AreEqual(SeverityLevel.Warning, trace.SeverityLevel);
            Assert.AreEqual(InstrumentationKey, trace.Context.InstrumentationKey);
            AssertHasProperty(trace, "message", "Test message");
            AssertHasProperty(trace, "InstanceName", InstanceName);
        }

        [TestMethod]
        public void PropertiesSetCorrectly()
        {
            using (var sink = new WindowsAzureApplicationInsightsSink(null, InstrumentationKey))
            {
                Assert.AreEqual(InstrumentationKey, sink.InstrumentationKey);
            }

            Assert.ThrowsException<ArgumentNullException>(() => new WindowsAzureApplicationInsightsSink(InstanceName, null));
        }

        [TestMethod]
        public void FlushesOnDispose()
        {
            TelemetryChannel.ResetTelemetry();

            using (var sink = new WindowsAzureApplicationInsightsSink(InstanceName, InstrumentationKey))
            {
                Assert.AreEqual(0, TelemetryChannel.FlushCount);

                sink.OnCompleted();

                Assert.AreEqual(1, TelemetryChannel.FlushCount);
            }

            Assert.AreEqual(0, TelemetryChannel.Traces.Count);
            Assert.AreEqual(2, TelemetryChannel.FlushCount);
        }

        // Confirm that events are sent to AppInsights, including both implicit and explicit properties
        [TestMethod]
        public void SendsEventsToApplicationInsights()
        {
            TelemetryChannel.ResetTelemetry();

            var eventEntry = new EventEntry(
                Guid.NewGuid(),
                11,
                "Test Message",
                new ReadOnlyCollection<object>(new object[] { "Value", 5 }),
                DateTimeOffset.Now.AddMinutes(-5),
                100,
                200,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new EventSchema(
                    11,
                    Guid.NewGuid(),
                    "MyProvider",
                    EventLevel.Warning,
                    EventTask.None,
                    "None",
                    EventOpcode.Start,
                    "Info",
                    EventKeywords.EventLogClassic,
                    "EventLogClassic",
                    4,
                    new[] { "Key", "Number" }));

            using (var sink = new WindowsAzureApplicationInsightsSink(InstanceName, InstrumentationKey))
            {
                sink.OnNext(eventEntry);
            }

            var trace = TelemetryChannel.Traces.SingleOrDefault() as TraceTelemetry;

            Assert.IsNotNull(trace);
            Assert.AreEqual(eventEntry.Timestamp, trace.Timestamp);
            Assert.AreEqual(SeverityLevel.Warning, trace.SeverityLevel);
            Assert.AreEqual(eventEntry.FormattedMessage, trace.Message);
            Assert.AreEqual(InstrumentationKey, trace.Context.InstrumentationKey);
            AssertHasProperty(trace, "InstanceName", InstanceName);
            AssertHasProperty(trace, "Key", "Value");
            AssertHasProperty(trace, "Number", "5");
            AssertHasProperty(trace, "ActivityId", eventEntry.ActivityId.ToString());
            AssertHasProperty(trace, "EventId", eventEntry.EventId.ToString());
            AssertHasProperty(trace, "ProcessId", eventEntry.ProcessId.ToString());
            AssertHasProperty(trace, "ProviderId", eventEntry.ProviderId.ToString());
            AssertHasProperty(trace, "RelatedActivityId", eventEntry.RelatedActivityId.ToString());
            AssertHasProperty(trace, "ThreadId", eventEntry.ThreadId.ToString());
            AssertHasProperty(trace, "EventName", eventEntry.Schema.EventName.ToString());
            AssertHasProperty(trace, "Keywords", eventEntry.Schema.Keywords.ToString());
            AssertHasProperty(trace, "KeywordsDescription", eventEntry.Schema.KeywordsDescription.ToString());
            AssertHasProperty(trace, "Opcode", eventEntry.Schema.Opcode.ToString());
            AssertHasProperty(trace, "OpcodeName", eventEntry.Schema.OpcodeName.ToString());
            AssertHasProperty(trace, "ProviderName", eventEntry.Schema.ProviderName.ToString());
            AssertDoesNotHaveProperty(trace, "Task");
            AssertHasProperty(trace, "TaskName", eventEntry.Schema.TaskName.ToString());
            AssertHasProperty(trace, "Version", eventEntry.Schema.Version.ToString());
        }

        // Confirm that null/default properties aren't include in traces
        [TestMethod]
        public void DefaultPropertiesNotTraced()
        {
            TelemetryChannel.ResetTelemetry();

            var eventEntry = new EventEntry(
                Guid.Empty,
                0,
                null,
                null,
                default,
                0,
                0,
                Guid.Empty,
                Guid.Empty,
                new EventSchema(
                    0,
                    Guid.Empty,
                    null,
                    EventLevel.LogAlways,
                    EventTask.None,
                    null,
                    EventOpcode.Info,
                    null,
                    EventKeywords.None,
                    null,
                    0,
                    Enumerable.Empty<string>()));

            using (var sink = new WindowsAzureApplicationInsightsSink(null, InstrumentationKey))
            {
                sink.OnNext(eventEntry);
            }

            var trace = TelemetryChannel.Traces.SingleOrDefault() as TraceTelemetry;

            Assert.IsNotNull(trace);

            // Default AppInsights timestamp (DateTimeOffset.Now) used if none is specified
            Assert.AreNotEqual(eventEntry.Timestamp, trace.Timestamp);
            Assert.IsTrue(trace.Timestamp > DateTimeOffset.Now.AddMinutes(-1));

            Assert.AreEqual(SeverityLevel.Information, trace.SeverityLevel);
            Assert.AreEqual(eventEntry.FormattedMessage, trace.Message);
            Assert.AreEqual(InstrumentationKey, trace.Context.InstrumentationKey);

            AssertHasProperty(trace, "EventId", "0");

            AssertDoesNotHaveProperty(trace, "InstanceName");
            AssertDoesNotHaveProperty(trace, "Key");
            AssertDoesNotHaveProperty(trace, "ActivityId");
            AssertDoesNotHaveProperty(trace, "ProcessId");
            AssertDoesNotHaveProperty(trace, "ProviderId");
            AssertDoesNotHaveProperty(trace, "RelatedActivityId");
            AssertDoesNotHaveProperty(trace, "ThreadId");
            AssertDoesNotHaveProperty(trace, "EventName");
            AssertDoesNotHaveProperty(trace, "Keywords");
            AssertDoesNotHaveProperty(trace, "KeywordsDescription");
            AssertDoesNotHaveProperty(trace, "Opcode");
            AssertDoesNotHaveProperty(trace, "OpcodeName");
            AssertDoesNotHaveProperty(trace, "ProviderName");
            AssertDoesNotHaveProperty(trace, "Task");
            AssertDoesNotHaveProperty(trace, "TaskName");
            AssertDoesNotHaveProperty(trace, "Version");
        }

        [DataTestMethod]
        [DataRow(EventLevel.Critical, SeverityLevel.Critical)]
        [DataRow(EventLevel.Error, SeverityLevel.Error)]
        [DataRow(EventLevel.Informational, SeverityLevel.Information)]
        [DataRow(EventLevel.LogAlways, SeverityLevel.Information)]
        [DataRow(EventLevel.Verbose, SeverityLevel.Verbose)]
        [DataRow(EventLevel.Warning, SeverityLevel.Warning)]
        public void EventLevelsAreTranslatedToSeverityLevels(EventLevel eventLevel, SeverityLevel severityLevel)
        {
            var eventEntry = EventEntryTestHelper.Create(level: eventLevel);
            TelemetryChannel.ResetTelemetry();

            using (var sink = new WindowsAzureApplicationInsightsSink(null, InstrumentationKey))
            {
                sink.OnNext(eventEntry);
            }

            var trace = TelemetryChannel.Traces.SingleOrDefault() as TraceTelemetry;

            Assert.IsNotNull(trace);
            Assert.AreEqual(severityLevel, trace.SeverityLevel);
        }

        [TestMethod]
        public void SendExceptionsToApplicationInsights()
        {
            TelemetryChannel.ResetTelemetry();

            var exc = new BadImageFormatException("TestMessage");
            using (var sink = new WindowsAzureApplicationInsightsSink(InstanceName, InstrumentationKey))
            {
                sink.OnError(exc);
            }

            var trace = TelemetryChannel.Traces.SingleOrDefault() as ExceptionTelemetry;

            Assert.IsNotNull(trace);
            Assert.AreEqual(exc, trace.Exception);
            Assert.AreEqual(null, trace.Message);
            Assert.AreEqual(null, trace.SeverityLevel);
            Assert.AreEqual(0, trace.Properties.Count);

            var excDetails = trace.ExceptionDetailsInfoList.SingleOrDefault();
            Assert.IsNotNull(excDetails);
            Assert.AreEqual(exc.Message, excDetails.Message);
            Assert.AreEqual(exc.GetType().ToString(), excDetails.TypeName);
        }

        private void AssertDoesNotHaveProperty(TraceTelemetry telemetry, string key)
        {
            Assert.IsTrue(!telemetry.Properties.ContainsKey(key));
        }

        public void AssertHasProperty(TraceTelemetry telemetry, string key, string expectedValue)
        {
            Assert.AreEqual(expectedValue, telemetry.Properties[key]);
        }

        [EventSource(Name = "Test Event Source")]
        private class TestEventSource : EventSource
        {
            public static TestEventSource Log = new TestEventSource();

            [Event(1, Message = "Warning Event", Level = EventLevel.Warning)]
            public void LogWarning(string message)
            {
                if (IsEnabled())
                {
                    WriteEvent(1, message);
                }
            }
        }
    }
}
