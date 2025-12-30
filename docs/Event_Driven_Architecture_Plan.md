# Event-Driven Architecture Plan for Data Normalization Service

## **Current State Analysis**

### **Polling-Based Implementation**
- **Background Service**: Continuously polls job queue every 5 minutes
- **Resource Usage**: Low during idle, high during processing
- **Latency**: Up to 5 minutes delay for job pickup
- **Scalability**: Limited to single instance processing

## **Event-Driven Architecture Options**

### **Option 1: Database Change Notifications (Railway Compatible)**

#### **Implementation**
```csharp
// Use PostgreSQL LISTEN/NOTIFY for job table changes
// Implement database trigger for job insertion
// Service subscribes to notifications
```

#### **Pros**
- ✅ **Railway Compatible**: Works with PostgreSQL
- ✅ **No External Dependencies**: Uses existing database
- ✅ **Real-time**: Immediate job pickup
- ✅ **Reliable**: Database guarantees delivery
- ✅ **Cost Effective**: No additional services

#### **Cons**
- ❌ **Database Coupling**: Tightly coupled to database
- ❌ **Limited Scalability**: Single database instance
- ❌ **Complex Setup**: Requires triggers and notification handling
- ❌ **Platform Lock-in**: PostgreSQL specific

#### **Railway Considerations**
- PostgreSQL supports LISTEN/NOTIFY
- No additional infrastructure costs
- Works within Railway's managed database limits

---

### **Option 2: Redis Pub/Sub (Recommended)**

#### **Implementation**
```csharp
// Redis pub/sub for job events
// Service subscribes to job channels
// Immediate event processing
```

#### **Pros**
- ✅ **High Performance**: Sub-millisecond latency
- ✅ **Scalable**: Multiple consumers possible
- ✅ **Railway Compatible**: Redis add-on available
- ✅ **Real-time**: Immediate job pickup
- ✅ **Flexible**: Multiple event types and channels
- ✅ **Reliable**: Redis persistence options

#### **Cons**
- ❌ **Additional Cost**: Redis add-on (~$5-10/month)
- ❌ **External Dependency**: Requires Redis service
- ❌ **Memory Usage**: Events stored in memory
- ❌ **Complexity**: Event ordering and persistence

#### **Railway Considerations**
- Redis add-on available
- Managed service with automatic scaling
- Built-in persistence and clustering

---

### **Option 3: SignalR Real-time Communication**

#### **Implementation**
```csharp
// WebSocket-based real-time communication
// Service connects to SignalR hub
// Immediate job notifications
```

#### **Pros**
- ✅ **Real-time**: Immediate job pickup
- ✅ **WebSocket Based**: Low latency
- ✅ **Built-in**: ASP.NET Core native
- ✅ **No External Dependencies**: Self-hosted
- ✅ **Cost Effective**: No additional services

#### **Cons**
- ❌ **HTTP Coupling**: Requires HTTP context
- ❌ **Scaling Challenges**: Multiple instances coordination
- ❌ **Connection Management**: WebSocket lifecycle
- ❌ **Limited Persistence**: No event storage

#### **Railway Considerations**
- Works within Railway's HTTP infrastructure
- No additional costs
- Scaling challenges with multiple instances

---

### **Option 4: Hybrid Approach (Best of Both Worlds)**

#### **Implementation**
```csharp
// Primary: Redis pub/sub for real-time
// Fallback: Database polling every 5 minutes
// Graceful degradation
```

#### **Pros**
- ✅ **Resilient**: Fallback mechanism
- ✅ **High Performance**: Primary Redis path
- ✅ **Cost Optimized**: Use Redis only when needed
- ✅ **Scalable**: Multiple consumers possible
- ✅ **Railway Optimized**: Leverage available services

#### **Cons**
- ❌ **Complexity**: Dual implementation
- ❌ **Maintenance**: Two code paths
- ❌ **Testing**: More scenarios to cover

## **Recommended Implementation: Redis Pub/Sub**

### **Phase 1: Infrastructure Setup**

#### **1.1 Redis Configuration**
```csharp
// appsettings.json
{
  "Redis": {
    "ConnectionString": "your-redis-connection",
    "EventBus": {
      "Enabled": true,
      "ChannelPrefix": "normaize:jobs:",
      "MaxRetryAttempts": 3,
      "RetryDelayMs": 1000
    }
  }
}
```

#### **1.2 Event Bus Interface**
```csharp
public interface IEventBus
{
    Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default);
    Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string channel, CancellationToken cancellationToken = default);
}
```

#### **1.3 Redis Event Bus Implementation**
```csharp
public class RedisEventBus : IEventBus, IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisEventBus> _logger;
    private readonly Dictionary<string, ISubscriber> _subscribers = new();

    public async Task PublishAsync<T>(string channel, T message, CancellationToken cancellationToken = default)
    {
        var subscriber = _redis.GetSubscriber();
        var serializedMessage = JsonSerializer.Serialize(message);
        await subscriber.PublishAsync(channel, serializedMessage);
    }

    public async Task SubscribeAsync<T>(string channel, Func<T, Task> handler, CancellationToken cancellationToken = default)
    {
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(channel, async (_, value) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(value!);
                if (message != null)
                {
                    await handler(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from channel {Channel}", channel);
            }
        });
        
        _subscribers[channel] = subscriber;
    }
}
```

### **Phase 2: Event-Driven Service**

#### **2.1 Event-Driven Background Service**
```csharp
public class EventDrivenDataNormalizationService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IJobQueueService _jobQueueService;
    private readonly ILogger<EventDrivenDataNormalizationService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event-driven data normalization service started");

        // Subscribe to job events
        await _eventBus.SubscribeAsync<DataNormalizationJobEvent>(
            "normaize:jobs:created",
            async (jobEvent) =>
            {
                _logger.LogInformation("Received job event: {JobId}", jobEvent.JobId);
                await ProcessJobAsync(jobEvent.JobId, stoppingToken);
            },
            stoppingToken);

        // Keep service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessJobAsync(int jobId, CancellationToken cancellationToken)
    {
        // Process job logic here
        // Similar to current implementation
    }
}
```

#### **2.2 Job Event Model**
```csharp
public class DataNormalizationJobEvent
{
    public int JobId { get; set; }
    public int DataSetId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
```

### **Phase 3: Migration Strategy**

#### **3.1 Gradual Migration**
```csharp
// Configuration-driven approach
public class DataNormalizationOptions
{
    public bool UseEventDriven { get; set; } = false;
    public bool EnableFallbackPolling { get; set; } = true;
    public TimeSpan FallbackPollingInterval { get; set; } = TimeSpan.FromMinutes(5);
}
```

#### **3.2 Service Factory**
```csharp
public static class DataNormalizationServiceFactory
{
    public static IHostedService CreateService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var useEventDriven = configuration.GetValue<bool>("DataNormalization:UseEventDriven");
        
        if (useEventDriven)
        {
            return serviceProvider.GetRequiredService<EventDrivenDataNormalizationService>();
        }
        
        return serviceProvider.GetRequiredService<DataNormalizationBackgroundService>();
    }
}
```

## **Railway-Specific Considerations**

### **Infrastructure Costs**
- **Current**: $0 additional (uses existing database)
- **Redis Option**: $5-10/month (Redis add-on)
- **Hybrid**: $2-5/month (conditional Redis usage)

### **Scaling Strategy**
- **Single Instance**: Current polling approach
- **Multiple Instances**: Event-driven with Redis
- **Auto-scaling**: Railway handles infrastructure scaling

### **Monitoring & Observability**
- **Redis Metrics**: Built-in monitoring
- **Application Metrics**: Custom event tracking
- **Railway Dashboard**: Infrastructure monitoring

## **Implementation Timeline**

### **Week 1: Infrastructure**
- [ ] Redis add-on setup
- [ ] Event bus interface and implementation
- [ ] Configuration updates

### **Week 2: Service Migration**
- [ ] Event-driven service implementation
- [ ] Event models and handlers
- [ ] Service factory and configuration

### **Week 3: Testing & Validation**
- [ ] Unit tests for event handling
- [ ] Integration tests with Redis
- [ ] Performance testing

### **Week 4: Deployment & Monitoring**
- [ ] Gradual rollout
- [ ] Monitoring setup
- [ ] Performance optimization

## **Success Metrics**

### **Performance Improvements**
- **Job Pickup Latency**: 5 minutes → <100ms
- **Resource Usage**: 20% reduction during idle
- **Scalability**: Support for multiple consumers

### **Cost Analysis**
- **Current**: $0 additional
- **Event-Driven**: $5-10/month
- **ROI**: Justified by improved performance and scalability

## **Risk Mitigation**

### **Redis Failures**
- Fallback to database polling
- Circuit breaker pattern
- Health checks and monitoring

### **Event Ordering**
- Correlation IDs for job tracking
- Idempotent job processing
- Event persistence for replay

### **Scaling Issues**
- Connection pooling
- Event partitioning
- Load balancing strategies

## **Conclusion**

The **Redis Pub/Sub approach** provides the best balance of:
- **Performance**: Real-time job processing
- **Scalability**: Multiple consumer support
- **Railway Compatibility**: Managed service integration
- **Cost Effectiveness**: Reasonable additional cost
- **Reliability**: Built-in persistence and clustering

This approach will transform the service from a polling-based system to a modern, event-driven architecture while maintaining Railway compatibility and providing a clear migration path.

