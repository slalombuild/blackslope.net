# Health Checks

## Health Check Architecture

The BlackSlope API implements a comprehensive health check system built on ASP.NET Core's native health check framework. The architecture provides both aggregate and granular health monitoring capabilities, enabling infrastructure components (Kubernetes, load balancers, monitoring systems) to assess application readiness and liveness.

### Core Components

The health check system consists of three primary components:

1. **Health Check Providers**: Individual health check implementations that verify specific system components
2. **Health Check Tags**: Categorization system for organizing and filtering health checks
3. **Health Check Endpoints**: HTTP endpoints that expose health status in JSON format

### Configuration

Health checks are configured in the `HealthCheckStartup` class, which provides a dedicated startup configuration separate from the main application startup. This separation allows for focused health check configuration and easier testing.

```csharp
public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<MovieRepositoryConfiguration>();

    services.AddHealthChecks()
        .AddSqlServer(config.MoviesConnectionString, 
            name: "MOVIES.DB", 
            tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database })
        .AddCheck<MoviesHealthCheck>("MOVIES.API", 
            tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api });
}
```

The configuration is retrieved from the application's configuration system using the assembly name as the section key, ensuring environment-specific settings can be applied through standard ASP.NET Core configuration providers.

### Health Check Tags

The system implements a tag-based categorization system defined in `HealthCheckTag.cs`:

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

Tags enable:
- **Filtered health checks**: Query specific subsystems independently
- **Logical grouping**: Organize checks by functional area or infrastructure layer
- **Flexible monitoring**: Different monitoring systems can target different tag combinations

### Endpoint Configuration

The health check endpoints are configured in the `Configure` method of `HealthCheckStartup`:

```csharp
public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, HostConfig hostConfig)
{
    var endpoint = hostConfig.HealthChecks.Endpoint;
    
    // Aggregate endpoint - all health checks
    app.UseHealthChecks(endpoint, new HealthCheckOptions()
    {
        Predicate = _ => true,
        ResponseWriter = HealthCheckResponseWriter,
    });

    // Tag-specific endpoints - filtered by tag
    foreach (var tag in HealthCheckTag.All)
    {
        app.UseHealthChecks($"{endpoint}/{tag}", new HealthCheckOptions()
        {
            Predicate = registration => registration.Tags.Contains(tag),
            ResponseWriter = HealthCheckResponseWriter,
        });
    }
}
```

This configuration creates multiple endpoints:
- **Base endpoint** (e.g., `/health`): Returns status of all registered health checks
- **Tag-filtered endpoints** (e.g., `/health/database`, `/health/api`): Returns status of checks matching the specified tag

The endpoint path is configurable via `HealthChecksConfig`:

```csharp
public class HealthChecksConfig
{
    public string Endpoint { get; set; }
}
```

## Built-in Health Checks

The application leverages ASP.NET Core's built-in health check providers for common infrastructure components.

### SQL Server Connectivity

The SQL Server health check verifies database connectivity and availability using the `AspNetCore.HealthChecks.SqlServer` package (version 5.0.3):

```csharp
services.AddHealthChecks()
    .AddSqlServer(config.MoviesConnectionString, 
        name: "MOVIES.DB", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database })
```

**Key Features:**
- **Connection validation**: Executes a simple query to verify the database is reachable and responsive
- **Timeout handling**: Respects connection timeout settings from the connection string
- **Named check**: Identified as "MOVIES.DB" in health check responses
- **Multi-tag support**: Tagged with both "movies" and "database" for flexible filtering

**Health Status Mapping:**
- `Healthy`: Database connection successful and query executed
- `Unhealthy`: Connection failed, timeout occurred, or query execution failed

### Entity Framework Core Context

While not explicitly shown in the provided source files, the application references `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (version 6.0.1), which provides health checks for Entity Framework Core database contexts.

**Typical Usage:**
```csharp
services.AddHealthChecks()
    .AddDbContextCheck<MoviesDbContext>();
```

This health check verifies:
- Database context can be created
- Database is accessible
- Pending migrations (optional check)

### Application Readiness

The health check system supports both **liveness** and **readiness** probes:

- **Liveness**: Indicates whether the application is running (all endpoints respond)
- **Readiness**: Indicates whether the application is ready to accept traffic (all health checks pass)

The aggregate endpoint (`/health`) serves as a readiness probe, while tag-specific endpoints can be used for more granular liveness checks.

## Custom Health Checks

The application implements custom health checks for external dependencies and services.

### Movie Service Health Check

The `MoviesHealthCheck` class demonstrates a custom health check implementation for an external HTTP API:

```csharp
public class MoviesHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MoviesHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClientFactory.CreateClient("movies")
                .GetAsync("api/version", cancellationToken).ConfigureAwait(false);
            
            return result.IsSuccessStatusCode
                ? new HealthCheckResult(HealthStatus.Healthy)
                : new HealthCheckResult(HealthStatus.Unhealthy);
        }
        catch (Exception e)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, exception: e);
        }
    }
}
```

**Implementation Details:**

1. **Dependency Injection**: Uses `IHttpClientFactory` for proper HttpClient lifecycle management
2. **Named Client**: References the "movies" named client configured in `Startup.cs`
3. **Cancellation Support**: Respects cancellation tokens for graceful shutdown
4. **ConfigureAwait**: Uses `ConfigureAwait(false)` to avoid deadlocks in synchronous contexts
5. **Exception Handling**: Catches all exceptions and returns unhealthy status with exception details

**HTTP Client Configuration:**

The named "movies" client is configured in `Startup.cs`:

```csharp
services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
services.AddHttpClient("movies", (provider, client) => 
    provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
```

This configuration:
- Uses the `IHttpClientDecorator` pattern for centralized client configuration
- Applies consistent settings (base address, timeouts, headers) across all movie service calls
- Leverages Polly resilience policies (retry, circuit breaker) configured via `Microsoft.Extensions.Http.Polly`

### Creating Custom Implementations

To create a custom health check:

1. **Implement IHealthCheck interface:**

```csharp
public class CustomHealthCheck : IHealthCheck
{
    private readonly IDependency _dependency;

    public CustomHealthCheck(IDependency dependency)
    {
        _dependency = dependency;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform health check logic
            var isHealthy = await _dependency.CheckStatusAsync(cancellationToken);
            
            return isHealthy
                ? HealthCheckResult.Healthy("Service is operational")
                : HealthCheckResult.Degraded("Service is degraded");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Service is unavailable", ex);
        }
    }
}
```

2. **Register the health check:**

```csharp
services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("CUSTOM.CHECK", 
        tags: new[] { "custom", "external" });
```

**Best Practices:**

- **Timeout Management**: Implement timeouts to prevent health checks from blocking indefinitely
- **Lightweight Checks**: Keep health checks fast (< 5 seconds) to avoid impacting monitoring systems
- **Meaningful Status**: Use `Healthy`, `Degraded`, and `Unhealthy` appropriately
- **Exception Details**: Include exception information for debugging, but avoid exposing sensitive data
- **Cancellation Support**: Always respect cancellation tokens
- **Dependency Injection**: Use constructor injection for testability

### Health Check Responses

The custom response writer formats health check results as JSON:

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

**Response Fields:**

- **status**: Aggregate status (`Healthy`, `Degraded`, `Unhealthy`)
- **details**: Array of individual health check results
  - **key**: Health check name
  - **value**: Individual check status
  - **description**: Optional description from the health check
  - **duration**: Time taken to execute the check
  - **exception**: Exception details if the check failed (includes stack trace)

**Status Aggregation Rules:**

- `Healthy`: All checks return `Healthy`
- `Degraded`: At least one check returns `Degraded`, none return `Unhealthy`
- `Unhealthy`: At least one check returns `Unhealthy`

**HTTP Status Codes:**

- `200 OK`: Status is `Healthy`
- `200 OK`: Status is `Degraded` (configurable)
- `503 Service Unavailable`: Status is `Unhealthy`

## Integration

### Kubernetes Probes

The health check endpoints are designed for Kubernetes liveness and readiness probes.

**Liveness Probe Configuration:**

```yaml
livenessProbe:
  httpGet:
    path: /health/api
    port: 80
    scheme: HTTP
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

**Readiness Probe Configuration:**

```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 80
    scheme: HTTP
  initialDelaySeconds: 10
  periodSeconds: 5
  timeoutSeconds: 3
  failureThreshold: 3
```

**Probe Strategy:**

- **Liveness**: Use tag-specific endpoints (e.g., `/health/api`) to check only critical components
- **Readiness**: Use the aggregate endpoint (`/health`) to ensure all dependencies are available
- **Startup**: Consider a separate startup probe for slow-starting applications

**Considerations:**

- Set `initialDelaySeconds` to allow for application startup and dependency initialization
- Configure `timeoutSeconds` shorter than health check execution time
- Use `failureThreshold` to avoid flapping during transient failures
- Monitor probe failures in Kubernetes events and logs

For detailed Kubernetes deployment configuration, see [Kubernetes Deployment Guide](/deployment/kubernetes.md).

### Load Balancer Health Checks

Cloud load balancers (Azure Load Balancer, AWS ELB, etc.) can use health check endpoints for traffic routing decisions.

**Azure Application Gateway Configuration:**

```json
{
  "probe": {
    "protocol": "Http",
    "path": "/health",
    "interval": 30,
    "timeout": 30,
    "unhealthyThreshold": 3
  }
}
```

**Best Practices:**

- Use the aggregate endpoint for load balancer health checks
- Configure appropriate intervals to balance responsiveness and load
- Set unhealthy thresholds to tolerate transient failures
- Monitor health check failures in load balancer metrics

### Monitoring Systems

Health check endpoints integrate with monitoring and alerting systems.

**Prometheus Integration:**

While not included in the current implementation, health checks can be exposed as Prometheus metrics using libraries like `AspNetCore.HealthChecks.Publisher.Prometheus`.

**Application Insights:**

Health check results can be logged to Application Insights for historical analysis:

```csharp
services.AddHealthChecks()
    .AddApplicationInsightsPublisher();
```

**Custom Monitoring:**

The JSON response format enables integration with custom monitoring solutions:

```csharp
// Example monitoring client
public async Task<HealthStatus> CheckApplicationHealthAsync()
{
    var response = await _httpClient.GetAsync("/health");
    var content = await response.Content.ReadAsStringAsync();
    var healthReport = JsonSerializer.Deserialize<HealthReport>(content);
    
    // Process health report
    return healthReport.Status;
}
```

**Alerting Strategies:**

- **Critical Alerts**: Trigger on `Unhealthy` status for critical components (database, authentication)
- **Warning Alerts**: Trigger on `Degraded` status or repeated failures
- **Informational**: Log all health check executions for trend analysis

For comprehensive monitoring setup, see [Monitoring and Observability Guide](/deployment/monitoring.md).

### API Reference

For detailed API endpoint specifications, request/response schemas, and authentication requirements, see [Health Check API Reference](/api_reference/health_api.md).

## Troubleshooting

### Common Issues

**Health Check Timeouts:**

If health checks consistently timeout:
- Verify database connection strings and network connectivity
- Check for blocking operations in custom health checks
- Review SQL Server query performance
- Ensure external APIs are responsive

**Flapping Health Status:**

If health status oscillates between healthy and unhealthy:
- Increase failure thresholds in Kubernetes probes
- Implement retry logic in custom health checks (using Polly)
- Add circuit breakers to prevent cascading failures
- Review resource constraints (CPU, memory, network)

**Exception in Health Check Response:**

If exceptions appear in health check responses:
- Review exception details and stack traces
- Check application logs for additional context
- Verify dependency configuration (connection strings, API endpoints)
- Ensure proper error handling in custom health checks

**Missing Health Checks:**

If expected health checks don't appear in responses:
- Verify health check registration in `HealthCheckStartup.ConfigureServices`
- Check tag filters in endpoint configuration
- Ensure custom health checks implement `IHealthCheck` correctly
- Review dependency injection registration

### Debugging

Enable detailed health check logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.Diagnostics.HealthChecks": "Debug"
    }
  }
}
```

This provides detailed information about health check execution, timing, and results in application logs.