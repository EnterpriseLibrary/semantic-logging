using System;
using System.Diagnostics.Tracing;
using EnterpriseLibrary.SemanticLogging.Sinks;

namespace EnterpriseLibrary.SemanticLogging
{
    /// <summary>
    /// Factories and helpers for using the <see cref="WindowsAzureApplicationInsightsSink"/>.
    /// </summary>
    public static class WindowsAzureApplicationInsightsLog
    {
        /// <summary>
        /// Creates an event listener that logs using a <see cref="WindowsAzureApplicationInsightsSink" />.
        /// </summary>
        /// <param name="instanceName">The name of the instance originating the entries.</param>
        /// <param name="instrumentationKey">The instrumentation key for sending telemetry to Application Insights</param>
        /// <returns>An event listener that uses <see cref="WindowsAzureApplicationInsightsSink"/> to log events.</returns>
        public static EventListener CreateListener(string instanceName, string instrumentationKey)
        {
            var listener = new ObservableEventListener();
            listener.LogToWindowsAzureApplicationInsights(instanceName, instrumentationKey);
            return listener;
        }

        /// <summary>
        /// Subscribes to an <see cref="IObservable{EventEntry}" /> using a <see cref="WindowsAzureApplicationInsightsSink" />.
        /// </summary>
        /// <param name="eventStream">The event stream. Typically this is an instance of <see cref="ObservableEventListener" />.</param>
        /// <param name="instanceName">The name of the instance originating the entries.</param>
        /// <param name="instrumentationKey">The instrumentation key for sending telemetry to Application Insights</param>
        /// <returns>A subscription to the sink that can be disposed to unsubscribe the sink and dispose it, or to get access to the sink instance.</returns>
        public static SinkSubscription<WindowsAzureApplicationInsightsSink> LogToWindowsAzureApplicationInsights(this IObservable<EventEntry> eventStream, string instanceName, string instrumentationKey)
        {
            var sink = new WindowsAzureApplicationInsightsSink(instanceName, instrumentationKey);
            var subscription = eventStream.Subscribe(sink);
            return new SinkSubscription<WindowsAzureApplicationInsightsSink>(subscription, sink);
        }
    }
}
