# Observability & Chaos Engineering Implementation Plan

## Executive Summary

This document outlines a DDD-compliant approach to enhance observability and implement chaos engineering in the Normaize server application. The plan maintains clean architecture boundaries while providing comprehensive telemetry through Serilog, OpenTelemetry, and structured logging.

---

## Current State Analysis

### Existing Infrastructure
- **Logging**: Serilog with Seq sink (based on workflow)
- **Testing**: Coverage analysis with SonarQube and Codecov
- **Architecture**: Likely DDD/Clean Architecture structure (Normaize.API, Normaize.Core, Normaize.Data)
- **Hosting**: Railway platform

### Gaps
- No distributed tracing
- Limited correlation between logs
- No metrics collection
- No chaos engineering capabilities
- Observability concerns mixed with business logic

---

## Proposed Architecture

### 1. Observability Boundaries (DDD-Compliant)

```
┌─────────────────────────────────────────────────┐
│ Domain Layer (Normaize.Core.Domain)            │
│ • Pure business logic                           │
│ • No logging, no telemetry                      │
│ • Returns domain events/results                 │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ Application Layer (Normaize.Core.Application)  │
│ • MediatR handlers                              │
│ • Returns Result<T> types                       │
│ • Emits application events                      │
│ • NO direct logging or telemetry                │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ Infrastructure (Normaize.Infrastructure)        │
│ • Serilog configuration                         │
│ • OpenTelemetry exporters                       │
│ • ChaosTelemetry service                        │
│ • Observability pipeline behaviors              │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ Host Layer (Normaize.API)                       │
│ • DI container configuration                    │
│ • Middleware registration                       │
│ • Service wiring                                │
└─────────────────────────────────────────────────┘
```

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1-2) - **IMMEDIATE VALUE**

#### 1.1 Structured Logging Enhancement

**Goal**: Upgrade Serilog with correlation and enrichment

**Tasks**:
- [ ] Add correlation ID middleware
- [ ] Configure Serilog enrichers
- [ ] Implement request logging
- [ ] Add LogContext scope to MediatR pipeline

**Deliverables**:
```csharp
// Normaize.Infrastructure/Logging/SerilogConfiguration.cs
public static class SerilogConfiguration
{
    public static IServiceCollection AddStructuredLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "Normaize")
            .Enrich.WithProperty("Version", GetVersion())
            .WriteTo.Console()
            .WriteTo.Seq(configuration["Seq:ServerUrl"])
            .CreateLogger();

        return services;
    }
}
```

**Expected Outcome**: Every log entry includes TraceId, Environment, Service, Version

---

#### 1.2 MediatR Observability Pipeline

**Goal**: Centralized telemetry for all commands/queries

**Tasks**:
- [ ] Create `ObservabilityBehavior<TRequest, TResponse>`
- [ ] Add trace/span creation
- [ ] Log request start/completion with timing
- [ ] Capture exceptions with context

**Deliverables**:
```csharp
// Normaize.Infrastructure/Behaviors/ObservabilityBehavior.cs
public class ObservabilityBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ObservabilityBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString();

        using (LogContext.PushProperty("RequestName", requestName))
        using (LogContext.PushProperty("RequestId", requestId))
        using (Activity.Current?.AddTag("request.name", requestName))
        {
            var sw = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation(
                    "Handling {RequestName} ({RequestId})",
                    requestName, requestId);

                var response = await next();
                
                sw.Stop();
                _logger.LogInformation(
                    "Completed {RequestName} in {ElapsedMs}ms",
                    requestName, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "Failed {RequestName} after {ElapsedMs}ms",
                    requestName, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
```

**Expected Outcome**: Uniform observability across all MediatR requests without handler modification

---

### Phase 2: Tracing Infrastructure (Week 3-4)

#### 2.1 OpenTelemetry Setup

**Goal**: Distributed tracing with activity-based model

**Tasks**:
- [ ] Install OpenTelemetry packages
- [ ] Configure ActivitySource
- [ ] Add instrumentation for ASP.NET Core, HttpClient, EF Core
- [ ] Set up console exporter (development)

**Deliverables**:
```csharp
// Normaize.Infrastructure/Telemetry/OpenTelemetryConfiguration.cs
public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddOpenTelemetryTracing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddSource("Normaize.*")
                .SetResourceBuilder(ResourceBuilder
                    .CreateDefault()
                    .AddService("Normaize.API")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = 
                            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                        ["service.version"] = GetVersion()
                    }))
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    opts.EnrichWithHttpRequest = EnrichRequest;
                    opts.EnrichWithHttpResponse = EnrichResponse;
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddConsoleExporter() // Development
                // .AddOtlpExporter() // Phase 3
            );

        return services;
    }
}
```

**Expected Outcome**: Full request traces with span timing, even without external backend

---

#### 2.2 Trace-Log Correlation

**Goal**: Connect Serilog logs to OpenTelemetry traces

**Tasks**:
- [ ] Add TraceId/SpanId enricher
- [ ] Update Seq queries to use trace IDs
- [ ] Create Seq dashboards for trace-based queries

**Deliverables**:
```csharp
// Normaize.Infrastructure/Logging/TraceContextEnricher.cs
public class TraceContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logEvent.AddPropertyIfAbsent(factory.CreateProperty(
                "TraceId", activity.TraceId.ToString()));
            logEvent.AddPropertyIfAbsent(factory.CreateProperty(
                "SpanId", activity.SpanId.ToString()));
        }
    }
}
```

**Expected Outcome**: Click a trace ID in Seq to see all related logs

---

### Phase 3: Chaos Engineering Foundation (Week 5-6)

#### 3.1 Chaos Telemetry Service

**Goal**: Clean interface for chaos event recording

**Tasks**:
- [ ] Create `IChaosTelemetry` interface in Application layer
- [ ] Implement `ChaosTelemetry` in Infrastructure
- [ ] Add chaos-specific metrics and traces

**Deliverables**:
```csharp
// Normaize.Core.Application/Chaos/IChaosTelemetry.cs
public interface IChaosTelemetry
{
    void RecordTick(string experimentName, string tickId, 
        ChaosExperimentType type);
    void RecordInjection(string faultType, string target, 
        double magnitude, string tickId);
    void RecordFailure(string experimentName, Exception exception);
}

// Normaize.Infrastructure/Chaos/ChaosTelemetry.cs
public class ChaosTelemetry : IChaosTelemetry
{
    private readonly ILogger<ChaosTelemetry> _logger;
    private readonly Counter<long> _tickCounter;
    private readonly Counter<long> _injectionCounter;
    private readonly Histogram<double> _magnitudeHistogram;

    public void RecordTick(string experimentName, string tickId, 
        ChaosExperimentType type)
    {
        using (LogContext.PushProperty("TickId", tickId))
        using (LogContext.PushProperty("ExperimentName", experimentName))
        {
            _logger.LogInformation(
                "Chaos tick fired: {ExperimentName} ({Type})",
                experimentName, type);

            _tickCounter.Add(1, new TagList
            {
                { "experiment", experimentName },
                { "type", type.ToString() }
            });

            Activity.Current?.AddEvent(new ActivityEvent(
                "chaos.tick",
                tags: new ActivityTagsCollection
                {
                    { "experiment.name", experimentName },
                    { "tick.id", tickId }
                }));
        }
    }
}
```

**Expected Outcome**: Chaos events visible in logs, traces, and metrics without business logic pollution

---

#### 3.2 Chaos Pipeline Behavior

**Goal**: Integrate chaos into MediatR pipeline

**Tasks**:
- [ ] Create `ChaosBehavior<TRequest, TResponse>`
- [ ] Implement fault injection strategies (latency, exceptions, timeouts)
- [ ] Add chaos configuration system

**Deliverables**:
```csharp
// Normaize.Infrastructure/Behaviors/ChaosBehavior.cs
public class ChaosBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IChaosEngine _chaosEngine;
    private readonly IChaosTelemetry _telemetry;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        if (_chaosEngine.ShouldInjectFault(requestName, out var fault))
        {
            _telemetry.RecordInjection(
                fault.Type, requestName, fault.Magnitude, fault.TickId);

            return await fault.Type switch
            {
                FaultType.Latency => InjectLatency(next, fault),
                FaultType.Exception => InjectException(fault),
                FaultType.Timeout => InjectTimeout(next, fault, cancellationToken),
                _ => await next()
            };
        }

        return await next();
    }
}
```

**Expected Outcome**: Controlled chaos injection with full observability

---

### Phase 4: Metrics & Advanced Observability (Week 7-8)

#### 4.1 OpenTelemetry Metrics

**Goal**: Prometheus-compatible metrics for chaos and business KPIs

**Tasks**:
- [ ] Configure OTel metrics
- [ ] Add chaos-specific metrics
- [ ] Add business metrics (commands/sec, success rate, duration)
- [ ] Expose Prometheus endpoint

**Deliverables**:
```csharp
// Normaize.Infrastructure/Telemetry/MetricsConfiguration.cs
public static class MetricsConfiguration
{
    public static IServiceCollection AddOpenTelemetryMetrics(
        this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(builder => builder
                .AddMeter("Normaize.*")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter());

        return services;
    }
}
```

**Expected Outcome**: Real-time dashboards showing chaos impact on system health

---

#### 4.2 Seq Dashboards & Alerts

**Goal**: Actionable insights from structured logs

**Tasks**:
- [ ] Create Seq signals for chaos events
- [ ] Set up environment safety alerts
- [ ] Build correlation dashboards (chaos ↔ errors)
- [ ] Configure retention policies

**Deliverables**:
- Seq Signal: "Chaos enabled in Production" → Alert
- Seq Signal: "Error rate increase during chaos" → Dashboard
- Seq Query: `Chaos.Enabled=true AND StatusCode >= 500`

**Expected Outcome**: Proactive alerts when chaos causes production issues

---

### Phase 5: Full APM Stack (Week 9-10) - **OPTIONAL**

#### 5.1 Grafana Tempo + Prometheus

**Goal**: Complete observability stack on Railway

**Tasks**:
- [ ] Deploy Grafana Tempo (OTLP receiver)
- [ ] Deploy Prometheus
- [ ] Configure OTLP exporters
- [ ] Build Grafana dashboards

**Railway Services**:
- `normaize-api` (existing)
- `seq` (existing)
- `grafana-tempo` (new)
- `prometheus` (new)
- `grafana` (new)

**Expected Outcome**: Distributed tracing UI + metrics visualization in Grafana

---

## Quick Wins (Can Do Today)

### 1. Enhanced Seq Logging (30 minutes)
```json
// Add to appsettings.json
{
  "Serilog": {
    "Using": ["Serilog.Enrichers.Environment"],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"],
    "Properties": {
      "Application": "Normaize",
      "Environment": "Development"
    }
  }
}
```

### 2. MediatR Pipeline (1 hour)
Add `ObservabilityBehavior` to DI in `Program.cs`:
```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), 
    typeof(ObservabilityBehavior<,>));
```

### 3. Correlation ID Middleware (30 minutes)
```csharp
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"]
        .FirstOrDefault() ?? Guid.NewGuid().ToString();
    
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        context.Response.Headers.Add("X-Correlation-ID", correlationId);
        await next();
    }
});
```

---

## Technology Stack

### Tier 1 (Immediate - Seq Only)
| Component | Package | Purpose |
|-----------|---------|---------|
| Serilog | `Serilog.AspNetCore` | Structured logging |
| Seq | `Serilog.Sinks.Seq` | Log aggregation |
| MediatR | `MediatR` | CQRS pipeline |
| OTel Tracing | `OpenTelemetry.Instrumentation.AspNetCore` | Local tracing |

### Tier 2 (Full Stack - Railway)
| Component | Package/Service | Purpose |
|-----------|-----------------|---------|
| Grafana Tempo | Docker image | Trace storage/query |
| Prometheus | Docker image | Metrics scraping |
| Grafana | Docker image | Visualization |
| OTel Exporter | `OpenTelemetry.Exporter.OpenTelemetryProtocol` | OTLP export |

---

## Success Metrics

### Phase 1-2 (Weeks 1-4)
- ✅ 100% of MediatR requests have TraceId
- ✅ Average log context includes 5+ properties
- ✅ Zero observability code in domain/application layers

### Phase 3-4 (Weeks 5-8)
- ✅ Chaos experiments recordable without code changes
- ✅ <100ms overhead from telemetry
- ✅ Seq dashboards show chaos correlation

### Phase 5 (Weeks 9-10)
- ✅ End-to-end traces visible in Grafana
- ✅ Prometheus scraping 50+ metrics
- ✅ <5 minute MTTR (Mean Time To Resolution) for chaos-related issues

---

## Risk Mitigation

### Performance Impact
- **Risk**: Telemetry overhead in hot paths
- **Mitigation**: Use sampling (trace 10% in prod, 100% in dev)

### Complexity Creep
- **Risk**: Observability sprawl across layers
- **Mitigation**: Strict boundary enforcement via architecture tests

### Railway Resource Limits
- **Risk**: Running Tempo/Prometheus exceeds plan
- **Mitigation**: Start with Tier 1 (Seq only), add Tier 2 when budget allows

---

## Recommended Reading Order

1. Read this plan top-to-bottom
2. Review Phase 1.2 (MediatR behavior) - this is your architectural cornerstone
3. Implement "Quick Wins" section first
4. Tackle Phase 1-2 before considering chaos engineering
5. Add chaos (Phase 3) only after observability is solid

---

## Next Steps

1. **Review & Approve**: Discuss with team, adjust timeline
2. **Spike**: Implement Phase 1.1 in a feature branch (1 day)
3. **Iterate**: Get Seq dashboards working before moving to Phase 2
4. **Validate**: Run chaos experiments in development first

---

## Related Documentation

- [Chaos Engineering Approaches](CHAOS_ENGINEERING_APPROACHES.md)
- [Chaos Engineering Logging Architecture](CHAOS_ENGINEERING_LOGGING_ARCHITECTURE.md)
- [Architecture Overview](ARCHITECTURE_OVERVIEW.md)
- [Testing Strategy](TESTING_STRATEGY.md)

---

**Document Version**: 1.0  
**Created**: 2025-12-29  
**Last Updated**: 2025-12-29  
**Target Completion**: 10 weeks (adjustable based on team capacity)