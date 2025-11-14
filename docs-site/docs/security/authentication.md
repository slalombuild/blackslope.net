# Authentication with Azure AD

This document provides comprehensive technical guidance for implementing and maintaining Azure Active Directory (Azure AD) authentication in the BlackSlope application. The system supports both web API authentication using JWT bearer tokens and console application authentication using service principal credentials.

## Azure AD Integration

The BlackSlope application integrates with Azure Active Directory to provide enterprise-grade authentication and authorization capabilities. The implementation supports both modern Azure AD authentication patterns (via `Azure.Identity`) and legacy ADAL-based authentication for backward compatibility.

### Architecture Overview

The authentication architecture consists of three primary components:

1. **Web API Authentication**: JWT bearer token validation for incoming HTTP requests
2. **Console Application Authentication**: Service principal-based authentication for background processes
3. **Configuration Management**: Centralized Azure AD settings across application components

### Key Dependencies

The authentication system leverages the following NuGet packages:

| Package | Version | Purpose |
|---------|---------|---------|
| Azure.Identity | 1.14.2 | Modern Azure AD authentication with managed identity support |
| Microsoft.IdentityModel.Clients.ActiveDirectory | 5.2.9 | Legacy ADAL authentication for console applications |
| Microsoft.IdentityModel.JsonWebTokens | 7.7.1 | JWT token handling and parsing |
| System.IdentityModel.Tokens.Jwt | 7.7.1 | JWT token validation and generation |

### Authentication Middleware Registration

The web API registers Azure AD authentication in the `Startup.cs` class using the `AddAzureAd` extension method:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvcService();
    ApplicationConfiguration(services);
    CorsConfiguration(services);

    services.AddSwagger(HostConfig.Swagger);
    services.AddAzureAd(HostConfig.AzureAd);  // Azure AD authentication registration
    services.AddAutoMapper(GetAssembliesToScanForMapperProfiles());
    // ... additional service registrations
}
```

The authentication middleware is activated in the request pipeline:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... middleware configuration
    
    app.UseRouting();
    app.UseCors("AllowSpecificOrigin");

    app.UseAuthentication();  // Enable authentication middleware

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

**Important**: The `UseAuthentication()` middleware must be called after `UseRouting()` and `UseCors()` but before `UseEndpoints()` to ensure proper request processing order.

## Configuration

### AzureAdConfig Setup

The Azure AD configuration is encapsulated in a strongly-typed configuration class:

```csharp
namespace BlackSlope.Api.Common.Configuration
{
    public class AzureAdConfig
    {
        public string AadInstance { get; set; }
        public string Tenant { get; set; }
        public string Audience { get; set; }
    }
}
```

**Configuration Properties**:

- **AadInstance**: The Azure AD authority URL template. Uses `{0}` as a placeholder for the tenant ID.
- **Tenant**: The Azure AD tenant ID (GUID) or domain name (e.g., `contoso.onmicrosoft.com`)
- **Audience**: The application ID URI that identifies your API as a valid token recipient

### appsettings Configuration

Azure AD settings are configured in `appsettings.json` under the application-specific section:

```json
{
  "BlackSlope.Api": {
    "BaseUrl": "http://localhost:55644",
    "AzureAd": {
      "AadInstance": "https://login.microsoftonline.com/{0}",
      "Tenant": "[tenant-id]",
      "Audience": "https://[host-name]"
    }
  }
}
```

**Configuration Guidelines**:

- Replace `[tenant-id]` with your Azure AD tenant ID (GUID format recommended for production)
- Replace `[host-name]` with your API's application ID URI registered in Azure AD
- For multi-tenant applications, use `common` or `organizations` instead of a specific tenant ID
- Store sensitive configuration values in Azure Key Vault or User Secrets for local development

### Service Registration

The configuration is loaded and registered as a singleton during application startup:

```csharp
private void ApplicationConfiguration(IServiceCollection services)
{
    services.AddSingleton(_ => _configuration);
    services.AddSingleton(_configuration
        .GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<HostConfig>());

    var serviceProvider = services.BuildServiceProvider();
    HostConfig = serviceProvider.GetService<HostConfig>();
}
```

This approach ensures that:
- Configuration is loaded once at startup
- The `HostConfig` (which contains `AzureAdConfig`) is available throughout the application lifecycle
- Configuration changes require an application restart to take effect

### Environment-Specific Configuration

For different environments (Development, Staging, Production), use environment-specific configuration files:

- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

Example for development:

```json
{
  "BlackSlope.Api": {
    "AzureAd": {
      "Tenant": "development-tenant-id",
      "Audience": "https://localhost:55644"
    }
  }
}
```

## Authentication Flow

### Web API Token Validation Flow

The authentication flow for incoming API requests follows this sequence:

1. **Client Request**: Client includes JWT bearer token in the `Authorization` header
   ```
   Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
   ```

2. **Middleware Interception**: The authentication middleware intercepts the request

3. **Token Extraction**: The middleware extracts the token from the Authorization header

4. **Token Validation**: The token is validated against Azure AD:
   - Signature verification using Azure AD public keys
   - Issuer validation (must match Azure AD tenant)
   - Audience validation (must match configured Audience)
   - Expiration time validation
   - Not-before time validation

5. **Claims Population**: Upon successful validation, claims are extracted and populated into `HttpContext.User`

6. **Authorization**: Controller actions can access user claims for authorization decisions

### Token Acquisition

Clients must acquire tokens from Azure AD before calling the API. The typical OAuth 2.0 flows supported include:

- **Client Credentials Flow**: For service-to-service authentication (used by console app)
- **Authorization Code Flow**: For user-delegated access (web/mobile apps)
- **On-Behalf-Of Flow**: For middle-tier services calling downstream APIs

### Token Validation

The `AddAzureAd` extension method configures JWT bearer authentication with the following validation parameters:

```csharp
// Typical implementation in AddAzureAd extension method
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = string.Format(azureAdConfig.AadInstance, azureAdConfig.Tenant);
        options.Audience = azureAdConfig.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5) // Allows 5 minutes clock skew
        };
    });
```

**Validation Parameters**:

- **ValidateIssuer**: Ensures the token was issued by the expected Azure AD tenant
- **ValidateAudience**: Verifies the token is intended for this API
- **ValidateLifetime**: Checks token expiration and not-before times
- **ValidateIssuerSigningKey**: Validates the token signature using Azure AD public keys
- **ClockSkew**: Tolerance for time differences between servers (default: 5 minutes)

### Claims Extraction

After successful authentication, claims are available through the `HttpContext.User` property:

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [HttpGet]
    public IActionResult GetUserInfo()
    {
        // Extract common claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        
        // Extract Azure AD specific claims
        var tenantId = User.FindFirst("tid")?.Value;
        var objectId = User.FindFirst("oid")?.Value;
        var appId = User.FindFirst("appid")?.Value;
        
        // Extract roles for authorization
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        
        return Ok(new { userId, userEmail, userName, tenantId, roles });
    }
}
```

**Common Azure AD Claims**:

| Claim Type | Description |
|------------|-------------|
| `oid` | Object ID - unique identifier for the user in Azure AD |
| `tid` | Tenant ID - identifies the Azure AD tenant |
| `appid` | Application ID - identifies the client application |
| `roles` | Application roles assigned to the user/application |
| `scp` | Scopes - delegated permissions granted to the application |
| `name` | Display name of the user |
| `upn` | User Principal Name |

## Console App Authentication

The console application (`RenameUtility`) uses the legacy ADAL library to authenticate as a service principal and acquire access tokens for calling the web API.

### AuthenticationToken Helper

The `AuthenticationToken` class provides a command-line interface for acquiring tokens:

```csharp
using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace BlackSlope.Hosts.ConsoleApp
{
    public static class AuthenticationToken
    {
        public static async Task GetAuthTokenAsync()
        {
            Console.WriteLine("Welcome to the Blackslope Console");
            Console.WriteLine("");

            // Collect authentication parameters from user input
            Console.Write("ClientId: ");
            var clientId = Console.ReadLine().Trim();
            Console.WriteLine();
            
            Console.Write("ClientSecret: ");
            var key = Console.ReadLine().Trim();
            Console.WriteLine();
            
            Console.Write("TenantId: ");
            var tenantId = Console.ReadLine().Trim();
            Console.WriteLine();
            
            Console.Write("App URI: ");
            var appIdUri = Console.ReadLine().Trim();
            Console.WriteLine();
            
            // Acquire token using service principal credentials
            var response = await GetTokenAsynch(clientId, key, tenantId, appIdUri);

            var token = response.AccessToken;
            Console.WriteLine($"Bearer {token}");

            Console.WriteLine("");
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        public static async Task<AuthenticationResult> GetTokenAsynch(
            string clientId, 
            string key, 
            string tenantId, 
            string appIdUniformResourceIdentifier)
        {
            var aadInstanceUrl = "https://login.microsoftonline.com/{0}";
            var authority = string.Format(
                CultureInfo.InvariantCulture, 
                aadInstanceUrl, 
                tenantId);

            var authContext = new AuthenticationContext(authority);
            var clientCredential = new ClientCredential(clientId, key);

            var response = await authContext.AcquireTokenAsync(
                appIdUniformResourceIdentifier, 
                clientCredential);

            return response;
        }
    }
}
```

### Service Principal Authentication

The console application uses the **Client Credentials Flow** (OAuth 2.0) to authenticate:

1. **Service Principal Registration**: Register an application in Azure AD and create a client secret
2. **Permission Grant**: Grant the service principal appropriate API permissions
3. **Token Acquisition**: Use client ID and secret to acquire an access token
4. **API Invocation**: Include the token in API requests

**Authentication Parameters**:

- **ClientId**: The application (client) ID from Azure AD app registration
- **ClientSecret**: The client secret generated for the application
- **TenantId**: The Azure AD tenant ID where the application is registered
- **App URI**: The application ID URI of the target API (must match the API's Audience configuration)

### Token Management

The `AuthenticationResult` object returned by ADAL contains:

```csharp
public class AuthenticationResult
{
    public string AccessToken { get; }        // JWT token for API authorization
    public DateTimeOffset ExpiresOn { get; }  // Token expiration timestamp
    public string TokenType { get; }          // Always "Bearer" for JWT tokens
    public string IdToken { get; }            // OpenID Connect ID token (if requested)
    // ... additional properties
}
```

**Token Usage Example**:

```csharp
var authResult = await AuthenticationToken.GetTokenAsynch(
    clientId, 
    clientSecret, 
    tenantId, 
    apiUri);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

var response = await httpClient.GetAsync("https://api.blackslope.com/api/movies");
```

### Migration to Azure.Identity

**Important**: The console application currently uses the legacy ADAL library (`Microsoft.IdentityModel.Clients.ActiveDirectory`), which is deprecated. Consider migrating to the modern `Azure.Identity` library:

```csharp
using Azure.Identity;
using Azure.Core;

public static async Task<string> GetTokenModernAsync(
    string clientId, 
    string clientSecret, 
    string tenantId, 
    string scope)
{
    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    var tokenRequestContext = new TokenRequestContext(new[] { $"{scope}/.default" });
    var token = await credential.GetTokenAsync(tokenRequestContext);
    
    return token.Token;
}
```

**Benefits of Migration**:
- Active support and security updates
- Better integration with Azure services
- Support for managed identities
- Improved token caching
- Consistent API across Azure SDKs

## Best Practices

### Secure Token Storage

**Never store tokens in plain text or commit them to source control.** Follow these guidelines:

1. **User Secrets for Development**:
   ```bash
   dotnet user-secrets set "AzureAd:ClientSecret" "your-secret-here"
   ```

2. **Azure Key Vault for Production**:
   ```csharp
   var keyVaultUrl = "https://your-keyvault.vault.azure.net/";
   var credential = new DefaultAzureCredential();
   
   configurationBuilder.AddAzureKeyVault(
       new Uri(keyVaultUrl), 
       credential);
   ```

3. **Environment Variables**:
   ```json
   {
     "AzureAd": {
       "ClientSecret": "#{AZURE_AD_CLIENT_SECRET}#"
     }
   }
   ```

4. **Managed Identity** (recommended for Azure-hosted applications):
   ```csharp
   var credential = new DefaultAzureCredential();
   // No secrets needed - uses managed identity
   ```

### Token Expiration Handling

Implement proper token expiration handling to avoid authentication failures:

```csharp
public class TokenCache
{
    private string _cachedToken;
    private DateTimeOffset _tokenExpiration;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public async Task<string> GetTokenAsync(
        Func<Task<AuthenticationResult>> acquireTokenFunc)
    {
        await _lock.WaitAsync();
        try
        {
            // Check if token is expired or will expire in next 5 minutes
            if (string.IsNullOrEmpty(_cachedToken) || 
                _tokenExpiration <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                var result = await acquireTokenFunc();
                _cachedToken = result.AccessToken;
                _tokenExpiration = result.ExpiresOn;
            }

            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

**Token Lifetime Considerations**:
- Default Azure AD token lifetime: 1 hour
- Implement token refresh 5-10 minutes before expiration
- Handle token refresh failures gracefully with retry logic
- Use the Polly library (already included) for resilient token acquisition

### Multi-Tenant Support

To support multiple Azure AD tenants, modify the configuration:

```json
{
  "AzureAd": {
    "AadInstance": "https://login.microsoftonline.com/{0}",
    "Tenant": "common",  // Accepts tokens from any Azure AD tenant
    "Audience": "https://[host-name]"
  }
}
```

**Tenant Validation Options**:

| Tenant Value | Behavior |
|--------------|----------|
| `{tenant-id}` | Single-tenant - only accepts tokens from specified tenant |
| `common` | Multi-tenant - accepts tokens from any Azure AD tenant and Microsoft accounts |
| `organizations` | Multi-tenant - accepts tokens from any Azure AD tenant (excludes personal Microsoft accounts) |
| `consumers` | Accepts only personal Microsoft accounts |

**Multi-Tenant Validation**:

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.microsoftonline.com/common";
        options.Audience = azureAdConfig.Audience;
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            IssuerValidator = (issuer, token, parameters) =>
            {
                // Custom issuer validation for multi-tenant scenarios
                var validIssuers = new[]
                {
                    "https://sts.windows.net/{tenant-id-1}/",
                    "https://sts.windows.net/{tenant-id-2}/"
                };
                
                if (!validIssuers.Contains(issuer))
                {
                    throw new SecurityTokenInvalidIssuerException(
                        $"Issuer '{issuer}' is not valid");
                }
                
                return issuer;
            }
        };
    });
```

### Security Hardening

1. **Enable HTTPS Only**:
   ```csharp
   services.AddHttpsRedirection(options =>
   {
       options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
       options.HttpsPort = 443;
   });
   ```

2. **Validate Token Signing Keys**:
   ```csharp
   options.TokenValidationParameters = new TokenValidationParameters
   {
       ValidateIssuerSigningKey = true,
       RequireSignedTokens = true,
       // ... other parameters
   };
   ```

3. **Implement Rate Limiting** for token endpoints to prevent brute-force attacks

4. **Log Authentication Failures** for security monitoring:
   ```csharp
   options.Events = new JwtBearerEvents
   {
       OnAuthenticationFailed = context =>
       {
           _logger.LogWarning(
               "Authentication failed: {Error}", 
               context.Exception.Message);
           return Task.CompletedTask;
       }
   };
   ```

5. **Use Correlation IDs** (already implemented via `CorrelationIdMiddleware`) for request tracing

### Performance Optimization

1. **Token Caching**: ADAL and Azure.Identity automatically cache tokens - leverage this behavior
2. **Connection Pooling**: Reuse `HttpClient` instances (already configured via `IHttpClientFactory`)
3. **Async/Await**: All authentication operations use async patterns for non-blocking I/O
4. **Health Checks**: Monitor authentication service availability using the configured health check endpoints

### Error Handling

Implement comprehensive error handling for authentication failures:

```csharp
try
{
    var token = await GetTokenAsync();
}
catch (AdalServiceException ex) when (ex.ErrorCode == "invalid_client")
{
    _logger.LogError("Invalid client credentials");
    // Handle invalid credentials
}
catch (AdalServiceException ex) when (ex.ErrorCode == "unauthorized_client")
{
    _logger.LogError("Client not authorized for this resource");
    // Handle authorization issues
}
catch (AdalServiceException ex)
{
    _logger.LogError(ex, "Azure AD authentication failed: {ErrorCode}", ex.ErrorCode);
    // Handle other ADAL errors
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected authentication error");
    // Handle unexpected errors
}
```

## Related Documentation

- [Authorization](/security/authorization.md) - Role-based and policy-based authorization implementation
- [Service Configuration](/configuration/service_configuration.md) - Comprehensive configuration management
- [CORS Configuration](/security/cors.md) - Cross-Origin Resource Sharing setup and security considerations

## Troubleshooting

### Common Issues

**Issue**: `401 Unauthorized` responses despite valid token

**Solutions**:
- Verify the `Audience` configuration matches the token's `aud` claim
- Check that the token hasn't expired
- Ensure the API's Azure AD app registration has the correct application ID URI
- Verify the token issuer matches the configured tenant

**Issue**: `IDX10205: Issuer validation failed`

**Solutions**:
- Confirm the `Tenant` configuration matches the token's `iss` claim
- For multi-tenant apps, use `common` or implement custom issuer validation
- Check for tenant ID mismatches between environments

**Issue**: Console app fails to acquire token

**Solutions**:
- Verify the service principal has been granted API permissions
- Ensure admin consent has been granted for application permissions
- Check that the client secret hasn't expired
- Confirm the App URI matches the API's application ID URI exactly

**Issue**: Token validation performance degradation

**Solutions**:
- Verify Azure AD metadata endpoint is accessible
- Check network connectivity to `login.microsoftonline.com`
- Review token validation caching configuration
- Monitor health check endpoints for authentication service availability