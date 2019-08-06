using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Diagnostics.Tracing;

namespace EnterpriseLibrary.SemanticLogging.Sinks
{
    /// <summary>
    /// Sink for sending semantic logging telemtry to Application Insights
    /// </summary>
    public class WindowsAzureApplicationInsightsSink : IObserver<EventEntry>, IDisposable
    {
        private readonly string instanceName;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsAzureApplicationInsightsSink"/> type with the specified instrumentation key.
        /// </summary>
        /// <param name="instanceName">The name of the instance originating the entries.</param>
        /// <param name="instrumentationKey">The instrumentation key for sending telemetry to Application Insights</param>
        public WindowsAzureApplicationInsightsSink(string instanceName, string instrumentationKey)
        {
            // Instance name is stored for later inclusion in telemetry from this sink
            this.instanceName = instanceName;

            // Set the InstrumentationKey property on TelemetryClient instead of passing in a TelemetryClientConfiguration
            // to enable retrieving the key later, if needed (as with the ApplicationInsightsTraceListener.InstrumentationKey property)
            this.telemetryClient = new TelemetryClient()
            {
                InstrumentationKey = instrumentationKey
            };
        }

        /// <summary>
        /// The instrumentation key used to record Application Insights telemetry
        /// </summary>
        public string InstrumentationKey
        {
            get => telemetryClient.InstrumentationKey;
            set => telemetryClient.InstrumentationKey = value;
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        public void OnCompleted() => this.Flush();

        private void Flush() => telemetryClient?.Flush();

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An <see cref="Exception"/> providing additional information about the error.</param>
        public void OnError(Exception error)
        {
            if (error != null)
            {
                // Application Insights is able to usefully track exceptions
                telemetryClient.TrackException(error);
            }
        }

        /// <summary>
        /// Provides the sink with new data to write.
        /// </summary>
        /// <param name="eventEntry">The current <see cref="EventEntry"/> to write.</param>
        public void OnNext(EventEntry eventEntry)
        {
            // Unlike the SQL and Table Storage sinks, there's no need to buffer events to be
            // sent here because TelemetryClient already buffers itself.
            if (eventEntry != null)
            {
                telemetryClient.TrackTrace(GetApplicationInsightsTrace(eventEntry));
            }
        }

        /// <summary>
        /// Constructs an Application Insights <see cref="TraceTelemetry"/> object from
        /// an Enterprise Library <see cref="EventEntry"/>
        /// </summary>
        /// <param name="eventEntry">The event to convert into an Application Insights trace</param>
        /// <returns>An Application Insights trace object</returns>
        private TraceTelemetry GetApplicationInsightsTrace(EventEntry eventEntry)
        {
            var telemetry = new TraceTelemetry(eventEntry.FormattedMessage, GetSeverity(eventEntry.Schema?.Level));

            if (eventEntry.Timestamp != default)
            {
                telemetry.Timestamp = eventEntry.Timestamp;
            }

            // Structured event data is stored as trace properties
            AddProperty(instanceName, "InstanceName", telemetry);
            AddProperty(eventEntry.EventId.ToString(), "EventId", telemetry);
            AddProperty(eventEntry.Schema.EventName, "EventName", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.ActivityId), "ActivityId", telemetry);
            AddProperty(eventEntry.Schema.Level.ToString(), "EventLevel", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.ThreadId), "ThreadId", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.ProcessId), "ProcessId", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.ProviderId), "ProviderId", telemetry);
            AddProperty(eventEntry.Schema.ProviderName, "ProviderName", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.Schema.Keywords), "Keywords", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.Schema.Opcode), "Opcode", telemetry);
            AddProperty(eventEntry.Schema.OpcodeName, "OpcodeName", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.Schema.Version), "Version", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.ActivityId), "ActivityId", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.RelatedActivityId), "RelatedActivityId", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.Schema.Task), "Task", telemetry);
            AddProperty(GetNonDefaultString(eventEntry.Schema.TaskName), "TaskName", telemetry);
            AddProperty(eventEntry.Schema.KeywordsDescription, "KeywordsDescription", telemetry);
            for (int i = 0; i < eventEntry.Schema.Payload.Length; i++)
            {
                AddProperty(eventEntry.Payload[i].ToString(), eventEntry.Schema.Payload[i], telemetry);
            }

            return telemetry;
        }

        // Some properties are not useful to include if they have default values; this
        // helper method returns null for those cases so that the properties aren't included.
        private string GetNonDefaultString<T>(T input) => input?.Equals(default(T)) ?? true ? null : input.ToString();

        private SeverityLevel GetSeverity(EventLevel? eventEntry)
        {
            switch(eventEntry)
            {
                case EventLevel.Verbose:
                    return SeverityLevel.Verbose;
                case EventLevel.Informational:
                    return SeverityLevel.Information;
                case EventLevel.Warning:
                    return SeverityLevel.Warning;
                case EventLevel.Error:
                    return SeverityLevel.Error;
                case EventLevel.Critical:
                    return SeverityLevel.Critical;
                default:
                    return SeverityLevel.Information;
            }
        }

        private void AddProperty(string value, string key, TraceTelemetry telemetry)
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                telemetry.Properties[key] = value;
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="WindowsAzureApplicationInsightsSink"/> class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Optionally releases managed resources used by the <see cref="WindowsAzureApplicationInsightsSink"/>
        /// </summary>
        /// <param name="disposing">Whether the method is called from <see cref="Dispose()"/></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Flush();
            }
        }
    }
}
