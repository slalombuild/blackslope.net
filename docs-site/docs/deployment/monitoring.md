# Monitoring and Observability

This document provides comprehensive guidance on monitoring and observability capabilities within the BlackSlope application. The system implements multiple layers of observability including structured logging, distributed tracing, health monitoring, and Application Insights integration.

## Application Insights

Application Insights provides cloud-based application performance management (APM) and monitoring capabilities for the BlackSlope API.

### ApplicationInsights Configuration

The Application Insights integration is configured through the `appsettings.json` file and a dedicated configuration class:

```json
{
  "BlackSlope.Api": {
    "ApplicationInsights": {
      "InstrumentationKey": "[instrumentation-key]"
    }
  }
}
```

The configuration is mapped to a strongly-typed class for type-safe access:

```csharp
namespace BlackSlope.Api.Common.Configuration
{
    public class ApplicationInsightsConfig
    {
        public string InstrumentationKey { get; set; }
    }
}
```

**Configuration Steps:**

1. **Obtain Instrumentation Key**: Create an Application Insights resource in Azure Portal and copy the instrumentation key
2. **Update Configuration**: Replace `[instrumentation-key]` with your actual key in `appsettings.json`
3. **Environment-Specific Keys**: Use different instrumentation keys for development, staging, and production environments
4. **User Secrets**: Store sensitive instrumentation keys in User Secrets for local development:
   ```bash
   dotnet user-secrets set "BlackSlope.Api:ApplicationInsights:InstrumentationKey" "your-key-here"
   ```

### Telemetry Collection

The application leverages the **Microsoft.AspNetCore.App** framework's built-in Application Insights integration. When properly configured, the following telemetry is automatically collected:

- **Request Telemetry**: HTTP requests, response times, status codes
- **Dependency Telemetry**: Database calls (via Entity Framework Core), HTTP client calls (via HttpClient)
- **Exception Telemetry**: Unhandled exceptions and their stack traces
- **Performance Counters**: CPU, memory, request rate metrics
- **Custom Events**: Application-specific events logged through ILogger

**Dependency Tracking:**

The application uses several libraries that automatically integrate with Application Insights:

- **Entity Framework Core 6.0.1**: Database queries and execution times are tracked
- **HttpClient with Polly**: Outbound HTTP calls, including retry attempts and circuit breaker states
- **SQL Server**: Direct SQL queries via `Microsoft.Data.SqlClient` (5.1.3)

### Custom Metrics

Custom metrics can be added through the standard `ILogger` interface, which integrates with Application Insights when configured:

```csharp
// Example: Logging custom metrics
public class MovieService
{
    private readonly ILogger<MovieService> _logger;

    public MovieService(ILogger<MovieService> logger)
    {
        _logger = logger;
    }

    public async Task<Movie> GetMovieAsync(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var movie = await _repository.GetByIdAsync(id);
            
            // Custom metric: Movie retrieval time
            _logger.LogInformation(
                "Movie {MovieId} retrieved in {ElapsedMs}ms", 
                id, 
                stopwatch.ElapsedMilliseconds);
            
            return movie;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve movie {MovieId}", id);
            throw;
        }
    }
}
```

**Best Practices for Custom Metrics:**

- Use structured logging with named parameters (e.g., `{MovieId}`) for better querying in Application Insights
- Include relevant context (user ID, correlation ID, operation name)
- Avoid logging sensitive data (passwords, tokens, PII)
- Use appropriate log levels (Information for metrics, Warning for anomalies, Error for failures)

## Logging

The BlackSlope application implements structured logging using Serilog, configured to support multiple output sinks including console, file, and Application Insights.

### Structured Logging with Serilog

Serilog configuration is managed through a dedicated configuration class and settings in `appsettings.json`:

```csharp
namespace BlackSlope.Api.Common.Configuration
{
    public class SerilogConfig
    {
        public string MinimumLevel { get; set; }
        public string FileName { get; set; }
        public bool WriteToConsole { get; set; }
        public bool WriteToFile { get; set; }
        public bool WriteToAppInsights { get; set; }
    }
}
```

**Configuration Example:**

```json
{
  "BlackSlope.Api": {
    "Serilog": {
      "MinimumLevel": "information",
      "FileName": "log.txt",
      "WriteToFile": "false",
      "WriteToAppInsights": "false",
      "WriteToConsole": "true"
    }
  }
}
```

**Log Levels:**

The `MinimumLevel` setting accepts either numeric values or string names:

| Level | Value | Description |
|-------|-------|-------------|
| Verbose | 0 | Detailed diagnostic information |
| Debug | 1 | Internal system events for debugging |
| Information | 2 | General informational messages |
| Warning | 3 | Warnings about potential issues |
| Error | 4 | Error events that might still allow the application to continue |
| Fatal | 5 | Critical errors causing application termination |

**Sink Configuration:**

The application supports three logging sinks, each controlled by a boolean flag:

1. **Console Sink** (`WriteToConsole`): Outputs logs to standard output, ideal for containerized environments and local development
2. **File Sink** (`WriteToFile`): Writes logs to a file specified by `FileName`, useful for persistent logging on VMs
3. **Application Insights Sink** (`WriteToAppInsights`): Sends logs directly to Application Insights for centralized cloud logging

**Recommended Configurations by Environment:**

| Environment | Console | File | App Insights |
|-------------|---------|------|--------------|
| Development | true | false | false |
| Docker/Kubernetes | true | false | true |
| VM/IIS | false | true | true |
| Production | true | false | true |

### Log Aggregation

When `WriteToAppInsights` is enabled, logs are automatically aggregated in Azure Application Insights, providing:

- **Centralized Log Storage**: All application instances send logs to a single location
- **Correlation**: Logs are automatically correlated with requests, dependencies, and exceptions
- **Retention**: Configurable retention policies (default 90 days)
- **Query Capabilities**: Powerful KQL (Kusto Query Language) for log analysis

**Example KQL Query:**

```kusto
traces
| where timestamp > ago(1h)
| where severityLevel >= 3  // Warning and above
| where customDimensions.CorrelationId == "specific-correlation-id"
| project timestamp, message, severityLevel, customDimensions
| order by timestamp desc
```

### Log Analysis

For detailed log analysis guidance, refer to the [Logging Configuration](/configuration/logging.md) documentation page.

**Key Analysis Scenarios:**

1. **Error Rate Monitoring**: Track error frequency over time to identify degradation
2. **Performance Analysis**: Analyze log timestamps to identify slow operations
3. **User Journey Tracking**: Follow correlation IDs through distributed operations
4. **Anomaly Detection**: Identify unusual patterns in log volume or error types

## Distributed Tracing

The BlackSlope application implements distributed tracing through correlation IDs, enabling request tracking across service boundaries and asynchronous operations.

### Correlation ID Propagation

The correlation ID system is implemented through middleware that intercepts every HTTP request:

```csharp
using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BlackSlope.Api.Common.Middleware.Correlation
{
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
            
            // Read correlation ID from request header or generate new one
            var correlationId = _correlationIdRequestReader.Read(context) 
                ?? GenerateCorrelationId();
            
            // Store correlation ID in scoped service for access throughout request
            currentCorrelationIdService.SetId(correlationId);

            Contract.Requires(context != null);
            
            // Write correlation ID to response header before sending response
            context.Response.OnStarting(() =>
            {
                _correlationIdResponseWriter.Write(context, correlationId);
                return Task.CompletedTask;
            });

            await _next(context);
        }

        private static Guid GenerateCorrelationId() => Guid.NewGuid();
    }
}
```

**How It Works:**

1. **Request Interception**: Middleware intercepts every incoming HTTP request
2. **ID Extraction/Generation**: Attempts to read correlation ID from request header; generates new GUID if not present
3. **Scoped Storage**: Stores correlation ID in `ICurrentCorrelationIdService` (scoped lifetime) for access throughout the request pipeline
4. **Response Header**: Writes correlation ID to response header using `OnStarting` callback
5. **Propagation**: Correlation ID is available to all services, repositories, and logging calls within the request scope

**Integration with Logging:**

```csharp
// Example: Including correlation ID in log entries
public class MovieController : ControllerBase
{
    private readonly ILogger<MovieController> _logger;
    private readonly ICurrentCorrelationIdService _correlationIdService;

    public MovieController(
        ILogger<MovieController> logger,
        ICurrentCorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMovie(int id)
    {
        var correlationId = _correlationIdService.GetId();
        
        _logger.LogInformation(
            "Retrieving movie {MovieId} with correlation ID {CorrelationId}", 
            id, 
            correlationId);
        
        // ... rest of implementation
    }
}
```

For detailed implementation guidance, see the [Correlation ID Feature](/features/correlation_id.md) documentation.

### Request Tracing

Request tracing enables end-to-end visibility of requests as they flow through the system:

**Trace Flow:**

1. **Entry Point**: Client sends request with optional `X-Correlation-ID` header
2. **Middleware Processing**: `CorrelationIdMiddleware` extracts or generates correlation ID
3. **Service Layer**: All service calls include correlation ID in log context
4. **Data Layer**: Entity Framework Core queries are logged with correlation ID
5. **External Calls**: HttpClient requests propagate correlation ID to downstream services
6. **Response**: Correlation ID is returned in response header for client reference

**HttpClient Integration:**

When making outbound HTTP calls, propagate the correlation ID:

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

    public async Task<ApiResponse> CallExternalApiAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/endpoint");
        
        // Propagate correlation ID to downstream service
        request.Headers.Add("X-Correlation-ID", _correlationIdService.GetId().ToString());
        
        var response = await _httpClient.SendAsync(request);
        return await response.Content.ReadAsAsync<ApiResponse>();
    }
}
```

### Performance Monitoring

Performance monitoring combines correlation IDs with timing information to identify bottlenecks:

**Key Performance Indicators:**

- **Request Duration**: Total time from request receipt to response sent
- **Database Query Time**: Time spent executing SQL queries (tracked by EF Core)
- **External API Latency**: Time spent waiting for downstream services
- **Cache Hit Rate**: Effectiveness of `Microsoft.Extensions.Caching.Memory` (6.0.2)

**Polly Integration for Resilience Metrics:**

The application uses **Polly** (7.2.2) with **Polly.Extensions.Http** (3.0.0) for resilience patterns. These automatically log retry attempts and circuit breaker state changes:

```csharp
// Example: Configuring Polly with logging
services.AddHttpClient<IExternalApiClient, ExternalApiClient>()
    .AddPolicyHandler((services, request) =>
    {
        var logger = services.GetRequiredService<ILogger<ExternalApiClient>>();
        
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount} after {Delay}ms due to {Exception}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    });
```

## Health Monitoring

The BlackSlope application implements comprehensive health checks using ASP.NET Core's health check framework, with custom checks for database connectivity and API dependencies.

### Health Check Endpoints

Health checks are configured in `HealthCheckStartup.cs` and exposed through multiple endpoints:

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BlackSlope.Api.Common.Configuration;
using BlackSlope.Api.HealthChecks;
using BlackSlope.Repositories.Movies.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BlackSlope.Api
{
    [ExcludeFromCodeCoverage]
    public class HealthCheckStartup
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
                .Get<MovieRepositoryConfiguration>();

            services.AddHealthChecks()
                // SQL Server database health check
                .AddSqlServer(
                    config.MoviesConnectionString, 
                    name: "MOVIES.DB", 
                    tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database })
                // Custom API health check
                .AddCheck<MoviesHealthCheck>(
                    "MOVIES.API", 
                    tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api });
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, HostConfig hostConfig)
        {
            if (env is null)
            {
                throw new System.ArgumentNullException(nameof(env));
            }

            var endpoint = hostConfig.HealthChecks.Endpoint;
            
            // Main health check endpoint - checks all registered health checks
            app.UseHealthChecks(endpoint, new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = HealthCheckResponseWriter,
            });

            // Tag-specific endpoints - filter health checks by tag
            foreach (var tag in HealthCheckTag.All)
            {
                app.UseHealthChecks($"{endpoint}/{tag}", new HealthCheckOptions()
                {
                    Predicate = registration => registration.Tags.Contains(tag),
                    ResponseWriter = HealthCheckResponseWriter,
                });
            }
        }

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
    }
}
```

**Available Endpoints:**

The health check endpoint is configured in `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "HealthChecks": {
      "Endpoint": "/health"
    }
  }
}
```

This creates the following endpoints:

| Endpoint | Description | Checks Performed |
|----------|-------------|------------------|
| `/health` | Overall system health | All registered health checks |
| `/health/movies` | Movies subsystem health | MOVIES.DB, MOVIES.API |
| `/health/database` | Database connectivity | MOVIES.DB |
| `/health/api` | API-level checks | MOVIES.API |

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
      "description": "Movies API is operational",
      "duration": "00:00:00.0012345",
      "exception": null
    }
  ]
}
```

**Health Check Status Values:**

- **Healthy**: All checks passed
- **Degraded**: Some non-critical checks failed
- **Unhealthy**: Critical checks failed

### Registered Health Checks

The application registers two types of health checks:

**1. SQL Server Database Health Check**

Uses **AspNetCore.HealthChecks.SqlServer** (5.0.3) to verify database connectivity:

```csharp
services.AddHealthChecks()
    .AddSqlServer(
        config.MoviesConnectionString, 
        name: "MOVIES.DB", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database });
```

This check:
- Attempts to open a connection to the SQL Server database
- Executes a simple query (`SELECT 1`)
- Returns `Healthy` if successful, `Unhealthy` if connection fails
- Includes connection timeout and query execution time in duration

**2. Custom Movies API Health Check**

A custom health check implementation for API-specific validation:

```csharp
services.AddHealthChecks()
    .AddCheck<MoviesHealthCheck>(
        "MOVIES.API", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api });
```

Custom health checks should implement `IHealthCheck`:

```csharp
public class MoviesHealthCheck : IHealthCheck
{
    private readonly IMovieRepository _movieRepository;

    public MoviesHealthCheck(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform a lightweight operation to verify API functionality
            var canConnect = await _movieRepository.CanConnectAsync(cancellationToken);
            
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Movies API is operational");
            }
            
            return HealthCheckResult.Degraded("Movies API is experiencing issues");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Movies API is unavailable", ex);
        }
    }
}
```

**Entity Framework Core Health Check**

The application also includes **Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore** (6.0.1), which can be used to check DbContext health:

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<MoviesDbContext>(
        name: "MOVIES.DBCONTEXT",
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database });
```

For comprehensive health check implementation details, see the [Health Checks Feature](/features/health_checks.md) documentation.

### Uptime Monitoring

Health check endpoints enable integration with external monitoring services:

**Kubernetes Liveness and Readiness Probes:**

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

For Kubernetes deployment configuration, refer to the [Kubernetes Deployment](/deployment/kubernetes.md) documentation.

**Azure Application Insights Availability Tests:**

Configure availability tests in Azure Portal to periodically ping health endpoints:

1. Navigate to Application Insights resource
2. Select "Availability" under "Investigate"
3. Click "+ Add Standard test"
4. Configure:
   - **URL**: `https://your-api.azurewebsites.net/health`
   - **Test frequency**: 5 minutes
   - **Test locations**: Multiple geographic locations
   - **Success criteria**: HTTP 200 status code
   - **Alerts**: Enable alerts for failures

**Third-Party Monitoring Services:**

Popular services that integrate with health check endpoints:

- **Pingdom**: HTTP monitoring with global test locations
- **UptimeRobot**: Free tier available, 5-minute intervals
- **StatusCake**: Comprehensive uptime and performance monitoring
- **Datadog**: Full-stack monitoring with APM integration

### Alerting

Configure alerts based on health check failures and performance degradation:

**Application Insights Alerts:**

1. **Availability Alert** (Health Check Failures):
   ```
   Condition: Availability test fails from 2+ locations
   Threshold: 2 failures within 5 minutes
   Action: Send email to ops team, create incident in PagerDuty
   ```

2. **Performance Alert** (Slow Response Times):
   ```
   Condition: Server response time > 2 seconds
   Threshold: 5 occurrences within 5 minutes
   Action: Send email to dev team
   ```

3. **Error Rate Alert** (Exception Spike):
   ```
   Condition: Failed request rate > 5%
   Threshold: Sustained for 10 minutes
   Action: Send SMS to on-call engineer, create incident
   ```

**Azure Monitor Action Groups:**

Create action groups to define alert recipients and actions:

```json
{
  "name": "BlackSlope-Critical-Alerts",
  "emailReceivers": [
    {
      "name": "OpsTeam",
      "emailAddress": "ops@company.com"
    }
  ],
  "smsReceivers": [
    {
      "name": "OnCall",
      "countryCode": "1",
      "phoneNumber": "5551234567"
    }
  ],
  "webhookReceivers": [
    {
      "name": "PagerDuty",
      "serviceUri": "https://events.pagerduty.com/integration/..."
    }
  ]
}
```

**Log-Based Alerts:**

Create custom alerts based on log queries:

```kusto
// Alert on high error rate
requests
| where timestamp > ago(5m)
| summarize 
    total = count(),
    failed = countif(success == false)
| extend errorRate = (failed * 100.0) / total
| where errorRate > 5
```

## Best Practices

### Meaningful Metrics

**Choose Metrics That Drive Action:**

- **Business Metrics**: Orders per minute, user registrations, revenue
- **Technical Metrics**: Request latency (p50, p95, p99), error rate, throughput
- **Infrastructure Metrics**: CPU usage, memory consumption, disk I/O

**Avoid Vanity Metrics:**

- Total number of requests (without context)
- Raw error counts (use error rate instead)
- Average response time (use percentiles instead)

**Implement the Four Golden Signals:**

1. **Latency**: Time to service requests (distinguish successful vs. failed requests)
2. **Traffic**: Demand on the system (requests per second)
3. **Errors**: Rate of failed requests
4. **Saturation**: Resource utilization (CPU, memory, database connections)

**Example Implementation:**

```csharp
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            await _next(context);
            
            // Log successful request with latency
            _logger.LogInformation(
                "Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            // Log failed request with error details
            _logger.LogError(
                ex,
                "Request {Method} {Path} failed after {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
```

### Alert Fatigue Prevention

**Strategies to Reduce False Positives:**

1. **Set Appropriate Thresholds**: Base thresholds on historical data and business requirements
2. **Use Time Windows**: Require sustained conditions (e.g., 5 minutes) before alerting
3. **Implement Alert Suppression**: Prevent duplicate alerts for the same issue
4. **Create Alert Hierarchies**: Start with warnings, escalate to critical only if unresolved

**Alert Severity Levels:**

| Severity | Response Time | Examples |
|----------|---------------|----------|
| Critical | Immediate (24/7) | Service down, data loss, security breach |
| High | Within 1 hour | Degraded performance, elevated error rate |
| Medium | Within 4 hours | Non-critical feature failure, resource warnings |
| Low | Next business day | Informational, optimization opportunities |

**Alert Tuning Process:**

1. **Monitor Alert Volume**: Track alerts per day/week
2. **Analyze False Positives**: Identify alerts that don't require action
3. **Adjust Thresholds**: Increase thresholds for noisy alerts
4. **Add Context**: Include runbook links and troubleshooting steps
5. **Regular Review**: Quarterly review of all alerts

**Example Alert Configuration:**

```csharp
// Configure health check with appropriate failure threshold
services.AddHealthChecks()
    .AddSqlServer(
        connectionString,
        name: "MOVIES.DB",
        failureStatus: HealthStatus.Degraded, // Don't immediately fail
        timeout: TimeSpan.FromSeconds(5))     // Reasonable timeout
    .AddCheck<MoviesHealthCheck>(
        "MOVIES.API",
        failureStatus: HealthStatus.Unhealthy,
        timeout: TimeSpan.FromSeconds(10));
```

### Dashboard Design

**Principles for Effective Dashboards:**

1. **Audience-Specific**: Create different dashboards for developers, operations, and business stakeholders
2. **Actionable Information**: Every metric should answer "What should I do?"
3. **Visual Hierarchy**: Most important metrics at the top, supporting details below
4. **Consistent Time Ranges**: Use standard time windows (last hour, last 24 hours, last 7 days)
5. **Contextual Thresholds**: Show acceptable ranges and alert thresholds

**Recommended Dashboard Structure:**

**1. Executive Dashboard (Business Stakeholders):**
- System availability (uptime percentage)
- Active users
- Request volume trends
- Error rate trends

**2. Operations Dashboard (DevOps Team):**
- Health check status (all endpoints)
- Resource utilization (CPU, memory, database connections)
- Error rate by endpoint
- Response time percentiles (p50, p95, p99)
- Recent deployments and incidents

**3. Developer Dashboard (Engineering Team):**
- Request latency by endpoint
- Database query performance
- External API latency
- Exception details and stack traces
- Correlation ID search

**Application Insights Dashboard Example:**

```kusto
// Dashboard query: Request latency percentiles
requests
| where timestamp > ago(1h)
| summarize 
    p50 = percentile(duration, 50),
    p95 = percentile(duration, 95),
    p99 = percentile(duration, 99)
    by bin(timestamp, 5m)
| render timechart
```

**Grafana Dashboard for Health Checks:**

```json
{
  "dashboard": {
    "title": "BlackSlope API Health",
    "panels": [
      {
        "title": "Health Check Status",
        "type": "stat",
        "targets": [
          {
            "expr": "up{job='blackslope-api'}",
            "legendFormat": "{{instance}}"
          }
        ]
      },
      {
        "title": "Database Connection Pool",
        "type": "graph",
        "targets": [
          {
            "expr": "sqlserver_connection_pool_size",
            "legendFormat": "Pool Size"
          }
        ]
      }
    ]
  }
}
```

**Key Performance Indicators (KPIs) to Display:**

- **Availability**: 99.9% uptime target (43 minutes downtime per month)
- **Latency**: p95 < 500ms, p99 < 1000ms
- **Error Rate**: < 0.1% of requests
- **Throughput**: Requests per second with capacity headroom
- **Database Performance**: Query execution time, connection pool utilization

**Dashboard Maintenance:**

- Review and update dashboards quarterly
- Remove unused or redundant metrics
- Add new metrics as features are deployed
- Gather feedback from dashboard users
- Document dashboard purpose and metric definitions

---

## Related Documentation

- [Logging Configuration](/configuration/logging.md) - Detailed Serilog configuration and log management
- [Correlation ID Feature](/features/correlation_id.md) - Implementation details for distributed tracing
- [Health Checks Feature](/features/health_checks.md) - Custom health check development guide
- [Kubernetes Deployment](/deployment/kubernetes.md) - Container orchestration and health probe configuration