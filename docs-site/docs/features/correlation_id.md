# Correlation ID Tracking

## Overview

The Correlation ID tracking system provides distributed request tracing capabilities across the application and its services. This middleware-based implementation automatically generates or propagates correlation identifiers for every HTTP request, enabling developers to trace request flows through logs, monitor distributed operations, and debug issues across service boundaries.

The system is built on ASP.NET Core middleware pipeline and uses dependency injection to provide correlation ID access throughout the application lifecycle. It follows a header-based propagation pattern, reading correlation IDs from incoming requests and writing them to outgoing responses.

## Correlation ID System

### Architecture Components

The correlation tracking system consists of four primary components:

| Component | Type | Lifetime | Purpose |
|-----------|------|----------|---------|
| `CorrelationIdMiddleware` | Middleware | Transient | Intercepts HTTP requests and manages correlation ID lifecycle |
| `ICurrentCorrelationIdService` | Service | Scoped | Provides access to the current request's correlation ID |
| `ICorrelationIdRequestReader` | Service | Transient | Reads correlation IDs from incoming HTTP headers |
| `ICorrelationIdResponseWriter` | Service | Transient | Writes correlation IDs to outgoing HTTP headers |

### Request Tracing Flow

The correlation ID flows through the application in the following sequence:

1. **Request Arrival**: Middleware intercepts the incoming HTTP request
2. **ID Resolution**: System attempts to read correlation ID from request headers
3. **ID Generation**: If no correlation ID exists, a new GUID is generated
4. **Context Storage**: Correlation ID is stored in scoped service for request duration
5. **Response Writing**: Correlation ID is written to response headers before sending
6. **Request Completion**: Scoped correlation service is disposed

### Correlation ID Generation

When a correlation ID is not provided in the incoming request, the system generates a new one:

```csharp
private static Guid GenerateCorrelationId() => Guid.NewGuid();
```

This ensures every request has a unique identifier, even when the request originates from clients that don't support correlation tracking.

### Header Propagation

The system uses a standardized header name for correlation ID propagation:

```csharp
private const string CorrelationIdHeaderKey = "CorrelationId";
```

This header is:
- **Read** from incoming requests to maintain correlation across service boundaries
- **Written** to outgoing responses to enable client-side correlation tracking
- **Case-sensitive** as per HTTP header specifications

## Implementation

### Correlation Middleware

The `CorrelationIdMiddleware` is the core component that orchestrates correlation ID management within the ASP.NET Core request pipeline:

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICorrelationIdRequestReader _correlationIdRequestReader;
    private readonly ICorrelationIdResponseWriter _correlationIdResponseWriter;

    public CorrelationIdMiddleware(
        RequestDelegate next, 
        ICorrelationIdRequestReader correlationIdRequestReader, 
        ICorrelationIdResponseWriter correlationIdResponseWriter)
    {
        _next = next;
        _correlationIdRequestReader = correlationIdRequestReader;
        _correlationIdResponseWriter = correlationIdResponseWriter;
    }

    public async Task Invoke(
        HttpContext context, 
        ICurrentCorrelationIdService currentCorrelationIdService)
    {
        Contract.Requires(currentCorrelationIdService != null);
        
        // Read existing correlation ID or generate new one
        var correlationId = _correlationIdRequestReader.Read(context) 
            ?? GenerateCorrelationId();
        
        // Store in scoped service for request duration
        currentCorrelationIdService.SetId(correlationId);

        Contract.Requires(context != null);
        
        // Register callback to write correlation ID to response headers
        context.Response.OnStarting(() =>
        {
            _correlationIdResponseWriter.Write(context, correlationId);
            return Task.CompletedTask;
        });

        // Continue pipeline execution
        await _next(context);
    }
}
```

**Key Implementation Details:**

- **Constructor Injection**: Middleware receives reader and writer services via constructor, while the current correlation service is injected per-request via the `Invoke` method
- **Contract Validation**: Uses `Contract.Requires` to validate method preconditions for null checks
- **Response Callback**: Uses `OnStarting` to ensure headers are written before response body begins streaming
- **Async Pipeline**: Properly awaits the next middleware delegate to maintain async flow

### Current Correlation Service

The `CurrentCorrelationIdService` provides scoped access to the correlation ID throughout the request lifecycle:

```csharp
public class CurrentCorrelationIdService : ICurrentCorrelationIdService
{
    private Guid? _correlationId;

    public CorrelationId Current()
    {
        if (_correlationId.HasValue)
        {
            return new CorrelationId(_correlationId.Value);
        }

        throw new InvalidOperationException(
            FormattableString.Invariant($"CorrelationId has not been set"));
    }

    public void SetId(Guid correlationId)
    {
        _correlationId = correlationId;
    }
}
```

**Design Considerations:**

- **Scoped Lifetime**: Registered as scoped to ensure each request has its own instance
- **Nullable Storage**: Uses `Guid?` to distinguish between unset and set states
- **Fail-Fast Behavior**: Throws exception if accessed before middleware sets the ID
- **Immutable Return**: Returns a `CorrelationId` value object rather than exposing the raw GUID

### Header Reading and Writing

The `CorrelationIdHeaderService` implements both reading and writing interfaces:

```csharp
public class CorrelationIdHeaderService : 
    ICorrelationIdRequestReader, 
    ICorrelationIdResponseWriter
{
    private const string CorrelationIdHeaderKey = "CorrelationId";

    public Guid? Read(HttpContext context)
    {
        Contract.Requires(context != null);
        var correlationId = context.Request.Headers[CorrelationIdHeaderKey];
        if (Guid.TryParse(correlationId.ToString(), out var result))
        {
            return result;
        }
        else
        {
            return null;
        }
    }

    public void Write(HttpContext context, Guid correlationId)
    {
        Contract.Requires(context != null);
        context.Response.Headers[CorrelationIdHeaderKey] = correlationId.ToString();
    }
}
```

**Implementation Notes:**

- **Contract Validation**: Uses `Contract.Requires` to validate method preconditions for null checks
- **Safe Parsing**: Uses `TryParse` to handle malformed correlation IDs gracefully
- **Null Return**: Returns `null` for invalid or missing headers, triggering ID generation
- **String Conversion**: Converts GUID to standard string format for HTTP header compatibility
- **Single Responsibility**: Encapsulates all header interaction logic in one service

### Value Object Pattern

The `CorrelationId` class serves as a value object wrapper:

```csharp
public class CorrelationId
{
    public CorrelationId(Guid current)
    {
        Current = current;
    }

    public Guid Current { get; }
}
```

This provides:
- **Type Safety**: Prevents accidental mixing of correlation IDs with other GUIDs
- **Immutability**: Read-only property ensures correlation ID cannot be modified after creation
- **Semantic Clarity**: Makes code more self-documenting when used in method signatures

## Usage Patterns

### Accessing Correlation ID in Code

Inject `ICurrentCorrelationIdService` into any service, controller, or middleware to access the current correlation ID:

```csharp
public class OrderService
{
    private readonly ICurrentCorrelationIdService _correlationIdService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ICurrentCorrelationIdService correlationIdService,
        ILogger<OrderService> logger)
    {
        _correlationIdService = correlationIdService;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(OrderRequest request)
    {
        var correlationId = _correlationIdService.Current();
        
        _logger.LogInformation(
            "Creating order with correlation ID: {CorrelationId}", 
            correlationId.Current);
        
        // Business logic here
    }
}
```

**Best Practices:**

- Always inject `ICurrentCorrelationIdService` rather than storing correlation IDs in static fields
- Access the correlation ID only when needed (lazy evaluation)
- Handle the `InvalidOperationException` if accessing outside the request pipeline

### Logging with Correlation IDs

Integrate correlation IDs into structured logging for request tracing:

```csharp
public class PaymentProcessor
{
    private readonly ICurrentCorrelationIdService _correlationIdService;
    private readonly ILogger<PaymentProcessor> _logger;

    public async Task ProcessPaymentAsync(Payment payment)
    {
        var correlationId = _correlationIdService.Current().Current;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["PaymentId"] = payment.Id
        }))
        {
            _logger.LogInformation("Starting payment processing");
            
            try
            {
                // Process payment
                _logger.LogInformation("Payment processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed");
                throw;
            }
        }
    }
}
```

This pattern enables:
- **Automatic correlation ID inclusion** in all log entries within the scope
- **Centralized log querying** by correlation ID across distributed systems
- **Request flow visualization** in log aggregation tools

See [Exception Handling](/features/exception_handling.md) for integration with error logging.

### Distributed Tracing

Propagate correlation IDs to downstream services using `HttpClient`:

```csharp
public class ExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ICurrentCorrelationIdService _correlationIdService;

    public ExternalApiClient(
        HttpClient httpClient,
        ICurrentCorrelationIdService correlationIdService)
    {
        _httpClient = httpClient;
        _correlationIdService = correlationIdService;
    }

    public async Task<ApiResponse> CallExternalServiceAsync(string endpoint)
    {
        var correlationId = _correlationIdService.Current().Current;
        
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("CorrelationId", correlationId.ToString());
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsAsync<ApiResponse>();
    }
}
```

**Integration with Polly:**

When using Polly for resilience (see tech stack), correlation IDs are automatically maintained across retries:

```csharp
services.AddHttpClient<ExternalApiClient>()
    .AddPolicyHandler((serviceProvider, request) =>
    {
        var correlationService = serviceProvider
            .GetRequiredService<ICurrentCorrelationIdService>();
        var correlationId = correlationService.Current().Current;
        
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Correlation ID is preserved across retries
                    Console.WriteLine(
                        $"Retry {retryCount} for correlation {correlationId}");
                });
    });
```

See [Monitoring](/deployment/monitoring.md) for correlation ID usage in production monitoring.

## Configuration

### Service Registration

Register correlation services in `Startup.cs` or `Program.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register correlation services
    services.AddCorrelation();
    
    // Other service registrations
    services.AddControllers();
    services.AddSwaggerGen();
}
```

The `AddCorrelation` extension method registers all required services:

```csharp
public static class CorrelationServiceCollectionExtensions
{
    public static IServiceCollection AddCorrelation(this IServiceCollection services)
    {
        // TryAdd ensures services aren't registered multiple times
        services.TryAddTransient<ICorrelationIdRequestReader, CorrelationIdHeaderService>();
        services.TryAddTransient<ICorrelationIdResponseWriter, CorrelationIdHeaderService>();
        services.TryAddScoped<ICurrentCorrelationIdService, CurrentCorrelationIdService>();

        return services;
    }
}
```

**Service Lifetime Rationale:**

- **Transient** for reader/writer: Stateless services that can be created per-use
- **Scoped** for current correlation service: Must maintain state for the request duration
- **TryAdd** pattern: Allows custom implementations to be registered before calling `AddCorrelation`

### Middleware Setup

Add the correlation middleware to the request pipeline:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Add correlation middleware early in the pipeline
    app.UseMiddleware<CorrelationIdMiddleware>();
    
    // Exception handling should come after correlation
    app.UseExceptionHandler("/error");
    
    // Standard middleware
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

**Pipeline Positioning:**

The correlation middleware should be positioned:
- **After** static file middleware (if correlation isn't needed for static files)
- **Before** exception handling middleware to ensure errors include correlation IDs
- **Before** authentication/authorization to trace security-related requests
- **Before** endpoint routing to ensure all endpoints have correlation IDs

See [Middleware Pipeline](/architecture/middleware_pipeline.md) for complete pipeline configuration.

### Header Configuration

The default header name is `CorrelationId`, but you can customize it by implementing a custom header service:

```csharp
public class CustomCorrelationIdHeaderService : 
    ICorrelationIdRequestReader, 
    ICorrelationIdResponseWriter
{
    private const string CustomHeaderKey = "X-Request-ID"; // Custom header name

    public Guid? Read(HttpContext context)
    {
        var correlationId = context.Request.Headers[CustomHeaderKey];
        if (Guid.TryParse(correlationId.ToString(), out var result))
        {
            return result;
        }
        return null;
    }

    public void Write(HttpContext context, Guid correlationId)
    {
        context.Response.Headers[CustomHeaderKey] = correlationId.ToString();
    }
}
```

Register the custom implementation before calling `AddCorrelation`:

```csharp
services.AddTransient<ICorrelationIdRequestReader, CustomCorrelationIdHeaderService>();
services.AddTransient<ICorrelationIdResponseWriter, CustomCorrelationIdHeaderService>();
services.AddCorrelation(); // Will use TryAdd, so custom services take precedence
```

## Advanced Scenarios

### Health Check Integration

Correlation IDs can be included in health check responses for diagnostic purposes:

```csharp
public class CorrelationAwareHealthCheck : IHealthCheck
{
    private readonly ICurrentCorrelationIdService _correlationIdService;

    public CorrelationAwareHealthCheck(ICurrentCorrelationIdService correlationIdService)
    {
        _correlationIdService = correlationIdService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var correlationId = _correlationIdService.Current().Current;
            
            return Task.FromResult(HealthCheckResult.Healthy(
                "Service is healthy",
                new Dictionary<string, object>
                {
                    ["correlationId"] = correlationId
                }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Correlation service unavailable", ex));
        }
    }
}
```

### Entity Framework Core Integration

Include correlation IDs in database audit fields:

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentCorrelationIdService _correlationIdService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentCorrelationIdService correlationIdService) 
        : base(options)
    {
        _correlationIdService = correlationIdService;
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.Current().Current;
        
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.CorrelationId = correlationId;
                entry.Entity.ModifiedDate = DateTime.UtcNow;
            }
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

## Troubleshooting

### Common Issues

**Issue: `InvalidOperationException` when accessing correlation ID**

```
System.InvalidOperationException: CorrelationId has not been set
```

**Causes:**
- Accessing correlation ID before middleware executes
- Accessing correlation ID outside HTTP request context (background jobs, startup code)
- Middleware not registered in pipeline

**Solutions:**
- Ensure `UseMiddleware<CorrelationIdMiddleware>()` is called in `Configure`
- For background jobs, generate and set correlation IDs manually
- Check middleware ordering in the pipeline

**Issue: Correlation IDs not appearing in logs**

**Causes:**
- Logging scope not configured
- Correlation ID not added to log context
- Logger configuration filtering correlation properties

**Solutions:**
- Use `BeginScope` with correlation ID in structured logging
- Configure log enrichers to automatically include correlation IDs
- Verify logger configuration includes custom properties

**Issue: Correlation IDs not propagating to downstream services**

**Causes:**
- Header not added to outgoing HTTP requests
- Different header names between services
- HttpClient not configured with correlation propagation

**Solutions:**
- Manually add correlation header to outgoing requests
- Standardize header names across all services
- Create delegating handler for automatic header propagation

### Performance Considerations

- **GUID Generation**: Minimal overhead (~100ns per generation)
- **Header Parsing**: String parsing adds negligible latency (<1ms)
- **Scoped Service**: No memory leaks as service is disposed per-request
- **Response Callback**: `OnStarting` callback has minimal performance impact

### Security Considerations

- **Information Disclosure**: Correlation IDs are GUIDs and don't expose sensitive information
- **Header Injection**: No risk as correlation IDs are validated GUIDs
- **Log Injection**: Correlation IDs are structured data, not free-form text
- **Rate Limiting**: Consider correlation IDs when implementing rate limiting to track request patterns

## Testing

### Unit Testing

Mock the correlation service for unit tests:

```csharp
[Fact]
public async Task Service_Should_Use_CorrelationId()
{
    // Arrange
    var correlationId = Guid.NewGuid();
    var mockCorrelationService = new Mock<ICurrentCorrelationIdService>();
    mockCorrelationService
        .Setup(x => x.Current())
        .Returns(new CorrelationId(correlationId));
    
    var service = new OrderService(mockCorrelationService.Object);
    
    // Act
    await service.CreateOrderAsync(new OrderRequest());
    
    // Assert
    mockCorrelationService.Verify(x => x.Current(), Times.Once);
}
```

### Integration Testing

Test correlation ID propagation in integration tests:

```csharp
[Fact]
public async Task Request_Should_Return_CorrelationId_Header()
{
    // Arrange
    var client = _factory.CreateClient();
    var correlationId = Guid.NewGuid();
    client.DefaultRequestHeaders.Add("CorrelationId", correlationId.ToString());
    
    // Act
    var response = await client.GetAsync("/api/orders");
    
    // Assert
    response.Headers.TryGetValues("CorrelationId", out var values);
    Assert.Equal(correlationId.ToString(), values.First());
}
```