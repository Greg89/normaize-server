using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Normaize.DataNormalization.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that adds OpenTelemetry TraceId and SpanId to log events
/// This enables correlation between logs and distributed traces
/// </summary>
public class TraceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            // Add TraceId (distributed trace identifier)
            if (activity.TraceId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "TraceId", activity.TraceId.ToString()));
            }

            // Add SpanId (current span identifier)
            if (activity.SpanId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "SpanId", activity.SpanId.ToString()));
            }

            // Add ParentSpanId if available (for span hierarchy)
            if (activity.ParentSpanId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ParentSpanId", activity.ParentSpanId.ToString()));
            }

            // Add TraceFlags (sampling decision)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "TraceFlags", activity.ActivityTraceFlags.ToString()));
        }
    }
}

