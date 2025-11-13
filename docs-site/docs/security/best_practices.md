# Security Best Practices

This document outlines comprehensive security best practices for the BlackSlope.NET application. As a .NET 6.0 web API with Azure AD integration, SQL Server database, and Docker containerization support, proper security configuration is critical for protecting sensitive data and preventing unauthorized access.

## Configuration Security

### Secrets Management

The BlackSlope.NET application handles sensitive configuration data including database connection strings, Azure AD credentials, and Application Insights instrumentation keys. Never commit these values to source control.

#### User Secrets in Development

For local development, use the .NET User Secrets feature to store sensitive configuration:

```bash
# Initialize user secrets for the API project
dotnet user-secrets init --project src/BlackSlope.Api/BlackSlope.Api.csproj

# Set individual secrets
dotnet user-secrets set "BlackSlope.Api:MoviesConnectionString" "Server=localhost;Database=movies;Integrated Security=true;" --project src/BlackSlope.Api/BlackSlope.Api.csproj
dotnet user-secrets set "BlackSlope.Api:AzureAd:Tenant" "your-tenant-id" --project src/BlackSlope.Api/BlackSlope.Api.csproj
dotnet user-secrets set "BlackSlope.Api:AzureAd:Audience" "https://your-app.azurewebsites.net" --project src/BlackSlope.Api/BlackSlope.Api.csproj
dotnet user-secrets set "BlackSlope.Api:ApplicationInsights:InstrumentationKey" "your-instrumentation-key" --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

User secrets are stored outside the project directory at:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

**Important**: The `appsettings.json` file currently contains placeholder values that must be replaced:

```json
{
  "BlackSlope.Api": {
    "AzureAd": {
      "Tenant": "[tenant-id]",
      "Audience": "https://[host-name]"
    },
    "ApplicationInsights": {
      "InstrumentationKey": "[instrumentation-key]"
    }
  }
}
```

Note: The connection string is stored separately as `MoviesConnectionString` at the root level of the configuration, not nested under `BlackSlope.Api`.

#### Environment Variables in Production

For production deployments, use environment variables or Azure App Service configuration:

```bash
# Example environment variable configuration
export BlackSlope__Api__MoviesConnectionString="Server=prod-sql.database.windows.net;Database=movies;User Id=appuser;Password=<secure-password>;"
export BlackSlope__Api__AzureAd__Tenant="production-tenant-id"
export BlackSlope__Api__AzureAd__Audience="https://api.production.com"
```

**Note**: The double underscore (`__`) syntax is used for nested configuration in environment variables.

#### Key Vault Integration

For enterprise deployments, integrate Azure Key Vault using the `Azure.Identity` package (version 1.14.2) already included in the project:

```csharp
// Add to Program.cs or Startup.cs
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                if (!context.HostingEnvironment.IsDevelopment())
                {
                    var builtConfig = config.Build();
                    var keyVaultEndpoint = builtConfig["KeyVaultEndpoint"];
                    
                    if (!string.IsNullOrEmpty(keyVaultEndpoint))
                    {
                        var credential = new DefaultAzureCredential();
                        config.AddAzureKeyVault(
                            new Uri(keyVaultEndpoint),
                            credential);
                    }
                }
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
```

**Key Vault Naming Conventions**:
- Use hyphens instead of colons: `BlackSlope-Api-MoviesConnectionString`
- Azure Key Vault automatically converts hyphens to the nested configuration format

### Connection String Security

The current connection string in `appsettings.json` uses Integrated Security, which is suitable for development but requires modification for production:

**Development (Integrated Security)**:
```json
"MoviesConnectionString": "data source=.,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;"
```

**Production (SQL Authentication with Managed Identity)**:
```json
"MoviesConnectionString": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=movies;Authentication=Active Directory Managed Identity;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;"
```

**Production (SQL Authentication with Username/Password)**:
```json
"MoviesConnectionString": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=movies;User ID=app_user;Password=<from-keyvault>;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;"
```

**Critical Security Settings**:
- `Encrypt=True`: Enforces SSL/TLS encryption
- `TrustServerCertificate=False`: Validates server certificate
- Use least-privilege database accounts (not `sa` or admin accounts)

## API Security

### HTTPS Enforcement

The application includes HTTPS redirection in `Startup.cs`:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    // ... additional middleware
}
```

**Production Checklist**:
1. Ensure valid SSL/TLS certificates are installed
2. Configure certificate renewal automation
3. Use TLS 1.2 or higher (disable TLS 1.0/1.1)
4. Implement certificate pinning for mobile clients if applicable

### HSTS Configuration

HTTP Strict Transport Security (HSTS) is enabled for non-development environments. Enhance the configuration:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
        options.ExcludedHosts.Clear(); // Remove localhost exclusions in production
    });
    
    // ... other services
}
```

**HSTS Best Practices**:
- Start with a short `MaxAge` (e.g., 5 minutes) and gradually increase
- Test thoroughly before enabling `Preload`
- Submit to the HSTS preload list only after confirming stable HTTPS operation
- Document the HSTS configuration in deployment procedures

### Input Validation

The application uses FluentValidation (version 10.3.6) for comprehensive input validation. The `BlackSlopeValidator` class provides centralized validation with both synchronous and asynchronous methods:

```csharp
public class BlackSlopeValidator : IBlackSlopeValidator
{
    private readonly IValidatorAbstractFactory _validatorAbstractFactory;

    public void AssertValid<T>(T instance, params string[] ruleSetsToExecute)
    {
        var ruleSetValidatorSelector = new RulesetValidatorSelector(ruleSetsToExecute);
        var validationContext = new ValidationContext<T>(instance, null, ruleSetValidatorSelector);

        var validator = _validatorAbstractFactory.Resolve<T>();
        var validationResult = validator.Validate(validationContext);

        HandleValidationFailure(validationResult, instance);
    }

    public void AssertValid<T>(T instance)
    {
        var validator = _validatorAbstractFactory.Resolve<T>();
        var validationResult = validator.Validate(instance);

        HandleValidationFailure(validationResult, instance);
    }

    public async Task AssertValidAsync<T>(T instance)
    {
        var validator = _validatorAbstractFactory.Resolve<T>();
        var validationResult = await validator.ValidateAsync(instance);

        HandleValidationFailure(validationResult, instance);
    }

    private static void HandleValidationFailure(ValidationResult result, object instance)
    {
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(CreateApiError).ToList();
            throw new ApiException(ApiHttpStatusCode.BadRequest, instance, errors);
        }
    }

    private static ApiError CreateApiError(ValidationFailure validationFailure)
    {
        int errorCode;
        string message = null;
        if (validationFailure.CustomState is Enum validationFailureEnum)
        {
            errorCode = (int)validationFailure.CustomState;
            message = validationFailureEnum.GetDescription();
        }
        else
        {
            errorCode = (int)ApiHttpStatusCode.BadRequest;
        }

        return new ApiError
        {
            Code = errorCode,
            Message = string.IsNullOrEmpty(message)
                ? validationFailure.ErrorMessage
                : message,
        };
    }
}
```

**Key Features**:
- **Synchronous Validation**: `AssertValid<T>()` for non-async contexts
- **Async Validation**: `AssertValidAsync<T>()` for asynchronous operations
- **RuleSet Support**: `AssertValid<T>(instance, rulesets)` allows selective validation rule execution
- **Custom Error Codes**: Validators can use `CustomState` with enums to provide specific error codes and descriptions
- **Consistent Error Handling**: All validation failures throw `ApiException` with structured error details

**Validation Implementation Example** (from `MoviesController.cs`):

```csharp
[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };

    // Validate request model before processing
    await _blackSlopeValidator.AssertValidAsync(request);

    var movie = _mapper.Map<MovieDomainModel>(viewModel);
    var createdMovie = await _movieService.CreateMovieAsync(movie);
    var response = _mapper.Map<MovieViewModel>(createdMovie);

    return HandleCreatedResponse(response);
}
```

**Input Validation Best Practices**:

1. **Validate All Inputs**: Never trust client data
2. **Whitelist Approach**: Define what is allowed, not what is forbidden
3. **Type Safety**: Use strongly-typed models with data annotations
4. **Length Limits**: Enforce maximum lengths to prevent buffer overflow attacks
5. **Format Validation**: Use regular expressions for structured data (emails, phone numbers, etc.)

**Example Validator Implementation**:

```csharp
public class CreateMovieRequestValidator : AbstractValidator<CreateMovieRequest>
{
    public CreateMovieRequestValidator()
    {
        RuleFor(x => x.Movie.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-:,.'!?]+$").WithMessage("Title contains invalid characters");

        RuleFor(x => x.Movie.ReleaseYear)
            .InclusiveBetween(1888, DateTime.Now.Year + 5)
            .WithMessage("Release year must be between 1888 and 5 years in the future");

        RuleFor(x => x.Movie.Rating)
            .InclusiveBetween(0, 10)
            .When(x => x.Movie.Rating.HasValue)
            .WithMessage("Rating must be between 0 and 10");
    }
}
```

### SQL Injection Prevention

The application uses Entity Framework Core (version 6.0.1) with parameterized queries, which provides built-in SQL injection protection:

```csharp
// SAFE: Entity Framework Core uses parameterized queries
var movie = await _context.Movies
    .Where(m => m.Id == id)
    .FirstOrDefaultAsync();

// SAFE: Even with string interpolation in LINQ
var searchTerm = "Inception";
var movies = await _context.Movies
    .Where(m => m.Title.Contains(searchTerm))
    .ToListAsync();
```

**Dangerous Patterns to Avoid**:

```csharp
// UNSAFE: Raw SQL with string concatenation
var query = $"SELECT * FROM Movies WHERE Title = '{userInput}'";
var movies = _context.Movies.FromSqlRaw(query).ToList();

// SAFE: Parameterized raw SQL
var movies = _context.Movies
    .FromSqlRaw("SELECT * FROM Movies WHERE Title = {0}", userInput)
    .ToList();

// SAFE: Using FromSqlInterpolated
var movies = _context.Movies
    .FromSqlInterpolated($"SELECT * FROM Movies WHERE Title = {userInput}")
    .ToList();
```

**Additional SQL Security Measures**:

1. **Principle of Least Privilege**: Database user should only have necessary permissions
2. **Disable xp_cmdshell**: Ensure dangerous stored procedures are disabled
3. **Regular Security Audits**: Review database permissions and access logs
4. **Stored Procedures**: Consider using stored procedures for complex queries
5. **ORM Validation**: Keep Entity Framework Core updated to latest security patches

### Additional API Security Headers

Implement security headers middleware for defense-in-depth:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Add security headers
    app.Use(async (context, next) =>
    {
        // Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // Prevent MIME type sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // Enable XSS protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
        
        // Referrer Policy
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Permissions Policy
        context.Response.Headers.Add("Permissions-Policy", 
            "geolocation=(), microphone=(), camera=()");

        await next();
    });

    // ... rest of middleware pipeline
}
```

## Authentication & Authorization

### Token Security

The application uses Azure AD authentication with JWT tokens, leveraging multiple authentication libraries:

- **Azure.Identity** (1.14.2): Modern Azure AD authentication
- **Microsoft.IdentityModel.Clients.ActiveDirectory** (5.2.9): Legacy ADAL support
- **Microsoft.IdentityModel.JsonWebTokens** (7.7.1): JWT handling
- **System.IdentityModel.Tokens.Jwt** (7.7.1): JWT validation

**Current Configuration** (from `appsettings.json`):

```json
{
  "AzureAd": {
    "AadInstance": "https://login.microsoftonline.com/{0}",
    "Tenant": "[tenant-id]",
    "Audience": "https://[host-name]"
  }
}
```

**Enable Authentication** (currently disabled in `MoviesController.cs`):

```csharp
// TODO: enable this once authentication middleware has been configured
// [Authorize]
public class MoviesController : BaseController
{
    // ... controller implementation
}
```

**Production Authentication Configuration**:

```csharp
// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{Configuration["BlackSlope.Api:AzureAd:Tenant"]}";
            options.Audience = Configuration["BlackSlope.Api:AzureAd:Audience"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5), // Reduce default 5-minute clock skew if needed
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Log authentication failures
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Startup>>();
                    logger.LogWarning("Authentication failed: {Exception}", context.Exception);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    // Additional token validation logic
                    return Task.CompletedTask;
                }
            };
        });

    services.AddAuthorization(options =>
    {
        // Define authorization policies
        options.AddPolicy("RequireAdministratorRole", policy =>
            policy.RequireRole("Administrator"));
        
        options.AddPolicy("RequireMovieWriteScope", policy =>
            policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", "Movies.Write"));
    });
}
```

**Token Security Best Practices**:

1. **Short Token Lifetimes**: Configure access tokens to expire within 1 hour
2. **Refresh Token Rotation**: Implement refresh token rotation for long-lived sessions
3. **Token Revocation**: Implement token revocation for logout and security incidents
4. **Secure Token Storage**: Never store tokens in localStorage (use httpOnly cookies or memory)
5. **Token Validation**: Always validate issuer, audience, expiration, and signature
6. **Scope Validation**: Verify tokens contain required scopes for operations

**Example Authorization Usage**:

```csharp
[Authorize(Policy = "RequireMovieWriteScope")]
[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    // Only users with Movies.Write scope can access this endpoint
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ... implementation
}

[Authorize(Roles = "Administrator")]
[HttpDelete]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<DeletedMovieResponse>> Delete(int id)
{
    // Only administrators can delete movies
    // ... implementation
}
```

### Password Policies

While the application uses Azure AD for authentication (which handles password policies), if implementing custom authentication:

**Minimum Requirements**:
- Minimum length: 12 characters
- Complexity: Uppercase, lowercase, numbers, and special characters
- Password history: Prevent reuse of last 10 passwords
- Account lockout: 5 failed attempts, 30-minute lockout
- Password expiration: 90 days (or follow organizational policy)
- Multi-factor authentication: Required for all users

**Implementation Example** (if using ASP.NET Core Identity):

```csharp
services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
});
```

### Session Management

**Token-Based Session Management**:

```csharp
public class TokenService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public TokenService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<bool> IsTokenRevoked(string tokenId)
    {
        return await _cache.GetOrCreateAsync($"revoked_token_{tokenId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return Task.FromResult(false);
        });
    }

    public async Task RevokeToken(string tokenId)
    {
        _cache.Set($"revoked_token_{tokenId}", true, TimeSpan.FromHours(24));
    }
}
```

**Session Security Best Practices**:

1. **Idle Timeout**: Implement automatic logout after 15-30 minutes of inactivity
2. **Absolute Timeout**: Force re-authentication after 8-12 hours
3. **Concurrent Session Control**: Limit number of active sessions per user
4. **Session Invalidation**: Invalidate all sessions on password change
5. **Secure Session Storage**: Use distributed cache (Redis) for multi-instance deployments

## Production Checklist

### Enable Authentication

**Critical**: The authentication is currently disabled in controllers. Before deploying to production:

1. **Remove the TODO comment and enable `[Authorize]` attribute**:

```csharp
// Change from:
// TODO: enable this once authentication middleware has been configured
// [Authorize]
public class MoviesController : BaseController

// To:
[Authorize]
public class MoviesController : BaseController
```

2. **Configure Azure AD settings** in production configuration
3. **Test authentication** with valid and invalid tokens
4. **Verify authorization policies** are correctly applied

### Restrict CORS Origins

The current CORS configuration allows all origins, which is **insecure for production**:

```csharp
// CURRENT (INSECURE):
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

**Production CORS Configuration**:

```csharp
private static void CorsConfiguration(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigin", builder =>
        {
            builder
                .WithOrigins(
                    "https://app.production.com",
                    "https://admin.production.com"
                )
                .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id")
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });
}
```

**CORS Security Considerations**:

- Never use `AllowAnyOrigin()` with `AllowCredentials()`
- Specify exact origins (avoid wildcards in production)
- Limit allowed headers to only those required
- Restrict HTTP methods to those actually used
- Use environment-specific configuration for different deployment stages

### Remove Debug Endpoints

**Identify and Remove Debug Code**:

The `MoviesController` contains a sample error endpoint that should be removed:

```csharp
// REMOVE THIS BEFORE PRODUCTION:
[HttpGet]
[Route("SampleError")]
public object SampleError()
{
    throw new HandledException(ExceptionType.Security, 
        "This is an example security issue.", 
        System.Net.HttpStatusCode.RequestEntityTooLarge);
}
```

**Additional Debug Features to Disable**:

1. **Developer Exception Page**: Already configured correctly in `Startup.cs`
2. **Swagger UI**: Consider disabling in production or protecting with authentication
3. **Detailed Error Messages**: Ensure `UseDeveloperExceptionPage()` is only in development
4. **Test Endpoints**: Remove or protect endpoints like `httpExponentialBackoffTest`

**Swagger Security Configuration**:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Only enable Swagger in development or with authentication
    if (env.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API V1");
        });
    }
    else
    {
        // Option 1: Disable completely
        // (no Swagger code)

        // Option 2: Protect with authentication
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API V1");
        });
        // Ensure [Authorize] is on Swagger endpoints
    }
}
```

### Security Headers

Implement comprehensive security headers (see [Additional API Security Headers](#additional-api-security-headers) section above).

**Recommended Headers**:
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `X-XSS-Protection: 1; mode=block`
- `Content-Security-Policy: default-src 'self'`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: geolocation=(), microphone=(), camera=()`

### Rate Limiting

Implement rate limiting to prevent abuse and DDoS attacks:

```csharp
// Install: AspNetCoreRateLimit package
public void ConfigureServices(IServiceCollection services)
{
    // Load configuration
    services.AddOptions();
    services.AddMemoryCache();
    
    services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
    services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
    
    services.AddInMemoryRateLimiting();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseIpRateLimiting();
    // ... other middleware
}
```

**Rate Limiting Configuration** (appsettings.json):

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000
      },
      {
        "Endpoint": "POST:/api/v1/movies",
        "Period": "1m",
        "Limit": 10
      }
    ]
  }
}
```

### Health Check Security

The application includes health checks configured in `HealthCheckStartup`:

**Secure Health Check Configuration**:

```csharp
public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddHealthChecks()
        .AddSqlServer(
            connectionString: configuration["BlackSlope.Api:MoviesConnectionString"],
            name: "sql-server",
            tags: new[] { "db", "sql", "sqlserver" })
        .AddDbContextCheck<MoviesContext>(
            name: "ef-core-context",
            tags: new[] { "db", "ef-core" });
}

public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, HostConfig hostConfig)
{
    app.UseHealthChecks(hostConfig.HealthChecks.Endpoint, new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            // Don't expose detailed health information to unauthorized users
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
                await context.Response.WriteAsync(report.Status.ToString());
                return;
            }

            // Detailed response for authenticated users
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration
                })
            });

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(result);
        }
    });
}
```

### Additional Production Security Measures

1. **Logging and Monitoring**:
   - Enable Application Insights (instrumentation key in configuration)
   - Log all authentication failures
   - Monitor for suspicious patterns
   - Set up alerts for security events

2. **Database Security**:
   - Use connection string encryption
   - Enable SQL Server auditing
   - Implement row-level security if needed
   - Regular backup and disaster recovery testing

3. **Container Security** (Docker):
   - Use minimal base images
   - Scan images for vulnerabilities
   - Run containers as non-root user
   - Keep base images updated

4. **Dependency Management**:
   - Regularly update NuGet packages
   - Monitor for security advisories
   - Use tools like `dotnet list package --vulnerable`

5. **Code Analysis**:
   - StyleCop.Analyzers (1.1.118) is already configured
   - Microsoft.CodeAnalysis.NetAnalyzers (6.0.0) is enabled
   - Review and address all security warnings

## Related Documentation

For more detailed information on specific security topics, refer to:

- [Authentication Configuration](/security/authentication.md) - Detailed Azure AD setup and token management
- [Authorization Policies](/security/authorization.md) - Role-based and claims-based authorization
- [CORS Configuration](/security/cors.md) - Cross-origin resource sharing best practices
- [Production Deployment](/deployment/production_best_practices.md) - Complete production deployment checklist

## Security Incident Response

**In Case of Security Breach**:

1. **Immediate Actions**:
   - Revoke all active tokens
   - Rotate all secrets and connection strings
   - Enable additional logging
   - Notify security team and stakeholders

2. **Investigation**:
   - Review application logs
   - Check database audit logs
   - Analyze network traffic
   - Identify scope of breach

3. **Remediation**:
   - Patch vulnerabilities
   - Update security configurations
   - Force password resets if needed
   - Document lessons learned

4. **Prevention**:
   - Implement additional security controls
   - Update security policies
   - Conduct security training
   - Schedule regular security audits