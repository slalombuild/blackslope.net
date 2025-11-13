# Middleware Pipeline

The BlackSlope API implements a carefully orchestrated middleware pipeline that processes HTTP requests and responses through a series of components. This pipeline handles cross-cutting concerns including HTTPS redirection, routing, CORS, authentication, correlation tracking, and exception handling.

## Pipeline Architecture

### Request Processing Flow

The middleware pipeline in ASP.NET Core processes requests in a specific order, with each middleware component having the opportunity to:

1. Process the incoming request before passing it to the next middleware
2. Execute logic after the next middleware has completed
3. Short-circuit the pipeline by not calling the next middleware

The request flows through the pipeline in the order middleware is registered, and the response flows back through the pipeline in reverse order:

```
Request  →  [Middleware 1]  →  [Middleware 2]  →  [Middleware 3]  →  Endpoint
Response ←  [Middleware 1]  ←  [Middleware 2]  ←  [Middleware 3]  ←  Endpoint
```

### Middleware Execution Order

The middleware pipeline is configured in the `Startup.Configure` method with the following execution order:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // 1. Developer Exception Page (Development only)
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // 2. HSTS (Production only)
        app.UseHsts();
    }

    // 3. Health Checks
    HealthCheckStartup.Configure(app, env, HostConfig);
    
    // 4. HTTPS Redirection
    app.UseHttpsRedirection();

    // 5. Swagger UI
    app.UseSwagger(HostConfig.Swagger);

    // 6. Routing
    app.UseRouting();
    
    // 7. CORS
    app.UseCors("AllowSpecificOrigin");

    // 8. Authentication
    app.UseAuthentication();

    // 9. Correlation ID Middleware (Custom)
    app.UseMiddleware<CorrelationIdMiddleware>();
    
    // 10. Exception Handling Middleware (Custom)
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 11. Endpoint Execution
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

**Critical Ordering Considerations:**

- **Exception handling** must be registered early to catch exceptions from subsequent middleware
- **Authentication** must come before authorization and any middleware that depends on user identity
- **CORS** must be positioned after `UseRouting()` but before `UseAuthentication()` and endpoint execution
- **Custom middleware** (CorrelationId, ExceptionHandling) is positioned after authentication but before endpoint execution

### Response Handling

Response handling occurs in reverse order through the pipeline. Each middleware can:

- Modify response headers before they're sent to the client
- Transform the response body
- Log response information
- Handle response-related errors

The `CorrelationIdMiddleware` demonstrates response handling using the `OnStarting` callback:

```csharp
context.Response.OnStarting(() =>
{
    _correlationIdResponseWriter.Write(context, correlationId);
    return Task.CompletedTask;
});
```

This ensures the correlation ID header is added to the response before any content is written.

## Built-in Middleware

### HTTPS Redirection

The `UseHttpsRedirection()` middleware automatically redirects HTTP requests to HTTPS, ensuring all communication is encrypted:

```csharp
app.UseHttpsRedirection();
```

**Configuration Notes:**
- Enabled in all environments (Development and Production)
- Uses default HTTPS port (443) unless configured otherwise
- Returns HTTP 307 (Temporary Redirect) status code by default

### Routing

The routing middleware is split into two components:

```csharp
// Endpoint routing - matches incoming requests to endpoints
app.UseRouting();

// ... other middleware ...

// Endpoint execution - executes the matched endpoint
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
```

This separation allows middleware between `UseRouting()` and `UseEndpoints()` to see which endpoint will be executed, enabling route-based decisions in CORS, authentication, and authorization middleware.

### CORS

Cross-Origin Resource Sharing (CORS) is configured to allow requests from any origin during development:

```csharp
private static void CorsConfiguration(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowSpecificOrigin",
            builder => builder.AllowAnyOrigin() // TODO: Replace with FE Service Host
                .AllowAnyHeader()
                .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE"));
    });
}
```

**Security Warning:** The current configuration uses `AllowAnyOrigin()`, which is permissive and suitable only for development. For production deployments, this should be replaced with specific origin URLs:

```csharp
builder.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
```

**Applied in Pipeline:**
```csharp
app.UseCors("AllowSpecificOrigin");
```

For detailed CORS configuration and security considerations, see [/security/cors.md](/security/cors.md).

### Authentication

Authentication middleware is configured using Azure Active Directory:

```csharp
app.UseAuthentication();
```

This middleware:
- Validates JWT tokens in the `Authorization` header
- Populates `HttpContext.User` with claims from the token
- Enables `[Authorize]` attributes on controllers and actions

The authentication is configured in `ConfigureServices`:

```csharp
services.AddAzureAd(HostConfig.AzureAd);
```

**Important:** Authentication middleware must be registered after `UseRouting()` and `UseCors()` but before `UseEndpoints()` to ensure proper request processing.

### HSTS (HTTP Strict Transport Security)

In production environments, HSTS middleware enforces HTTPS connections:

```csharp
if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // The default HSTS value is 30 days
    app.UseHsts();
}
```

HSTS instructs browsers to only communicate with the server over HTTPS for a specified duration (default: 30 days), preventing protocol downgrade attacks.

## Custom Middleware

### Correlation ID Middleware

The `CorrelationIdMiddleware` ensures every request has a unique identifier for distributed tracing and log correlation.

**Implementation:**

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
        // Read correlation ID from request header or generate new one
        var correlationId = _correlationIdRequestReader.Read(context) 
            ?? GenerateCorrelationId();
        
        // Store correlation ID for use throughout the request
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

    private static Guid GenerateCorrelationId() => Guid.NewGuid();
}
```

**Key Features:**

1. **Request Reading:** Attempts to read correlation ID from incoming request headers
2. **ID Generation:** Generates a new GUID if no correlation ID is provided
3. **Scoped Storage:** Stores the correlation ID in a scoped service (`ICurrentCorrelationIdService`) accessible throughout the request lifetime
4. **Response Writing:** Adds the correlation ID to response headers using `OnStarting` callback
5. **Dependency Injection:** Uses constructor injection for services with singleton lifetime and method injection for scoped services

**Registration:**

```csharp
// In ConfigureServices
services.AddCorrelation();

// In Configure
app.UseMiddleware<CorrelationIdMiddleware>();
```

**Usage Scenarios:**
- Tracking requests across microservices
- Correlating logs from different components
- Debugging distributed transactions
- Performance monitoring and tracing

For detailed correlation ID implementation and usage patterns, see [/features/correlation_id.md](/features/correlation_id.md).

### Exception Handling Middleware

The `ExceptionHandlingMiddleware` provides centralized exception handling with consistent error response formatting.

**Implementation:**

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = ApiHttpStatusCode.InternalServerError;
        string response;
        
        if (exception is ApiException)
        {
            // Handle custom API exceptions with specific error codes
            var apiException = exception as ApiException;
            statusCode = apiException.ApiHttpStatusCode;

            var apiErrors = new List<ApiError>();
            foreach (var error in apiException.ApiErrors)
            {
                apiErrors.Add(PrepareApiError(error.Code, error.Message));
            }

            var apiResponse = PrepareResponse(apiException.Data, apiErrors);
            response = Serialize(apiResponse);
        }
        else
        {
            // Handle unexpected exceptions with generic error message
            var apiErrors = new List<ApiError>
            {
                PrepareApiError((int)statusCode, statusCode.GetDescription()),
            };
            var apiResponse = PrepareResponse(null, apiErrors);
            response = Serialize(apiResponse);
        }

        _logger.LogError(exception, response);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(response);
    }

    private static ApiResponse PrepareResponse(
        object data, 
        IEnumerable<ApiError> apiErrors)
    {
        return new ApiResponse
        {
            Data = data,
            Errors = apiErrors,
        };
    }

    private static ApiError PrepareApiError(int code, string message)
    {
        return new ApiError
        {
            Code = code,
            Message = message,
        };
    }

    private static string Serialize(ApiResponse apiResponse)
    {
        return JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });
    }
}
```

**Key Features:**

1. **Exception Catching:** Wraps the entire pipeline in a try-catch block
2. **Custom Exception Handling:** Distinguishes between `ApiException` (expected errors) and unexpected exceptions
3. **Consistent Response Format:** Returns standardized JSON error responses
4. **Logging:** Logs all exceptions with full details for debugging
5. **Status Code Mapping:** Maps exceptions to appropriate HTTP status codes
6. **Multiple Error Support:** Can return multiple error messages in a single response

**Response Format:**

```json
{
  "data": null,
  "errors": [
    {
      "code": 500,
      "message": "Internal Server Error"
    }
  ]
}
```

**Registration:**

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Exception Handling Strategy:**

- **ApiException:** Custom exceptions thrown by business logic with specific error codes and messages
- **Other Exceptions:** Unexpected errors that return a generic 500 Internal Server Error response
- **Security:** Prevents sensitive error details from leaking to clients in production

For comprehensive exception handling patterns and custom exception types, see [/features/exception_handling.md](/features/exception_handling.md).

### Order and Dependencies

The custom middleware order is critical for proper functionality:

```csharp
app.UseAuthentication();                              // 1. Authenticate user
app.UseMiddleware<CorrelationIdMiddleware>();         // 2. Assign correlation ID
app.UseMiddleware<ExceptionHandlingMiddleware>();     // 3. Handle exceptions
```

**Dependency Rationale:**

1. **Authentication First:** User identity must be established before correlation tracking or exception handling
2. **Correlation ID Second:** The correlation ID should be available for logging in exception handling
3. **Exception Handling Third:** Must wrap endpoint execution to catch all exceptions

**Gotchas and Considerations:**

- **Exception Middleware Position:** If placed too early, it won't catch exceptions from authentication or CORS middleware
- **Correlation ID Timing:** Must be registered before any middleware that logs using the correlation ID
- **Response Modification:** Both custom middleware modify responses; ensure they don't conflict
- **Async/Await:** All middleware must properly await the next delegate to avoid threading issues

**Dependency Injection Patterns:**

```csharp
// Constructor injection for singleton/transient services
public CorrelationIdMiddleware(
    RequestDelegate next, 
    ICorrelationIdRequestReader reader)
{
    _next = next;
    _reader = reader;
}

// Method injection for scoped services
public async Task Invoke(
    HttpContext context, 
    ICurrentCorrelationIdService scopedService)
{
    // Use scopedService here
}
```

This pattern ensures proper service lifetime management and prevents captive dependencies.

## Integration with Application Architecture

The middleware pipeline integrates with the broader application architecture documented in [/architecture/overview.md](/architecture/overview.md):

- **Health Checks:** Registered early in the pipeline via `HealthCheckStartup.Configure()`
- **Swagger:** Configured for API documentation and testing
- **Logging:** Serilog integration configured in `Program.cs` via `UseSerilog()`
- **Configuration:** `HostConfig` provides environment-specific settings

**Startup Configuration Flow:**

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseSerilog(Assembly.GetExecutingAssembly().GetName().Name);
        webBuilder.UseStartup<Startup>();
    });
```

The `UseSerilog()` call is an extension method that configures Serilog logging with the application name. This ensures logging is configured before the middleware pipeline is built, allowing all middleware to use structured logging with correlation IDs.

## Performance Considerations

**Middleware Performance Impact:**

- **Correlation ID:** Minimal overhead (GUID generation and header manipulation)
- **Exception Handling:** Zero overhead in happy path; only impacts performance when exceptions occur
- **CORS:** Adds header validation overhead on every request
- **Authentication:** JWT validation adds latency; consider caching validated tokens

**Optimization Strategies:**

1. **Early Short-Circuiting:** Health check endpoints bypass most middleware
2. **Async Operations:** All middleware uses async/await for non-blocking I/O
3. **Memory Efficiency:** Response buffering is avoided where possible
4. **Caching:** Authentication results can be cached using `Microsoft.Extensions.Caching.Memory`

## Testing Middleware

**Unit Testing Approach:**

```csharp
[Fact]
public async Task CorrelationIdMiddleware_GeneratesId_WhenNotProvided()
{
    // Arrange
    var context = new DefaultHttpContext();
    var middleware = new CorrelationIdMiddleware(
        next: (innerContext) => Task.CompletedTask,
        correlationIdRequestReader: mockReader.Object,
        correlationIdResponseWriter: mockWriter.Object);

    // Act
    await middleware.Invoke(context, mockCorrelationService.Object);

    // Assert
    mockCorrelationService.Verify(x => x.SetId(It.IsAny<Guid>()), Times.Once);
}
```

**Integration Testing:**

Use `WebApplicationFactory<Startup>` to test the complete middleware pipeline with real HTTP requests and responses.

## Troubleshooting Common Issues

**Issue: CORS Errors in Production**
- **Cause:** `AllowAnyOrigin()` configuration
- **Solution:** Configure specific origins in production settings

**Issue: Correlation IDs Not Appearing in Logs**
- **Cause:** Middleware order or scoped service lifetime issues
- **Solution:** Ensure `CorrelationIdMiddleware` is registered before logging middleware

**Issue: Exception Details Leaking to Clients**
- **Cause:** `UseDeveloperExceptionPage()` enabled in production
- **Solution:** Verify environment-specific configuration in `Configure` method

**Issue: Authentication Failing Intermittently**
- **Cause:** Middleware order or missing CORS configuration
- **Solution:** Ensure `UseAuthentication()` is after `UseCors()` and before `UseEndpoints()`