# Debugging Guide

This guide provides comprehensive debugging strategies and techniques for the BlackSlope.NET application, covering local development, API testing, database troubleshooting, and remote debugging scenarios.

## Local Debugging

### Setting Breakpoints

The BlackSlope.NET solution supports standard Visual Studio and Visual Studio Code debugging capabilities for both the Web API and console applications.

**Web API Debugging:**

1. Open the solution in Visual Studio or VS Code
2. Set the startup project to `BlackSlope.Api`
3. Configure the launch profile in `launchSettings.json`:

```json
{
  "profiles": {
    "BlackSlope.Api": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```

4. Set breakpoints by clicking in the left margin of the code editor or pressing `F9`
5. Start debugging with `F5` or the Debug menu

**Console Application Debugging (RenameUtility):**

The RenameUtility console application shares core infrastructure with the Web API and can be debugged independently:

1. Set `RenameUtility` as the startup project
2. Configure command-line arguments in project properties if needed
3. Set breakpoints in shared authentication or data access code
4. Launch with `F5` to debug console-specific functionality

**Key Areas for Breakpoints:**

- **Middleware Pipeline**: Set breakpoints in custom middleware like `CorrelationIdMiddleware` to trace request flow
- **Controllers**: Debug API endpoint logic and request handling
- **Validators**: Inspect FluentValidation rules during request validation
- **Repository Layer**: Debug Entity Framework Core queries and data access
- **AutoMapper Profiles**: Verify object mapping transformations
- **Polly Policies**: Debug retry logic and circuit breaker behavior

### Step-Through Debugging

**Debugging Techniques:**

- **Step Into (F11)**: Navigate into method calls to debug internal implementation
- **Step Over (F10)**: Execute the current line and move to the next
- **Step Out (Shift+F11)**: Complete the current method and return to the caller
- **Run to Cursor (Ctrl+F10)**: Execute until reaching the cursor position

**Debugging Async/Await Code:**

The application extensively uses async/await patterns. When debugging asynchronous code:

```csharp
public async Task Invoke(HttpContext context, ICurrentCorrelationIdService currentCorrelationIdService)
{
    // Set breakpoint here to inspect incoming request
    var correlationId = _correlationIdRequestReader.Read(context) ?? GenerateCorrelationId();
    currentCorrelationIdService.SetId(correlationId);

    // Set breakpoint here to verify correlation ID is set
    context.Response.OnStarting(() =>
    {
        // This executes later - set breakpoint to verify response writing
        _correlationIdResponseWriter.Write(context, correlationId);
        return Task.CompletedTask;
    });

    // Set breakpoint here to debug next middleware in pipeline
    await _next(context);
}
```

**Tips for Async Debugging:**
- Use the **Tasks** window (Debug → Windows → Tasks) to view all running tasks
- Check the **Call Stack** window to understand async state machine transitions
- Be aware that stepping through async code may show compiler-generated state machine code

### Variable Inspection

**Locals Window:**
- View all local variables in the current scope
- Expand complex objects to inspect properties
- Particularly useful for inspecting `HttpContext`, Entity Framework entities, and DTOs

**Watch Window:**
- Add specific expressions to monitor across debugging sessions
- Useful for tracking correlation IDs: `currentCorrelationIdService.GetId()`
- Monitor Entity Framework change tracker: `_context.ChangeTracker.Entries()`

**Immediate Window:**
- Execute code during debugging session
- Test LINQ queries against in-memory collections
- Invoke methods to verify behavior

```csharp
// Example: Test AutoMapper mapping in Immediate Window
var dto = _mapper.Map<MovieDto>(movieEntity);
```

**Quick Watch (Shift+F9):**
- Inspect complex expressions without adding to Watch window
- Useful for examining nested objects and collections

### Call Stack Analysis

The **Call Stack** window is essential for understanding execution flow, especially in the middleware pipeline.

**Typical Call Stack for API Request:**

```
BlackSlope.Api.Controllers.MoviesController.GetById(int id)
  ↓
BlackSlope.Api.Services.MovieService.GetMovieByIdAsync(int id)
  ↓
BlackSlope.Api.Repositories.MovieRepository.GetByIdAsync(int id)
  ↓
Microsoft.EntityFrameworkCore.DbSet<Movie>.FindAsync(int id)
  ↓
BlackSlope.Api.Common.Middleware.Correlation.CorrelationIdMiddleware.Invoke(HttpContext context)
  ↓
BlackSlope.Api.Common.Middleware.ExceptionHandling.ExceptionHandlingMiddleware.Invoke(HttpContext context)
```

**Analyzing Middleware Order:**
The call stack reveals middleware execution order. The deepest middleware in the stack executes first. Use this to verify:
- Exception handling middleware is outermost
- Correlation ID middleware executes early
- Authentication middleware runs before authorization

## Logging for Debugging

### Enabling Debug Logs

The application uses **Serilog** for structured logging with configuration defined in `SerilogConfig`:

```csharp
public class SerilogConfig
{
    public string MinimumLevel { get; set; }
    public string FileName { get; set; }
    public bool WriteToConsole { get; set; }
    public bool WriteToFile { get; set; }
    public bool WriteToAppInsights { get; set; }
}
```

**Development Environment Configuration:**

Update `appsettings.Development.json` to enable debug-level logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  }
}
```

**Granular Logging Control:**

Enable debug logging for specific namespaces:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "BlackSlope.Api.Controllers": "Debug",
      "BlackSlope.Api.Repositories": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Entity Framework Core Query Logging:**

To debug SQL queries generated by EF Core:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Query": "Debug"
    }
  }
}
```

### Serilog Configuration

**Console Logging:**

Enable console output for immediate feedback during debugging:

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteToConsole": true,
    "WriteToFile": false,
    "WriteToAppInsights": false
  }
}
```

**File Logging:**

Configure file-based logging for persistent debug information:

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "FileName": "logs/blackslope-debug-.txt",
    "WriteToConsole": true,
    "WriteToFile": true,
    "WriteToAppInsights": false
  }
}
```

**Structured Logging Best Practices:**

Use structured logging with named properties for better searchability:

```csharp
// Good - Structured logging
_logger.LogDebug("Processing movie request for MovieId: {MovieId}, UserId: {UserId}", 
    movieId, userId);

// Avoid - String interpolation loses structure
_logger.LogDebug($"Processing movie request for MovieId: {movieId}, UserId: {userId}");
```

**Log Levels:**

| Level | Usage | Example Scenario |
|-------|-------|------------------|
| **Trace** | Very detailed diagnostic information | Method entry/exit, loop iterations |
| **Debug** | Detailed information for debugging | Variable values, conditional branches |
| **Information** | General application flow | Request started, operation completed |
| **Warning** | Unexpected but recoverable situations | Retry attempts, fallback logic |
| **Error** | Error conditions that don't stop execution | Handled exceptions, validation failures |
| **Critical** | Fatal errors requiring immediate attention | Unhandled exceptions, system failures |

### Reading Log Output

**Console Output Format:**

```
[2024-01-15 10:23:45.123 DBG] Processing movie request for MovieId: 42, UserId: "user123", CorrelationId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
[2024-01-15 10:23:45.234 INF] Executing SQL query: SELECT * FROM Movies WHERE Id = @p0
[2024-01-15 10:23:45.345 DBG] AutoMapper mapping Movie entity to MovieDto
[2024-01-15 10:23:45.456 INF] Request completed in 333ms with status code 200
```

**File Output:**

Log files are typically written to the `logs/` directory with rolling file names based on date:
- `logs/blackslope-debug-20240115.txt`
- `logs/blackslope-debug-20240116.txt`

**Filtering Logs:**

Use text search or log analysis tools to filter by:
- **Correlation ID**: Find all logs for a specific request
- **Log Level**: Focus on errors or debug information
- **Namespace**: Filter by component (Controllers, Repositories, etc.)
- **Timestamp**: Narrow down to specific time ranges

### Correlation ID Tracing

The application implements correlation ID tracking through `CorrelationIdMiddleware` to trace requests across the entire pipeline.

**How Correlation IDs Work:**

```csharp
public async Task Invoke(HttpContext context, ICurrentCorrelationIdService currentCorrelationIdService)
{
    // Read correlation ID from request header or generate new one
    var correlationId = _correlationIdRequestReader.Read(context) ?? GenerateCorrelationId();
    
    // Store in scoped service for access throughout request
    currentCorrelationIdService.SetId(correlationId);

    // Write correlation ID to response header
    context.Response.OnStarting(() =>
    {
        _correlationIdResponseWriter.Write(context, correlationId);
        return Task.CompletedTask;
    });

    await _next(context);
}

private static Guid GenerateCorrelationId() => Guid.NewGuid();
```

**Using Correlation IDs for Debugging:**

1. **Client-Side**: Send `X-Correlation-ID` header with requests
2. **Server-Side**: Correlation ID is automatically logged with each log entry
3. **Tracing**: Search logs by correlation ID to see complete request flow

**Example Log Trace:**

```
[10:23:45.123 DBG] CorrelationId: a1b2c3d4 - Request started: GET /api/movies/42
[10:23:45.234 DBG] CorrelationId: a1b2c3d4 - Validating request parameters
[10:23:45.345 DBG] CorrelationId: a1b2c3d4 - Executing database query
[10:23:45.456 DBG] CorrelationId: a1b2c3d4 - Mapping entity to DTO
[10:23:45.567 INF] CorrelationId: a1b2c3d4 - Request completed: 200 OK
```

**Distributed Tracing:**

For microservices or external API calls, propagate correlation IDs:

```csharp
// Add correlation ID to outgoing HTTP requests
httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId.ToString());
```

For more details on correlation ID implementation, see [Correlation ID Feature Documentation](/features/correlation_id.md).

## API Debugging

### Using Swagger for Testing

The application includes **Swashbuckle.AspNetCore.SwaggerUI** (version 6.3.0) for interactive API documentation and testing.

**Accessing Swagger UI:**

1. Start the application in Development mode
2. Navigate to `https://localhost:5001/swagger` or `http://localhost:5000/swagger`
3. The Swagger UI displays all available endpoints with schemas

**Testing Endpoints:**

1. **Expand an endpoint** to view details (parameters, request body, responses)
2. **Click "Try it out"** to enable interactive testing
3. **Enter parameters** or request body JSON
4. **Click "Execute"** to send the request
5. **Review response** including status code, headers, and body

**Debugging with Swagger:**

- **Set breakpoints** in controller actions before executing Swagger requests
- **Inspect request binding** to verify parameter mapping and model validation
- **Test validation rules** by sending invalid data and observing FluentValidation responses
- **Verify response mapping** by checking AutoMapper transformations

**Custom Headers:**

Add custom headers like correlation IDs in Swagger:
1. Click "Authorize" or use the request header section
2. Add `X-Correlation-ID` with a test GUID
3. Execute request and verify the same ID appears in response headers

### Postman Debugging

**Setting Up Postman:**

1. Import API endpoints from Swagger JSON: `https://localhost:5001/swagger/v1/swagger.json`
2. Create environment variables for base URL and authentication tokens
3. Configure pre-request scripts for correlation IDs

**Postman Pre-Request Script for Correlation ID:**

```javascript
// Generate correlation ID if not provided
if (!pm.request.headers.has("X-Correlation-ID")) {
    const correlationId = pm.variables.replaceIn('{{$guid}}');
    pm.request.headers.add({
        key: "X-Correlation-ID",
        value: correlationId
    });
    pm.environment.set("lastCorrelationId", correlationId);
}
```

**Postman Tests for Debugging:**

```javascript
// Verify correlation ID is returned
pm.test("Correlation ID returned", function () {
    pm.response.to.have.header("X-Correlation-ID");
    const responseCorrelationId = pm.response.headers.get("X-Correlation-ID");
    const requestCorrelationId = pm.request.headers.get("X-Correlation-ID");
    pm.expect(responseCorrelationId).to.eql(requestCorrelationId);
});

// Log response time for performance debugging
pm.test("Response time acceptable", function () {
    console.log("Response time: " + pm.response.responseTime + "ms");
    pm.expect(pm.response.responseTime).to.be.below(2000);
});
```

**Collection Runner for Regression Testing:**

Use Postman's Collection Runner to execute multiple requests sequentially, useful for debugging workflow scenarios and data dependencies.

### HTTP Request Inspection

**Using Fiddler or Browser DevTools:**

1. **Fiddler**: Configure as system proxy to capture all HTTP traffic
2. **Browser DevTools**: Use Network tab for browser-based API calls
3. **Inspect request headers**: Verify authentication tokens, correlation IDs, content types
4. **Examine request body**: Validate JSON structure and data types

**Common Request Issues:**

| Issue | Symptom | Debug Approach |
|-------|---------|----------------|
| **Missing Authentication** | 401 Unauthorized | Verify `Authorization: Bearer <token>` header |
| **Invalid Content-Type** | 415 Unsupported Media Type | Ensure `Content-Type: application/json` |
| **Malformed JSON** | 400 Bad Request | Validate JSON syntax and structure |
| **Missing Required Fields** | 400 Bad Request with validation errors | Check FluentValidation rules |
| **CORS Issues** | Preflight OPTIONS failure | Verify CORS policy configuration |

**Request Logging Middleware:**

Enable detailed request logging by setting log level to Debug:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Hosting": "Debug",
      "Microsoft.AspNetCore.Routing": "Debug"
    }
  }
}
```

### Response Analysis

**Status Codes:**

| Code | Meaning | Common Causes |
|------|---------|---------------|
| **200 OK** | Success | Request processed successfully |
| **201 Created** | Resource created | POST request successful |
| **204 No Content** | Success with no body | DELETE or PUT successful |
| **400 Bad Request** | Client error | Validation failure, malformed request |
| **401 Unauthorized** | Authentication required | Missing or invalid token |
| **403 Forbidden** | Authorization failed | Valid token but insufficient permissions |
| **404 Not Found** | Resource not found | Invalid ID or route |
| **500 Internal Server Error** | Server error | Unhandled exception (check logs) |

**Response Headers:**

Key headers to inspect:
- **X-Correlation-ID**: Trace request through logs
- **Content-Type**: Verify response format (application/json)
- **Cache-Control**: Check caching behavior
- **Date**: Verify server time synchronization

**Response Body Validation:**

- **Schema Validation**: Compare response against Swagger schema
- **Data Integrity**: Verify mapped properties match expected values
- **Null Handling**: Check for unexpected null values in non-nullable fields
- **Collection Counts**: Verify pagination and filtering results

**Exception Response Format:**

The application uses custom exception handling middleware. Error responses follow this structure:

```json
{
  "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "message": "An error occurred processing your request",
  "details": "Validation failed for property 'Title': Title is required",
  "timestamp": "2024-01-15T10:23:45.123Z"
}
```

## Database Debugging

### SQL Server Profiler

**Setting Up SQL Server Profiler:**

1. Open **SQL Server Management Studio (SSMS)**
2. Navigate to **Tools → SQL Server Profiler**
3. Create a new trace with template "Standard"
4. Filter by **DatabaseName** to focus on your application database
5. Filter by **ApplicationName** to isolate Entity Framework queries

**Key Events to Monitor:**

- **SQL:BatchCompleted**: View completed SQL statements
- **RPC:Completed**: Track stored procedure calls
- **Errors and Warnings**: Identify SQL errors and deadlocks
- **Audit Login/Logout**: Monitor connection pool behavior

**Profiler Filters for Debugging:**

```
ApplicationName LIKE 'EntityFramework%'
Duration > 1000  -- Queries taking more than 1 second
```

**Alternative: Extended Events:**

For production environments, use Extended Events instead of Profiler:

```sql
CREATE EVENT SESSION [BlackSlope_Debug] ON SERVER
ADD EVENT sqlserver.sql_statement_completed(
    WHERE ([duration] > 1000000)  -- 1 second in microseconds
)
ADD TARGET package0.event_file(SET filename=N'C:\Logs\BlackSlope_Debug.xel')
WITH (MAX_MEMORY=4096 KB, EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS);

ALTER EVENT SESSION [BlackSlope_Debug] ON SERVER STATE = START;
```

### Query Analysis

**Entity Framework Core Query Logging:**

Enable detailed query logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Sample Query Log Output:**

```
Executed DbCommand (45ms) [Parameters=[@p0='42'], CommandType='Text', CommandTimeout='30']
SELECT [m].[Id], [m].[Title], [m].[ReleaseDate], [m].[Genre]
FROM [Movies] AS [m]
WHERE [m].[Id] = @p0
```

**Common Query Performance Issues:**

| Issue | Symptom | Solution |
|-------|---------|----------|
| **N+1 Queries** | Multiple queries for related data | Use `.Include()` or `.ThenInclude()` |
| **Missing Indexes** | Slow WHERE or JOIN clauses | Add indexes to frequently queried columns |
| **SELECT N+1** | Lazy loading triggers multiple queries | Use eager loading with `.Include()` |
| **Large Result Sets** | Memory pressure, slow responses | Implement pagination with `.Skip()` and `.Take()` |
| **Cartesian Products** | Exponential result growth | Review JOIN conditions |

**Analyzing Query Plans:**

In SSMS, enable **Include Actual Execution Plan** (Ctrl+M) before running queries:

1. Copy SQL from EF Core logs
2. Execute in SSMS with execution plan enabled
3. Look for:
   - **Table Scans**: Indicate missing indexes
   - **Key Lookups**: Suggest covering indexes
   - **High Cost Operations**: Identify bottlenecks
   - **Warnings**: Address implicit conversions or missing statistics

### Entity Framework Logging

**DbContext Logging Configuration:**

Configure detailed EF Core logging in your DbContext:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseSqlServer(connectionString)
        .EnableSensitiveDataLogging()  // Include parameter values (Development only!)
        .EnableDetailedErrors()         // Include detailed error information
        .LogTo(Console.WriteLine, LogLevel.Information);
}
```

**⚠️ Security Warning:** Only enable `EnableSensitiveDataLogging()` in Development environments. It exposes parameter values in logs, which may include sensitive data.

**Change Tracker Debugging:**

Inspect Entity Framework's change tracker to understand entity states:

```csharp
// In your repository or service
var entries = _context.ChangeTracker.Entries();
foreach (var entry in entries)
{
    _logger.LogDebug("Entity: {EntityType}, State: {State}", 
        entry.Entity.GetType().Name, 
        entry.State);
}
```

**Entity States:**

- **Detached**: Not tracked by context
- **Unchanged**: Tracked but not modified
- **Added**: Will be inserted on SaveChanges
- **Modified**: Will be updated on SaveChanges
- **Deleted**: Will be deleted on SaveChanges

**Debugging SaveChanges Failures:**

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database update failed");
    
    // Inspect inner exception for SQL error details
    if (ex.InnerException is SqlException sqlEx)
    {
        _logger.LogError("SQL Error Number: {ErrorNumber}, Message: {Message}", 
            sqlEx.Number, sqlEx.Message);
    }
    
    // Log entity states
    foreach (var entry in ex.Entries)
    {
        _logger.LogError("Failed entity: {EntityType}, State: {State}", 
            entry.Entity.GetType().Name, 
            entry.State);
    }
    
    throw;
}
```

**Common DbUpdateException Scenarios:**

| SQL Error | Meaning | Debug Approach |
|-----------|---------|----------------|
| **2627** | Unique constraint violation | Check for duplicate keys |
| **547** | Foreign key constraint violation | Verify related entities exist |
| **2601** | Duplicate key | Check unique indexes |
| **8152** | String truncation | Verify string lengths match column definitions |

### Connection Debugging

**Connection String Configuration:**

The application uses `Microsoft.Data.SqlClient` (version 5.1.3). Verify connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=localhost;Database=Movies;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Connection String Troubleshooting:**

| Issue | Symptom | Solution |
|-------|---------|----------|
| **Server not found** | SqlException: "A network-related or instance-specific error" | Verify server name, check SQL Server is running |
| **Login failed** | SqlException: "Login failed for user" | Check credentials, verify SQL authentication mode |
| **Database not found** | SqlException: "Cannot open database" | Verify database exists, check user permissions |
| **SSL/TLS error** | SqlException: "The certificate chain was issued by an authority that is not trusted" | Add `TrustServerCertificate=True` (Development only) |

**Connection Pool Monitoring:**

Enable connection pool logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Data.SqlClient": "Debug"
    }
  }
}
```

**Connection Pool Issues:**

- **Pool Exhaustion**: Increase `Max Pool Size` or fix connection leaks
- **Connection Leaks**: Ensure DbContext is properly disposed (use `using` statements)
- **Timeout Errors**: Increase `Connection Timeout` or optimize long-running queries

**Health Checks for Database Connectivity:**

The application includes `AspNetCore.HealthChecks.SqlServer` (version 5.0.3) and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (version 6.0.1).

Access health check endpoint: `https://localhost:5001/health`

**Sample Health Check Response:**

```json
{
  "status": "Healthy",
  "results": {
    "sqlserver": {
      "status": "Healthy",
      "description": "SQL Server is responsive",
      "duration": "00:00:00.0234567"
    },
    "dbcontext": {
      "status": "Healthy",
      "description": "Entity Framework Core DbContext is healthy",
      "duration": "00:00:00.0123456"
    }
  }
}
```

**Debugging Unhealthy Status:**

If health checks fail:
1. Check application logs for detailed error messages
2. Verify SQL Server is running and accessible
3. Test connection string manually using SSMS
4. Check firewall rules and network connectivity
5. Verify database user permissions

## Remote Debugging

### Attaching to Containers

The application supports Docker containerization with **Microsoft.VisualStudio.Azure.Containers.Tools.Targets** (version 1.14.0).

**Docker Configuration:**

The application includes Docker support with Windows containers as the default target. The `launchSettings.json` includes a Docker profile:

```json
{
  "profiles": {
    "Docker": {
      "commandName": "Docker",
      "launchBrowser": true,
      "launchUrl": "{Scheme}://{ServiceHost}:{ServicePort}/swagger"
    }
  }
}
```

**Building and Running Docker Container:**

```bash
# Navigate to /src directory
cd src

# Build Docker image
docker build -t blackslope.api -f Dockerfile .

# Run container with debugging port exposed
docker run -d -p 5000:80 -p 5001:443 --name blackslope-container blackslope.api
```

**Attaching Visual Studio Debugger to Container:**

1. **Start container** with the application running
2. In Visual Studio, select **Debug → Attach to Process**
3. Set **Connection type** to "Docker (Windows Container)"
4. Select **blackslope-container** from the list
5. Find the **dotnet.exe** process for BlackSlope.Api
6. Click **Attach**

**Remote Debugging Prerequisites:**

- Visual Studio Remote Debugger must be installed in the container
- Container must be running in debug configuration
- Firewall rules must allow debugger communication

**Dockerfile for Debug Configuration:**

```dockerfile
# Use SDK image for debugging
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS debug
WORKDIR /app
COPY . .

# Install remote debugger
RUN apt-get update && apt-get install -y unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

# Expose debugging port
EXPOSE 4024

ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

**VS Code Remote Debugging:**

Configure `.vscode/launch.json` for Docker debugging:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Docker .NET Attach",
      "type": "docker",
      "request": "attach",
      "platform": "netCore",
      "sourceFileMap": {
        "/app": "${workspaceFolder}/src"
      }
    }
  ]
}
```

### Remote Debugging Configuration

**Environment-Specific Configuration:**

Use environment variables to enable remote debugging:

```bash
# Set environment variable in container
docker run -e ASPNETCORE_ENVIRONMENT=Development \
           -e REMOTE_DEBUGGING=true \
           -p 5000:80 -p 4024:4024 \
           blackslope.api
```

**Debugging Symbols:**

Ensure PDB files are included in the container for source-level debugging:

```dockerfile
# Copy PDB files for debugging
COPY --from=build /app/publish/*.pdb /app/
```

**Network Configuration:**

For remote debugging across networks:
1. Expose debugger port (default 4024)
2. Configure firewall rules to allow inbound connections
3. Use SSH tunneling for secure remote debugging

**SSH Tunnel Example:**

```bash
# Create SSH tunnel to remote Docker host
ssh -L 4024:localhost:4024 user@remote-host

# Attach debugger to localhost:4024
```

### Production Debugging Considerations

**⚠️ Security Warnings:**

- **Never enable remote debugging in production** without proper security controls
- **Disable sensitive data logging** in production environments
- **Use read-only debugging** when possible to avoid state changes
- **Limit debugging access** to authorized personnel only

**Production-Safe Debugging Techniques:**

1. **Snapshot Debugging**: Use Azure Application Insights Snapshot Debugger for production issues
2. **Log Analysis**: Rely on comprehensive logging rather than live debugging
3. **Health Checks**: Monitor application health endpoints
4. **Metrics**: Use Application Performance Monitoring (APM) tools
5. **Staging Environment**: Reproduce issues in staging before debugging

**Application Insights Integration:**

The application supports Azure Application Insights through `SerilogConfig`:

```csharp
public class SerilogConfig
{
    public bool WriteToAppInsights { get; set; }
}
```

Enable Application Insights in production:

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteToAppInsights": true
  },
  "ApplicationInsights": {
    "InstrumentationKey": "your-instrumentation-key"
  }
}
```

**Production Debugging Checklist:**

- [ ] Verify comprehensive logging is enabled
- [ ] Ensure correlation IDs are propagated
- [ ] Configure Application Insights or equivalent APM
- [ ] Set up alerts for critical errors
- [ ] Enable health check endpoints
- [ ] Configure SQL Server Extended Events (not Profiler)
- [ ] Use read-only database replicas for query analysis
- [ ] Document incident response procedures

**Polly Resilience Debugging:**

The application uses **Polly** (version 7.2.2) for resilience patterns. Debug retry and circuit breaker behavior:

```csharp
// Enable Polly logging
services.AddHttpClient("FakeApi")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Log retry attempts
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                // Log retry attempts for debugging
                Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
            });
}
```

**Monitoring Polly Policies:**

- Track retry counts and durations
- Monitor circuit breaker state transitions (Closed → Open → Half-Open)
- Alert on excessive retries indicating upstream service issues
- Log timeout occurrences for performance analysis

---

## Related Documentation

- [Common Issues Troubleshooting Guide](/troubleshooting/common_issues.md) - Solutions to frequently encountered problems
- [Logging Configuration](/configuration/logging.md) - Detailed Serilog configuration and best practices
- [Correlation ID Feature](/features/correlation_id.md) - Implementation details for request tracing

## Additional Resources

- [Entity Framework Core Logging Documentation](https://docs.microsoft.com/en-us/ef/core/logging-events-diagnostics/)
- [ASP.NET Core Debugging Guide](https://docs.microsoft.com/en-us/aspnet/core/test/debugging)
- [Polly Documentation](https://github.com/App-vNext/Polly)
- [Docker Debugging in Visual Studio](https://docs.microsoft.com/en-us/visualstudio/containers/edit-and-refresh)