# Logging Configuration

The BlackSlope API uses the standard **Microsoft.Extensions.Logging** framework built into ASP.NET Core 6.0. The application may optionally be configured with **Serilog** for enhanced structured logging capabilities with support for multiple output sinks including console, file, and Azure Application Insights.

> **Note:** The logging infrastructure described in this document reflects an extended configuration with Serilog. The base application uses standard .NET logging via `Microsoft.Extensions.Logging.Debug`.

## Serilog Integration (Optional)

### Overview

When configured, Serilog is integrated at the application host level through the `Program.cs` entry point, ensuring that logging is available throughout the entire application lifecycle, including startup and shutdown events.

### Host Builder Configuration

The application configures Serilog using the `UseSerilog` extension method in the host builder:

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseSerilog(Assembly.GetExecutingAssembly().GetName().Name);
        webBuilder.UseStartup<Startup>();
    });
```

**Key Points:**
- Serilog is configured **before** the `Startup` class is invoked, ensuring all startup logs are captured
- The assembly name (`BlackSlope.Api`) is passed to identify the configuration section in `appsettings.json`
- This approach replaces the default .NET logging provider with Serilog

### Structured Logging Benefits

Serilog provides structured logging capabilities that offer significant advantages over traditional text-based logging:

- **Queryable Logs**: Log events are treated as structured data, not just text strings
- **Rich Context**: Properties can be attached to log events for filtering and analysis
- **Multiple Sinks**: Simultaneous output to console, files, and cloud services
- **Performance**: Efficient serialization and minimal overhead
- **Correlation**: Integration with correlation ID middleware for request tracking

## Configuration

### SerilogConfig Class

The `SerilogConfig` class defines the configuration model for Serilog settings:

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

**Configuration Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `MinimumLevel` | `string` | Minimum log level to capture (verbose, debug, information, warning, error, fatal) |
| `FileName` | `string` | File path for file-based logging output |
| `WriteToConsole` | `bool` | Enable/disable console output sink |
| `WriteToFile` | `bool` | Enable/disable file output sink |
| `WriteToAppInsights` | `bool` | Enable/disable Azure Application Insights sink |

### appsettings.json Configuration

The Serilog configuration is defined in the `appsettings.json` file under the application-specific section:

```json
{
  "BlackSlope.Api": {
    "Serilog": {
      "MinimumLevel": "information",
      "FileName": "log.txt",
      "WriteToFile": "false",
      "WriteToAppInsights": "false",
      "WriteToConsole": "true"
    },
    "ApplicationInsights": {
      "InstrumentationKey": "[instrumentation-key]"
    }
  }
}
```

**Configuration Notes:**
- The `MinimumLevel` accepts both numeric values (0-5) and string names (verbose, debug, information, warning, error, fatal)
- Boolean values are stored as strings in JSON and parsed appropriately
- Application Insights requires a valid instrumentation key when `WriteToAppInsights` is enabled

For more information on application settings structure, see [Application Settings](/configuration/application_settings.md).

### UseSerilog Extension Method

The `UseSerilog` extension method in `BlackSlopeHostBuilderExtensions.cs` orchestrates the Serilog configuration:

```csharp
public static IWebHostBuilder UseSerilog(this IWebHostBuilder webHostBuilder, string appSettingsSection) =>
    webHostBuilder.UseSerilog((ctx, config) =>
    {
        var appSettings = ctx.Configuration.GetSection(appSettingsSection).Get<HostConfig>();
        var serilogConfig = appSettings.Serilog;

        // If they follow conventions for Serilog in config it will be read here.
        config.ReadFrom.Configuration(ctx.Configuration);

        SetLogLevel(config, serilogConfig);
        LogToFile(config, serilogConfig);
        LogToConsole(config, serilogConfig);
        LogToApplicationInsights(config, appSettings, serilogConfig);
    });
```

**Configuration Flow:**
1. Retrieves the `HostConfig` from the specified configuration section
2. Reads Serilog's conventional configuration from `appsettings.json`
3. Applies custom log level configuration
4. Conditionally enables file, console, and Application Insights sinks

### Log Level Configuration

The minimum log level is configured dynamically using a `LoggingLevelSwitch`:

```csharp
private static void SetLogLevel(LoggerConfiguration config, SerilogConfig serilogConfig)
{
    Enum.TryParse<LogEventLevel>(serilogConfig.MinimumLevel, true, out var minimumLevel);
    var levelSwitch = new LoggingLevelSwitch
    {
        MinimumLevel = minimumLevel,
    };

    config.MinimumLevel.ControlledBy(levelSwitch);
}
```

**Log Levels (in order of severity):**

| Level | Numeric Value | Use Case |
|-------|---------------|----------|
| Verbose | 0 | Extremely detailed diagnostic information |
| Debug | 1 | Internal system events for debugging |
| Information | 2 | General informational messages (default) |
| Warning | 3 | Indicators of potential issues |
| Error | 4 | Error events that might still allow the application to continue |
| Fatal | 5 | Critical errors causing application termination |

**Best Practice:** Use `Information` level for production environments and `Debug` or `Verbose` for development/troubleshooting scenarios.

## Log Sinks

### Console Sink

The console sink outputs log events to the standard output stream, ideal for containerized environments and local development:

```csharp
private static void LogToConsole(LoggerConfiguration config, SerilogConfig serilogConfig)
{
    if (serilogConfig.WriteToConsole)
    {
        config.WriteTo.Console();
    }
}
```

**Use Cases:**
- Docker container logs (captured by container orchestration platforms)
- Local development debugging
- CI/CD pipeline output
- Kubernetes pod logs

**Default Configuration:** Enabled (`WriteToConsole: true`)

### File Sink

The file sink writes log events to a rolling file on the local file system:

```csharp
private static void LogToFile(LoggerConfiguration config, SerilogConfig serilogConfig)
{
    if (serilogConfig.WriteToFile)
    {
        // TODO: Rolling Interval day should be configurable (defaulted to day)
        config.WriteTo.File(serilogConfig.FileName, rollingInterval: RollingInterval.Day);
    }
}
```

**File Rolling Behavior:**
- **Rolling Interval:** Daily (creates a new file each day)
- **File Naming:** `log.txt`, `log20240101.txt`, `log20240102.txt`, etc.
- **Retention:** Files are not automatically deleted (implement external cleanup if needed)

**Considerations:**
- File-based logging is **disabled by default** (`WriteToFile: false`)
- Ensure the application has write permissions to the specified directory
- Monitor disk space usage in production environments
- Consider using centralized logging instead of local files for production deployments

**TODO:** The rolling interval is currently hardcoded to `RollingInterval.Day`. Consider making this configurable for different retention policies.

### Application Insights Sink

The Application Insights sink sends log events to Azure Application Insights for cloud-based monitoring and analytics:

```csharp
private static void LogToApplicationInsights(LoggerConfiguration config, HostConfig appSettings, SerilogConfig serilogConfig)
{
    if (serilogConfig.WriteToAppInsights)
    {
#pragma warning disable CS0618  // Suppress warning since we want to be able to log early error,
        // remove when the issue (https://github.com/serilog/serilog-sinks-applicationinsights/issues/121) is closed

        // TODO: TelemetryConverter should be configurable (defaulted to Trace)
        // Note: best practice is to use the existing Telemetry
        if (string.IsNullOrEmpty(TelemetryConfiguration.Active.InstrumentationKey))
        {
            config.WriteTo.ApplicationInsights(appSettings.ApplicationInsights.InstrumentationKey, TelemetryConverter.Traces);
            config.WriteTo.ApplicationInsights(appSettings.ApplicationInsights.InstrumentationKey, TelemetryConverter.Events);
        }
        else
        {
            config.WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Traces);
            config.WriteTo.ApplicationInsights(TelemetryConfiguration.Active, TelemetryConverter.Events);
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
```

**Telemetry Converters:**
- **Traces:** Log events are sent as trace telemetry (standard log messages)
- **Events:** Log events are also sent as custom events for analytics

**Configuration Requirements:**
1. Set `WriteToAppInsights` to `true` in `appsettings.json`
2. Provide a valid Application Insights instrumentation key
3. Ensure the application has network connectivity to Azure

**Known Issues:**
- The code uses deprecated API methods (CS0618 warning suppressed)
- GitHub issue tracking the deprecation: https://github.com/serilog/serilog-sinks-applicationinsights/issues/121
- The implementation checks for an existing `TelemetryConfiguration.Active` to reuse telemetry context when available

**TODO:** Make the `TelemetryConverter` type configurable rather than hardcoding both Traces and Events.

For monitoring and observability practices, see [Monitoring](/deployment/monitoring.md).

## Log Enrichment

### Correlation ID Enrichment

The application integrates with the Correlation ID middleware to enrich log events with request correlation identifiers. This enables tracking of requests across distributed systems and microservices.

**Middleware Registration in `Startup.cs`:**

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... other middleware (routing, CORS, authentication)
    
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

**Benefits:**
- **Request Tracing:** Track a single request through multiple services and components
- **Debugging:** Quickly locate all log entries related to a specific request
- **Performance Analysis:** Measure end-to-end request duration across services
- **Error Investigation:** Correlate errors with specific user actions or API calls

For detailed information on correlation ID implementation, see [Correlation ID](/features/correlation_id.md).

### Context Enrichment

Serilog automatically enriches log events with contextual information:

**Automatic Enrichment:**
- **Timestamp:** UTC timestamp of the log event
- **Log Level:** Severity level of the event
- **Message Template:** Structured message template with placeholders
- **Properties:** Custom properties attached to the log event
- **Exception Details:** Full exception information including stack traces

**Example Structured Log:**

```csharp
// In your code
_logger.LogInformation("Processing movie request for {MovieId} by user {UserId}", movieId, userId);

// Resulting structured log event
{
  "Timestamp": "2024-01-15T10:30:45.123Z",
  "Level": "Information",
  "MessageTemplate": "Processing movie request for {MovieId} by user {UserId}",
  "Properties": {
    "MovieId": 12345,
    "UserId": "user@example.com",
    "CorrelationId": "abc-123-def-456"
  }
}
```

### Request Logging

Serilog captures HTTP request information through ASP.NET Core integration:

**Captured Information:**
- HTTP method (GET, POST, PUT, DELETE, etc.)
- Request path and query string
- Response status code
- Request duration
- Client IP address (if configured)
- User agent information

**Configuration:**

The `config.ReadFrom.Configuration(ctx.Configuration)` call in the `UseSerilog` extension enables Serilog to read additional enrichment configuration from `appsettings.json`, including request logging settings.

## Best Practices

### Log Levels

**Guideline for Selecting Log Levels:**

```csharp
// Verbose: Extremely detailed, typically only enabled during specific investigations
_logger.LogTrace("Entering method ProcessMovie with parameters: {Parameters}", parameters);

// Debug: Detailed information for diagnosing issues during development
_logger.LogDebug("Database query executed: {Query}", query);

// Information: General flow of the application, business events
_logger.LogInformation("Movie {MovieId} successfully created by user {UserId}", movieId, userId);

// Warning: Unexpected situations that don't prevent operation
_logger.LogWarning("Movie {MovieId} not found in cache, fetching from database", movieId);

// Error: Errors and exceptions that are handled but indicate problems
_logger.LogError(ex, "Failed to process movie {MovieId}", movieId);

// Fatal: Critical errors that cause application termination
_logger.LogCritical(ex, "Database connection failed, application cannot continue");
```

**Production Recommendations:**
- Set minimum level to `Information` for normal operation
- Temporarily increase to `Debug` or `Verbose` for troubleshooting specific issues
- Never leave `Verbose` or `Debug` enabled in production long-term (performance impact)
- Use `Warning` and above for alerting and monitoring

### Structured Logging Patterns

**DO: Use Message Templates with Named Properties**

```csharp
// Good: Structured with named properties
_logger.LogInformation("User {UserId} updated movie {MovieId} with title {Title}", 
    userId, movieId, title);

// Bad: String interpolation loses structure
_logger.LogInformation($"User {userId} updated movie {movieId} with title {title}");
```

**DO: Use Consistent Property Names**

```csharp
// Consistent naming across the application
_logger.LogInformation("Processing request for {MovieId}", movieId);
_logger.LogWarning("Movie {MovieId} not found", movieId);
_logger.LogError("Failed to delete {MovieId}", movieId);
```

**DO: Include Relevant Context**

```csharp
// Include context that helps with debugging
_logger.LogInformation(
    "Movie search completed: {ResultCount} results for query {SearchQuery} in {ElapsedMs}ms",
    results.Count, searchQuery, stopwatch.ElapsedMilliseconds);
```

**DON'T: Log Entire Objects**

```csharp
// Bad: Logs object.ToString() which may not be useful
_logger.LogInformation("Processing movie: {Movie}", movieObject);

// Good: Log specific properties
_logger.LogInformation("Processing movie: {MovieId}, {Title}, {ReleaseYear}", 
    movie.Id, movie.Title, movie.ReleaseYear);
```

### Sensitive Data Handling

**Critical Security Considerations:**

```csharp
// NEVER log sensitive information
// ❌ DON'T DO THIS:
_logger.LogInformation("User logged in with password {Password}", password);
_logger.LogDebug("Processing payment with card number {CardNumber}", cardNumber);
_logger.LogInformation("API key: {ApiKey}", apiKey);

// ✅ DO THIS:
_logger.LogInformation("User {UserId} logged in successfully", userId);
_logger.LogDebug("Processing payment for user {UserId}", userId);
_logger.LogInformation("API authentication successful for client {ClientId}", clientId);
```

**Sensitive Data Categories to Avoid:**
- Passwords, API keys, tokens, secrets
- Credit card numbers, SSNs, personal identification numbers
- Full email addresses (consider logging only domain or hashed values)
- Personal health information (PHI)
- Personally identifiable information (PII) subject to GDPR/CCPA

**Data Masking Example:**

```csharp
// Mask sensitive portions of data
var maskedEmail = email.Substring(0, 3) + "***@" + email.Split('@')[1];
_logger.LogInformation("Password reset requested for {MaskedEmail}", maskedEmail);

// Or use hashing for correlation without exposing data
var userHash = ComputeHash(userId);
_logger.LogInformation("User action performed by {UserHash}", userHash);
```

### Performance Considerations

**Minimize Logging Overhead:**

```csharp
// Use log level checks for expensive operations
if (_logger.IsEnabled(LogLevel.Debug))
{
    var detailedInfo = ExpensiveSerializationMethod(largeObject);
    _logger.LogDebug("Detailed state: {DetailedInfo}", detailedInfo);
}

// Avoid unnecessary string formatting
// Bad: String is formatted even if logging is disabled
_logger.LogDebug($"Processing {items.Count} items: {string.Join(", ", items)}");

// Good: Formatting only happens if debug logging is enabled
_logger.LogDebug("Processing {ItemCount} items: {Items}", items.Count, items);
```

**Asynchronous Logging:**

Serilog sinks write asynchronously by default, minimizing impact on request processing. However, be aware of:

- **Buffer Limits:** Excessive logging can fill buffers, causing backpressure
- **Sink Performance:** Slow sinks (e.g., network-based) can impact application performance
- **Batching:** Application Insights sink batches events for efficiency

**Monitoring Logging Performance:**

```csharp
// Monitor logging subsystem health
_logger.LogInformation("Logging statistics: {EventsWritten} events, {FailedWrites} failures", 
    eventsWritten, failedWrites);
```

**Recommendations:**
- Avoid logging in tight loops or high-frequency operations
- Use sampling for high-volume events (log every Nth occurrence)
- Monitor Application Insights ingestion costs and quotas
- Implement log level filtering at the source to reduce sink load
- Consider using separate sinks for different log levels (e.g., errors to alerting system)

### Configuration Management

**Environment-Specific Configuration:**

Use different `appsettings.{Environment}.json` files for environment-specific logging:

```json
// appsettings.Development.json
{
  "BlackSlope.Api": {
    "Serilog": {
      "MinimumLevel": "debug",
      "WriteToConsole": "true",
      "WriteToFile": "false",
      "WriteToAppInsights": "false"
    }
  }
}

// appsettings.Production.json
{
  "BlackSlope.Api": {
    "Serilog": {
      "MinimumLevel": "information",
      "WriteToConsole": "true",
      "WriteToFile": "false",
      "WriteToAppInsights": "true"
    }
  }
}
```

**Azure Configuration:**

For Azure deployments, override settings using Application Settings or Key Vault references to avoid storing instrumentation keys in source control.

---

## Related Documentation

- [Application Settings](/configuration/application_settings.md) - Complete application configuration reference
- [Correlation ID](/features/correlation_id.md) - Request correlation and distributed tracing
- [Monitoring](/deployment/monitoring.md) - Production monitoring and observability practices