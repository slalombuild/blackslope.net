# Health Check API

## Overview

The Health Check API provides comprehensive monitoring endpoints for the BlackSlope application and its dependencies. Built on ASP.NET Core's health check framework, it enables orchestration systems (like Kubernetes), monitoring tools, and operations teams to verify the application's operational status and readiness to serve traffic.

The implementation leverages the `Microsoft.Extensions.Diagnostics.HealthChecks` framework along with specialized providers for SQL Server (`AspNetCore.HealthChecks.SqlServer` v5.0.3) and Entity Framework Core (`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` v6.0.1).

## Architecture

The health check system is organized into several key components:

- **Health Check Implementations**: Custom health checks that verify specific dependencies
- **Tag-Based Filtering**: Categorization system for grouping related health checks
- **Response Writers**: Custom JSON formatters for health check results
- **Configuration**: Endpoint configuration through `HostConfig`

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Health Check Endpoints                    │
│  /health, /health/movies, /health/database, /health/api     │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
    ┌────▼─────┐          ┌─────▼──────┐
    │  Tagged  │          │  Response  │
    │ Filtering│          │   Writer   │
    └────┬─────┘          └─────┬──────┘
         │                      │
    ┌────▼──────────────────────▼─────┐
    │     Health Check Registry        │
    └────┬─────────────────────────────┘
         │
    ┌────┴─────────────────────────────┐
    │                                   │
┌───▼────────┐  ┌──────────────┐  ┌───▼──────────┐
│ SQL Server │  │ Movies API   │  │   Custom     │
│   Check    │  │    Check     │  │   Checks     │
└────────────┘  └──────────────┘  └──────────────┘
```

## Endpoints

### GET /health

The primary health check endpoint that executes all registered health checks regardless of tags.

**Response Format:**
```json
{
  "status": "Healthy",
  "details": [
    {
      "key": "MOVIES.DB",
      "value": "Healthy",
      "description": null,
      "duration": "00:00:00.0234567",
      "exception": null
    },
    {
      "key": "MOVIES.API",
      "value": "Healthy",
      "description": null,
      "duration": "00:00:00.1234567",
      "exception": null
    }
  ]
}
```

**Status Codes:**
- `200 OK`: All health checks passed (Healthy)
- `503 Service Unavailable`: One or more health checks failed (Unhealthy or Degraded)

**Use Case:** General application health monitoring and dashboard displays.

### GET /health/movies

Filtered endpoint that executes only health checks tagged with `movies`. This includes both the Movies database connection and the Movies API availability check.

**Included Checks:**
- `MOVIES.DB`: SQL Server connectivity for the Movies database
- `MOVIES.API`: Movies API endpoint availability

**Use Case:** Targeted monitoring of the Movies subsystem dependencies.

### GET /health/database

Filtered endpoint for all database-related health checks.

**Included Checks:**
- `MOVIES.DB`: SQL Server connectivity

**Use Case:** Database-specific monitoring and alerting.

### GET /health/api

Filtered endpoint for external API dependencies.

**Included Checks:**
- `MOVIES.API`: Movies API endpoint availability

**Use Case:** Monitoring external service dependencies separately from internal resources.

### GET /health/ready

**Note:** While mentioned in the summary, this endpoint is not explicitly implemented in the provided source code. To implement a readiness probe, add the following to `HealthCheckStartup.Configure`:

```csharp
app.UseHealthChecks($"{endpoint}/ready", new HealthCheckOptions()
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter,
});
```

**Intended Use:** Kubernetes readiness probes to determine if the pod should receive traffic.

### GET /health/live

**Note:** While mentioned in the summary, this endpoint is not explicitly implemented in the provided source code. To implement a liveness probe, add the following to `HealthCheckStartup.Configure`:

```csharp
app.UseHealthChecks($"{endpoint}/live", new HealthCheckOptions()
{
    Predicate = _ => true, // Or specific lightweight checks
    ResponseWriter = HealthCheckResponseWriter,
});
```

**Intended Use:** Kubernetes liveness probes to determine if the pod should be restarted.

## Health Check Components

### SQL Server Connectivity Check

Automatically registered through the `AddSqlServer` extension method, this check verifies connectivity to the Movies database.

**Configuration:**

```csharp
var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
    .Get<MovieRepositoryConfiguration>();

services.AddHealthChecks()
    .AddSqlServer(
        config.MoviesConnectionString, 
        name: "MOVIES.DB", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database }
    );
```

**Implementation Details:**
- Uses the connection string from `MovieRepositoryConfiguration`
- Executes a simple query to verify database connectivity
- Timeout and retry behavior inherited from the connection string settings
- Tagged with both `movies` and `database` for flexible filtering

**Failure Scenarios:**
- Database server unreachable
- Invalid credentials
- Network connectivity issues
- Database not accepting connections (e.g., during maintenance)

### Movies API Health Check

Custom implementation (`MoviesHealthCheck`) that verifies the availability of the Movies API. The implementation is defined in a separate file (`BlackSlope.Api.HealthChecks.MoviesHealthCheck`) and registered through the health check system.

**Registration:**

```csharp
services.AddHealthChecks()
    .AddCheck<MoviesHealthCheck>(
        "MOVIES.API", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api }
    );
```

**Implementation Notes:**
- The `MoviesHealthCheck` class is referenced in the registration but its implementation is not shown in `HealthCheckStartup.cs`
- Likely performs HTTP requests to verify the Movies API endpoint availability
- Should use `IHttpClientFactory` for proper HttpClient lifecycle management
- Expected to handle cancellation tokens and exceptions gracefully

### Entity Framework Core Context Check

While not explicitly shown in the provided source files, the `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` package (v6.0.1) is included in the tech stack, enabling DbContext health checks.

**Typical Implementation:**

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<MoviesDbContext>(
        name: "MOVIES.EF.CONTEXT",
        tags: new[] { HealthCheckTag.Database }
    );
```

**What It Checks:**
- DbContext can be resolved from DI container
- Database connection can be established
- Basic query execution succeeds

## Tag-Based Filtering System

The health check system uses a tag-based architecture for organizing and filtering checks.

**Tag Definitions:**

```csharp
public static class HealthCheckTag
{
    public const string Movies = "movies";
    public const string Database = "database";
    public const string Api = "api";

    public static IEnumerable<string> All
    {
        get
        {
            return new List<string>()
            {
                Movies,
                Database,
                Api,
            };
        }
    }
}
```

**Tag Assignment Strategy:**

| Health Check | Tags | Rationale |
|--------------|------|-----------|
| MOVIES.DB | `movies`, `database` | Database dependency for Movies feature |
| MOVIES.API | `movies`, `api` | External API dependency for Movies feature |

**Dynamic Endpoint Registration:**

```csharp
foreach (var tag in HealthCheckTag.All)
{
    app.UseHealthChecks($"{endpoint}/{tag}", new HealthCheckOptions()
    {
        Predicate = registration => registration.Tags.Contains(tag),
        ResponseWriter = HealthCheckResponseWriter,
    });
}
```

This approach automatically creates filtered endpoints for each defined tag, ensuring consistency and reducing boilerplate code.

## Configuration

Health check endpoints are configured through the `HostConfig` class, which is bound from the application's configuration file.

**Configuration Class:**

```csharp
public class HealthChecksConfig
{
    public string Endpoint { get; set; }
}
```

**Usage in Startup:**

```csharp
public static void Configure(
    IApplicationBuilder app, 
    IWebHostEnvironment env, 
    HostConfig hostConfig)
{
    var endpoint = hostConfig.HealthChecks.Endpoint;
    app.UseHealthChecks(endpoint, new HealthCheckOptions()
    {
        Predicate = _ => true,
        ResponseWriter = HealthCheckResponseWriter,
    });
    
    // Additional endpoint registrations...
}
```

**Example appsettings.json:**

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "Server=...;Database=Movies;...",
    "HealthChecks": {
      "Endpoint": "/health"
    }
  }
}
```

**Note:** The configuration section name matches the executing assembly name (`Assembly.GetExecutingAssembly().GetName().Name`), which is used to retrieve the `MovieRepositoryConfiguration` containing the connection string.

**Configuration Considerations:**
- The endpoint path should not conflict with API routes
- Consider using a non-standard path for security through obscurity in production
- Ensure the endpoint is accessible to monitoring systems but potentially restricted from public access

## Custom Response Writer

The health check system uses a custom response writer to format results as JSON with detailed information.

**Implementation:**

```csharp
private static async Task HealthCheckResponseWriter(HttpContext c, HealthReport r)
{
    c.Response.ContentType = MediaTypeNames.Application.Json;

    var result = JsonSerializer.Serialize(new
    {
        status = r.Status.ToString(),
        details = r.Entries.Select(e => new
        {
            key = e.Key,
            value = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration = e.Value.Duration,
            exception = e.Value.Exception,
        }),
    });

    await c.Response.WriteAsync(result).ConfigureAwait(false);
}
```

**Response Structure:**

```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "details": [
    {
      "key": "CHECK_NAME",
      "value": "Healthy|Degraded|Unhealthy",
      "description": "Optional description",
      "duration": "00:00:00.0123456",
      "exception": {
        "message": "Error details if failed",
        "stackTrace": "..."
      }
    }
  ]
}
```

**Key Features:**
- Uses `System.Text.Json` (v6.0.10) for high-performance serialization
- Includes execution duration for performance monitoring
- Exposes exception details for debugging (consider filtering in production)
- Sets proper `Content-Type` header

**Security Consideration:**
The current implementation exposes full exception details, including stack traces. For production environments, consider filtering sensitive information:

```csharp
exception = env.IsDevelopment() ? e.Value.Exception : null
```

## Integration with Startup

The health check system is integrated into the application startup through a dedicated `HealthCheckStartup` class, following the separation of concerns principle.

**Service Registration (ConfigureServices):**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... other service registrations
    
    HealthCheckStartup.ConfigureServices(services, Configuration);
    
    // ... remaining service registrations
}
```

**Middleware Registration (Configure):**

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env, HostConfig hostConfig)
{
    // ... early middleware (exception handling, etc.)
    
    HealthCheckStartup.Configure(app, env, hostConfig);
    
    // ... remaining middleware and routing
}
```

**Design Benefits:**
- Encapsulates health check configuration logic
- Improves testability by isolating health check setup
- Simplifies the main `Startup` class
- Allows for easy enabling/disabling of health checks

## Kubernetes Integration

For Kubernetes deployments, health check endpoints should be configured as probes in the pod specification.

**Example Deployment YAML:**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: blackslope-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: blackslope-api:latest
        ports:
        - containerPort: 80
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
```

**Probe Configuration Guidelines:**

| Probe Type | Recommended Endpoint | Purpose |
|------------|---------------------|---------|
| Liveness | `/health` or `/health/live` | Determines if container should be restarted |
| Readiness | `/health` or `/health/ready` | Determines if pod should receive traffic |
| Startup | `/health` | Determines if application has started (for slow-starting apps) |

**Timing Recommendations:**
- **initialDelaySeconds**: Set based on application startup time (typically 10-30 seconds)
- **periodSeconds**: How often to check (5-10 seconds for readiness, 10-30 for liveness)
- **timeoutSeconds**: Maximum time to wait for response (3-5 seconds)
- **failureThreshold**: Number of consecutive failures before action (2-3 for readiness, 3-5 for liveness)

See [/deployment/kubernetes.md](/deployment/kubernetes.md) for complete Kubernetes deployment configuration.

## Monitoring and Alerting

### Prometheus Integration

To integrate with Prometheus, consider adding the `AspNetCore.HealthChecks.Publisher.Prometheus` package:

```csharp
services.AddHealthChecks()
    .AddSqlServer(...)
    .AddCheck<MoviesHealthCheck>(...)
    .ForwardToPrometheus();
```

### Application Insights

For Azure deployments, health check results can be logged to Application Insights:

```csharp
services.AddHealthChecks()
    .AddApplicationInsightsPublisher();
```

### Custom Alerting

Implement a custom health check publisher for integration with alerting systems:

```csharp
public class AlertingHealthCheckPublisher : IHealthCheckPublisher
{
    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        if (report.Status == HealthStatus.Unhealthy)
        {
            // Send alert to monitoring system
        }
    }
}
```

## Performance Considerations

### Execution Timeout

Health checks should complete quickly to avoid blocking monitoring systems:

```csharp
services.AddHealthChecks()
    .AddSqlServer(
        config.MoviesConnectionString,
        timeout: TimeSpan.FromSeconds(3),
        name: "MOVIES.DB"
    );
```

### Caching Results

For expensive health checks, consider implementing caching using `Microsoft.Extensions.Caching.Memory` (v6.0.2):

```csharp
public class CachedMoviesHealthCheck : IHealthCheck
{
    private readonly IMemoryCache _cache;
    private readonly MoviesHealthCheck _innerCheck;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync("movies-health", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await _innerCheck.CheckHealthAsync(context, cancellationToken);
        });
    }
}
```

### Parallel Execution

The health check framework executes checks in parallel by default, but you can control this behavior:

```csharp
app.UseHealthChecks("/health", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter,
    AllowCachingResponses = false, // Disable HTTP caching
});
```

## Troubleshooting

### Common Issues

**Issue: Health checks always return Unhealthy**
- Verify connection strings in configuration
- Check network connectivity to dependencies
- Review exception details in the response
- Ensure HttpClient is properly configured with base address

**Issue: Health checks timeout**
- Increase timeout values in health check configuration
- Verify database query performance
- Check for network latency issues
- Review Polly timeout policies on HttpClient

**Issue: Inconsistent results**
- Check for transient network issues
- Review Polly retry policies
- Verify database connection pool settings
- Monitor for resource exhaustion (connections, memory)

### Debugging

Enable detailed logging for health checks:

```csharp
services.AddLogging(builder =>
{
    builder.AddFilter("Microsoft.Extensions.Diagnostics.HealthChecks", LogLevel.Debug);
});
```

Add custom logging to health checks:

```csharp
public class MoviesHealthCheck : IHealthCheck
{
    private readonly ILogger<MoviesHealthCheck> _logger;
    
    public async Task<HealthCheckResult> CheckHealthAsync(...)
    {
        _logger.LogInformation("Executing Movies API health check");
        
        try
        {
            var result = await _httpClientFactory.CreateClient("movies")
                .GetAsync("api/version", cancellationToken);
                
            _logger.LogInformation(
                "Movies API health check completed with status: {StatusCode}", 
                result.StatusCode);
                
            return result.IsSuccessStatusCode
                ? new HealthCheckResult(HealthStatus.Healthy)
                : new HealthCheckResult(HealthStatus.Unhealthy);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Movies API health check failed");
            return new HealthCheckResult(HealthStatus.Unhealthy, exception: e);
        }
    }
}
```

## Best Practices

1. **Keep Checks Lightweight**: Health checks should complete in seconds, not minutes
2. **Use Appropriate Tags**: Tag checks logically for flexible filtering
3. **Handle Exceptions Gracefully**: Always catch and report exceptions rather than throwing
4. **Implement Timeouts**: Prevent hanging health checks from blocking monitoring
5. **Cache Expensive Checks**: Use in-memory caching for checks that don't need real-time results
6. **Monitor Check Duration**: Track execution time to identify performance issues
7. **Secure Endpoints**: Consider authentication/authorization for health check endpoints in production
8. **Filter Sensitive Data**: Don't expose connection strings, credentials, or internal details in responses
9. **Test Failure Scenarios**: Regularly test that health checks correctly identify failures
10. **Document Dependencies**: Clearly document what each health check verifies

## Related Documentation

- [Health Check Features](/features/health_checks.md) - Detailed feature documentation
- [Kubernetes Deployment](/deployment/kubernetes.md) - Kubernetes configuration and deployment
- [Movies API Reference](/api_reference/movies_api.md) - Movies API endpoint documentation

## Extension Points

### Adding New Health Checks

To add a new health check:

1. Create a class implementing `IHealthCheck`:

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Implement check logic
        var isHealthy = await PerformCheckAsync(cancellationToken);
        
        return isHealthy
            ? HealthCheckResult.Healthy("Check passed")
            : HealthCheckResult.Unhealthy("Check failed");
    }
}
```

2. Register in `HealthCheckStartup.ConfigureServices`:

```csharp
services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>(
        "CUSTOM.CHECK",
        tags: new[] { "custom", HealthCheckTag.Api }
    );
```

3. Add new tag to `HealthCheckTag` if needed:

```csharp
public static class HealthCheckTag
{
    public const string Custom = "custom";
    // ... existing tags
}
```

### Custom Response Formats

To implement a custom response format (e.g., XML):

```csharp
private static async Task XmlHealthCheckResponseWriter(HttpContext c, HealthReport r)
{
    c.Response.ContentType = MediaTypeNames.Application.Xml;
    
    var xml = new XDocument(
        new XElement("HealthReport",
            new XAttribute("status", r.Status),
            r.Entries.Select(e => new XElement("Check",
                new XAttribute("name", e.Key),
                new XAttribute("status", e.Value.Status),
                new XAttribute("duration", e.Value.Duration)
            ))
        )
    );
    
    await c.Response.WriteAsync(xml.ToString()).ConfigureAwait(false);
}
```