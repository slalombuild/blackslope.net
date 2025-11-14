# CORS Configuration

## Cross-Origin Resource Sharing

### CORS Concepts

Cross-Origin Resource Sharing (CORS) is a security feature implemented by web browsers to control how web applications running at one origin (domain) can access resources from a different origin. In the BlackSlope.NET application, CORS is configured to enable the web API to be consumed by frontend applications that may be hosted on different domains.

**Same-Origin Policy**: By default, browsers enforce the Same-Origin Policy, which restricts web pages from making requests to a different domain than the one that served the web page. Two URLs have the same origin if they share the same:
- Protocol (http/https)
- Domain (example.com)
- Port (80, 443, etc.)

**CORS Mechanism**: CORS relaxes this restriction by allowing servers to specify which origins are permitted to access their resources through HTTP headers. The server includes specific CORS headers in its responses to inform the browser which cross-origin requests should be allowed.

### Browser Security Model

The browser security model enforces CORS policies through a series of checks:

1. **Simple Requests**: For simple requests (GET, HEAD, POST with specific content types), the browser sends the request directly and checks the `Access-Control-Allow-Origin` header in the response.

2. **Preflight Requests**: For complex requests (those with custom headers, methods other than GET/POST/HEAD, or specific content types), the browser first sends an OPTIONS request to determine if the actual request is safe to send.

3. **Credentials**: When requests include credentials (cookies, authorization headers), additional restrictions apply. The server must explicitly allow credentials, and wildcards cannot be used for origins.

### Pre-flight Requests

Pre-flight requests are OPTIONS requests sent by the browser before the actual request when certain conditions are met:

**Triggers for Pre-flight**:
- HTTP methods: PUT, DELETE, PATCH, or custom methods
- Custom headers beyond simple headers (Accept, Accept-Language, Content-Language, Content-Type)
- Content-Type values other than application/x-www-form-urlencoded, multipart/form-data, or text/plain

**Pre-flight Flow**:
```
Client                          Server
  |                               |
  |--- OPTIONS (Pre-flight) ----->|
  |                               |
  |<-- CORS Headers (200 OK) -----|
  |                               |
  |--- Actual Request (PUT) ----->|
  |                               |
  |<-- Response with Data --------|
```

The server must respond to the OPTIONS request with appropriate CORS headers indicating which methods, headers, and origins are allowed.

## Current Configuration

The BlackSlope.NET application currently implements a permissive CORS policy that allows requests from any origin. This configuration is located in the `Startup.cs` file:

```csharp
private static void CorsConfiguration(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowSpecificOrigin",
            builder => builder.AllowAnyOrigin() // TODO: Replace with FE Service Host as appropriate to constrain clients
                .AllowAnyHeader()
                .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE"));
    });
}
```

### AllowAnyOrigin Setup (TODO: Restrict)

**Current Implementation**:
- **Policy Name**: `AllowSpecificOrigin` (note: the name is misleading as it currently allows all origins)
- **Origin Configuration**: `AllowAnyOrigin()` - accepts requests from any domain
- **Security Implication**: This is a development-friendly configuration but poses security risks in production

**Important Note**: The TODO comment in the code explicitly indicates this needs to be changed before production deployment:
```csharp
// TODO: Replace with FE Service Host as appropriate to constrain clients
```

**Why This Matters**:
- Allowing any origin means any website can make requests to your API
- This can expose your API to unauthorized access and potential data leakage
- Credentials (cookies, authorization tokens) cannot be used with `AllowAnyOrigin()`

### Allowed Methods

The current configuration explicitly allows the following HTTP methods:

```csharp
.WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")
```

**Method Breakdown**:
- **GET**: Retrieve resources (read operations)
- **POST**: Create new resources
- **PUT**: Update existing resources (full replacement)
- **DELETE**: Remove resources
- **OPTIONS**: Pre-flight requests (automatically handled by CORS middleware)

**Note**: PATCH is not currently included in the allowed methods list. If your API uses PATCH for partial updates, you'll need to add it to this list.

### Allowed Headers

The configuration uses `AllowAnyHeader()`, which permits any HTTP header in cross-origin requests:

```csharp
.AllowAnyHeader()
```

**Common Headers This Allows**:
- `Content-Type`: Specifies the media type of the request body
- `Authorization`: Contains authentication credentials (JWT tokens, etc.)
- `X-Correlation-Id`: Custom header used by BlackSlope for request tracking
- `Accept`: Specifies acceptable response media types
- Custom application-specific headers

**Security Consideration**: While convenient for development, allowing any header in production should be evaluated based on your security requirements. Consider restricting to only the headers your application actually uses.

### Policy Application

The CORS policy is applied in the middleware pipeline in the `Configure` method:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... other middleware
    
    app.UseRouting();
    app.UseCors("AllowSpecificOrigin");  // Applied after UseRouting
    
    app.UseAuthentication();
    
    // ... remaining middleware
}
```

**Middleware Order Importance**:
1. `UseRouting()` must be called before `UseCors()`
2. `UseCors()` must be called before `UseAuthentication()` and `UseAuthorization()`
3. This ensures CORS headers are added before authentication checks occur

## Production CORS Setup

For production deployments, the CORS configuration must be updated to restrict access to specific, trusted origins. Here's how to implement a secure CORS policy:

### Specific Origin Configuration

**Recommended Production Configuration**:

```csharp
private static void CorsConfiguration(IServiceCollection services, IConfiguration configuration)
{
    // Read allowed origins from configuration
    var allowedOrigins = configuration.GetSection("BlackSlope.Api:AllowedOrigins")
        .Get<string[]>() ?? new string[] { };

    services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowSpecificOrigin",
            builder => builder
                .WithOrigins(allowedOrigins)  // Specific origins only
                .AllowAnyHeader()
                .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")
                .AllowCredentials());  // Enable credentials support
    });
}
```

**Configuration in appsettings.json**:

```json
{
  "BlackSlope.Api": {
    "AllowedOrigins": [
      "https://app.yourcompany.com",
      "https://admin.yourcompany.com"
    ],
    // ... other configuration
  }
}
```

**Environment-Specific Configuration**:

For different environments, use environment-specific configuration files:

- `appsettings.Development.json`:
```json
{
  "BlackSlope.Api": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200"
    ]
  }
}
```

- `appsettings.Production.json`:
```json
{
  "BlackSlope.Api": {
    "AllowedOrigins": [
      "https://app.yourcompany.com"
    ]
  }
}
```

### Credentials Support

When your API requires authentication (as BlackSlope does with Azure AD), you must enable credentials support:

```csharp
builder => builder
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")
    .AllowCredentials()  // Required for cookies and Authorization headers
```

**Important Constraints**:
- `AllowCredentials()` cannot be used with `AllowAnyOrigin()`
- You must specify exact origins when using credentials
- Wildcards in origins are not supported with credentials

**Authentication Integration**:

BlackSlope uses Azure AD authentication (see [/security/authentication.md](/security/authentication.md)), which requires the Authorization header to be sent with requests:

```csharp
// From appsettings.json
"AzureAd": {
  "AadInstance": "https://login.microsoftonline.com/{0}",
  "Tenant": "[tenant-id]",
  "Audience": "https://[host-name]"
}
```

The CORS policy must allow credentials for the JWT tokens to be included in cross-origin requests.

### Exposed Headers

By default, browsers only expose a limited set of response headers to JavaScript. If your API returns custom headers that the frontend needs to access, you must explicitly expose them:

```csharp
builder => builder
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")
    .AllowCredentials()
    .WithExposedHeaders("X-Correlation-Id", "X-Pagination-Total", "X-Custom-Header")
```

**BlackSlope Custom Headers**:

The application uses a correlation ID middleware (see [/architecture/middleware_pipeline.md](/architecture/middleware_pipeline.md)):

```csharp
// From Startup.cs
app.UseMiddleware<CorrelationIdMiddleware>();
```

If your frontend needs to access the `X-Correlation-Id` header for logging or debugging, it must be exposed:

```csharp
.WithExposedHeaders("X-Correlation-Id")
```

**Common Headers to Expose**:
- `X-Correlation-Id`: Request tracking identifier
- `X-Total-Count`: Total number of items (for pagination)
- `Location`: URL of newly created resources
- `ETag`: Resource version identifier
- Custom business-specific headers

## Best Practices

### Restricting Origins

**1. Never Use AllowAnyOrigin in Production**

The current configuration is explicitly marked for change:
```csharp
builder.AllowAnyOrigin() // TODO: Replace with FE Service Host
```

**2. Use Configuration-Based Origins**

Store allowed origins in configuration files rather than hardcoding them:

```csharp
// Good: Configuration-based
var allowedOrigins = configuration.GetSection("BlackSlope.Api:AllowedOrigins").Get<string[]>();
builder.WithOrigins(allowedOrigins)

// Bad: Hardcoded
builder.WithOrigins("https://app.example.com", "https://admin.example.com")
```

**3. Use Exact Matches**

Avoid wildcards and subdomain patterns when possible:

```csharp
// Good: Exact origins
.WithOrigins("https://app.example.com", "https://admin.example.com")

// Risky: Subdomain wildcard (not supported by default)
// .WithOrigins("https://*.example.com")  // This won't work as expected
```

**4. Implement Origin Validation**

For advanced scenarios, implement custom origin validation:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.SetIsOriginAllowed(origin =>
        {
            // Custom validation logic
            var uri = new Uri(origin);
            return uri.Host.EndsWith(".yourcompany.com") && uri.Scheme == "https";
        })
        .AllowAnyHeader()
        .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")
        .AllowCredentials();
    });
});
```

### Security Implications

**1. CORS is Not a Security Feature for Server-Side Protection**

CORS is enforced by browsers, not servers. It protects users, not your API:
- Server-side validation and authentication are still required
- CORS does not prevent direct API calls from tools like Postman or curl
- Always implement proper authentication (BlackSlope uses Azure AD - see [/security/authentication.md](/security/authentication.md))

**2. Credentials and Origins**

When using `AllowCredentials()`:
- Never use `AllowAnyOrigin()` - this combination is not allowed
- Specify exact origins
- Ensure HTTPS is used in production

**3. Pre-flight Request Caching**

Configure pre-flight caching to reduce OPTIONS requests:

```csharp
builder => builder
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")
    .AllowCredentials()
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))  // Cache pre-flight for 10 minutes
```

**4. Method Restrictions**

Only allow methods your API actually uses:

```csharp
// Current configuration allows these methods
.WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")

// If you don't use DELETE, remove it
.WithMethods("PUT", "POST", "OPTIONS", "GET")
```

**5. Header Restrictions**

Consider restricting headers in production:

```csharp
// Instead of AllowAnyHeader()
.WithHeaders("Content-Type", "Authorization", "X-Correlation-Id", "Accept")
```

### Testing CORS Policies

**1. Browser Developer Tools**

Use browser DevTools to inspect CORS headers:
- Network tab shows OPTIONS pre-flight requests
- Check response headers for `Access-Control-*` headers
- Console shows CORS errors if policy is violated

**2. Manual Testing with curl**

Test pre-flight requests:

```bash
# Pre-flight request
curl -X OPTIONS http://localhost:55644/api/movies \
  -H "Origin: https://app.example.com" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type" \
  -v

# Actual request
curl -X POST http://localhost:55644/api/movies \
  -H "Origin: https://app.example.com" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Movie"}' \
  -v
```

**3. Integration Tests**

Create integration tests for CORS policies:

```csharp
[Fact]
public async Task Options_Request_Returns_Cors_Headers()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Options, "/api/movies");
    request.Headers.Add("Origin", "https://app.example.com");
    request.Headers.Add("Access-Control-Request-Method", "POST");

    // Act
    var response = await client.SendAsync(request);

    // Assert
    response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Origin");
    response.Headers.Should().Contain(h => h.Key == "Access-Control-Allow-Methods");
}
```

**4. Environment-Specific Testing**

Test CORS configuration in each environment:
- **Development**: Verify localhost origins work
- **Staging**: Test with staging frontend URLs
- **Production**: Ensure only production origins are allowed

**5. Common CORS Errors**

| Error | Cause | Solution |
|-------|-------|----------|
| "No 'Access-Control-Allow-Origin' header" | Origin not in allowed list | Add origin to configuration |
| "Credentials flag is true, but Access-Control-Allow-Credentials is not" | Missing `AllowCredentials()` | Add `.AllowCredentials()` to policy |
| "Method not allowed by Access-Control-Allow-Methods" | Method not in allowed list | Add method to `.WithMethods()` |
| "Request header not allowed by Access-Control-Allow-Headers" | Custom header not allowed | Add header to `.WithHeaders()` or use `.AllowAnyHeader()` |

### Migration Checklist

When moving from the current development configuration to production:

- [ ] Remove `AllowAnyOrigin()` from CORS policy
- [ ] Add `AllowedOrigins` section to `appsettings.json`
- [ ] Configure environment-specific origins in `appsettings.{Environment}.json`
- [ ] Add `.AllowCredentials()` to support Azure AD authentication
- [ ] Update `CorsConfiguration` method to read from configuration
- [ ] Consider restricting headers with `.WithHeaders()` instead of `.AllowAnyHeader()`
- [ ] Add `.WithExposedHeaders()` for custom response headers like `X-Correlation-Id`
- [ ] Test CORS policy with actual frontend application
- [ ] Document allowed origins in deployment documentation (see [/deployment/production_best_practices.md](/deployment/production_best_practices.md))
- [ ] Set up monitoring for CORS-related errors in Application Insights
- [ ] Configure `.SetPreflightMaxAge()` to optimize pre-flight request caching

### Related Configuration

CORS configuration works in conjunction with other security and middleware components:

**Authentication** (see [/security/authentication.md](/security/authentication.md)):
```csharp
// CORS must be configured before authentication
app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
```

**Middleware Pipeline** (see [/architecture/middleware_pipeline.md](/architecture/middleware_pipeline.md)):
```csharp
app.UseRouting();
app.UseCors("AllowSpecificOrigin");  // After routing, before authentication
app.UseAuthentication();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Health Checks**:
Health check endpoints may need to be excluded from CORS restrictions or configured separately depending on your monitoring setup.