# Production Best Practices

This guide provides comprehensive best practices for deploying and operating the BlackSlope.NET application in production environments. The application is built on .NET 6.0 with ASP.NET Core, Entity Framework Core, and SQL Server, designed for Azure cloud deployments.

## Pre-Deployment Checklist

### Enable Authentication and Authorization

The application includes Azure AD authentication infrastructure but requires explicit enablement before production deployment.

**Current State:**
```csharp
// TODO: enable this once authentication middleware has been configured
// [Authorize]
public class MoviesController : BaseController
```

**Required Actions:**

1. **Configure Azure AD Settings** in `appsettings.json`:
```json
{
  "BlackSlope.Api": {
    "AzureAd": {
      "AadInstance": "https://login.microsoftonline.com/{0}",
      "Tenant": "[tenant-id]",
      "Audience": "https://[host-name]"
    }
  }
}
```

2. **Enable Authorization Attributes** on all controllers:
```csharp
[Authorize]
public class MoviesController : BaseController
{
    // Controller implementation
}
```

3. **Verify Authentication Middleware** is properly configured in `Startup.cs`:
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ... other middleware
    app.UseAuthentication();
    // ... remaining middleware
}
```

**Note:** The `UseAuthorization()` middleware is not currently configured in Startup.cs and should be added after `UseAuthentication()` for proper authorization handling.

**Authentication Stack:**
- **Azure.Identity** (1.14.2): Modern Azure AD authentication
- **Microsoft.IdentityModel.Clients.ActiveDirectory** (5.2.9): Legacy ADAL support
- **System.IdentityModel.Tokens.Jwt** (7.7.1): JWT token validation

### Restrict CORS to Specific Origins

The current CORS configuration allows all origins, which is insecure for production.

**Current Configuration:**
```csharp
services.AddCors(options =>
{
    options.AddPolicy(
        "AllowSpecificOrigin",
        builder => builder.AllowAnyOrigin() // TODO: Replace with FE Service Host
            .AllowAnyHeader()
            .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE"));
});
```

**Production Configuration:**
```csharp
services.AddCors(options =>
{
    options.AddPolicy(
        "AllowSpecificOrigin",
        builder => builder
            .WithOrigins(
                "https://app.yourdomain.com",
                "https://admin.yourdomain.com"
            )
            .AllowAnyHeader()
            .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});
```

**Best Practices:**
- Use environment-specific configuration for allowed origins
- Enable credentials only when necessary
- Set appropriate preflight cache duration
- Restrict HTTP methods to only those required

### Remove Debug/Test Endpoints

The application contains test endpoints that must be removed before production deployment.

**Endpoints to Remove:**

1. **Sample Error Endpoint:**
```csharp
[HttpGet]
[Route("SampleError")]
public object SampleError()
{
    throw new HandledException(ExceptionType.Security, 
        "This is an example security issue.", 
        System.Net.HttpStatusCode.RequestEntityTooLarge);
}
```

2. **Polly Test Endpoint:**
```csharp
[HttpGet]
[Route("api/v1/movies/httpExponentialBackoffTest")]
public async Task<ActionResult> GetExponentialBackoff()
{
    await _movieService.GetExponentialBackoff();
    return HandleSuccessResponse(null);
}
```

**Verification Steps:**
- Review all controllers for test/debug routes
- Search codebase for `TODO` comments indicating temporary code
- Remove or secure Swagger UI endpoint (see below)

### Configure Production Logging

The application uses Serilog with configurable sinks. Production logging requires careful configuration to balance observability with performance and cost.

**Current Configuration:**
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

**Production Configuration:**
```json
{
  "BlackSlope.Api": {
    "Serilog": {
      "MinimumLevel": "warning",
      "FileName": "log.txt",
      "WriteToFile": "false",
      "WriteToAppInsights": "true",
      "WriteToConsole": "false"
    },
    "ApplicationInsights": {
      "InstrumentationKey": "[your-instrumentation-key]"
    }
  }
}
```

**Logging Levels:**
- **Verbose (0)**: Detailed diagnostic information (development only)
- **Debug (1)**: Internal system events (development only)
- **Information (2)**: General informational messages (staging)
- **Warning (3)**: Abnormal or unexpected events (production minimum)
- **Error (4)**: Errors and exceptions (always log)
- **Fatal (5)**: Critical failures (always log)

**Best Practices:**
- Use Application Insights for centralized logging in Azure
- Set minimum level to `warning` or `error` in production
- Disable console logging in production
- Implement structured logging with correlation IDs
- Configure log retention policies
- Sanitize sensitive data from logs (PII, credentials, tokens)

### Set Up Monitoring and Alerts

**Health Check Configuration:**

The application includes comprehensive health checks for monitoring system health:

```csharp
services.AddHealthChecks()
    .AddSqlServer(config.MoviesConnectionString, 
        name: "MOVIES.DB", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database })
    .AddCheck<MoviesHealthCheck>("MOVIES.API", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api });
```

**Health Check Endpoint:**
- `/health` - Overall system health (configured in appsettings.json)

**Health Check Response Format:**
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
      "duration": "00:00:00.0012345",
      "exception": null
    }
  ]
}
```

**Monitoring Recommendations:**
- Configure Application Insights for telemetry collection
- Set up alerts for health check failures
- Monitor response times and error rates
- Track database connection pool metrics
- Monitor memory and CPU utilization
- Set up availability tests for critical endpoints

**Key Metrics to Monitor:**
- Request duration (p50, p95, p99)
- Error rate (4xx, 5xx responses)
- Database query performance
- Connection pool exhaustion
- Memory leaks and GC pressure
- Circuit breaker state changes

See [/deployment/monitoring.md](/deployment/monitoring.md) for detailed monitoring configuration.

## Performance Optimization

### Response Caching

Implement response caching for read-heavy endpoints to reduce database load and improve response times.

**Configuration in Startup.cs:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddResponseCaching();
    services.AddMemoryCache();
    
    // Configure cache profiles
    services.AddControllers(options =>
    {
        options.CacheProfiles.Add("Default30",
            new CacheProfile
            {
                Duration = 30,
                Location = ResponseCacheLocation.Any,
                VaryByHeader = "Accept,Accept-Encoding"
            });
        
        options.CacheProfiles.Add("Never",
            new CacheProfile
            {
                Location = ResponseCacheLocation.None,
                NoStore = true
            });
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseResponseCaching();
    // ... other middleware
}
```

**Controller Implementation:**
```csharp
[HttpGet]
[Route("api/v1/movies")]
[ResponseCache(CacheProfileName = "Default30")]
public async Task<ActionResult<List<MovieViewModel>>> Get()
{
    var movies = await _movieService.GetAllMoviesAsync();
    var response = _mapper.Map<List<MovieViewModel>>(movies);
    return HandleSuccessResponse(response);
}

[HttpPost]
[Route("api/v1/movies")]
[ResponseCache(CacheProfileName = "Never")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    // Create operation - never cache
}
```

**In-Memory Caching with IMemoryCache:**
```csharp
public class MovieService : IMovieService
{
    private readonly IMemoryCache _cache;
    private readonly IMovieRepository _repository;
    
    public async Task<List<MovieDomainModel>> GetAllMoviesAsync()
    {
        return await _cache.GetOrCreateAsync("all_movies", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);
            return await _repository.GetAllAsync();
        });
    }
}
```

**Caching Best Practices:**
- Cache GET requests only
- Use appropriate cache durations based on data volatility
- Implement cache invalidation strategies
- Consider distributed caching (Redis) for multi-instance deployments
- Use `VaryByHeader` for content negotiation
- Monitor cache hit rates

### Database Connection Pooling

Entity Framework Core automatically manages connection pooling, but proper configuration is critical for production performance.

**Connection String Configuration:**
```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=.,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;"
  }
}
```

**Production Connection String (example):**
```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=sqlserver.database.windows.net;initial catalog=movies;User ID=appuser;Password=***;MultipleActiveResultSets=True;Min Pool Size=10;Max Pool Size=100;Connection Timeout=30;Pooling=true;"
  }
}
```

**Connection Pool Parameters:**
- **Min Pool Size**: Minimum connections maintained (default: 0, recommended: 10-20)
- **Max Pool Size**: Maximum connections allowed (default: 100, adjust based on load)
- **Connection Timeout**: Seconds to wait for connection (default: 15, recommended: 30)
- **Pooling**: Enable connection pooling (default: true)
- **MultipleActiveResultSets**: Enable MARS for parallel queries (use cautiously)

**DbContext Configuration:**
```csharp
services.AddDbContext<MoviesContext>(options =>
{
    options.UseSqlServer(
        configuration.GetConnectionString("MoviesConnectionString"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
            
            sqlOptions.CommandTimeout(30);
            sqlOptions.MigrationsAssembly("BlackSlope.Api");
        });
    
    // Disable tracking for read-only queries
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
```

**Best Practices:**
- Use `AsNoTracking()` for read-only queries
- Dispose DbContext properly (automatic with DI)
- Monitor connection pool exhaustion
- Adjust pool size based on concurrent request load
- Use async methods to avoid thread pool starvation

### HTTP Compression

Enable response compression to reduce bandwidth and improve client performance.

**Configuration:**
```csharp
using Microsoft.AspNetCore.ResponseCompression;

public void ConfigureServices(IServiceCollection services)
{
    services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            new[] { "application/json", "application/xml" });
    });
    
    services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });
    
    services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal;
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseResponseCompression();
    // ... other middleware (must be early in pipeline)
}
```

**Compression Considerations:**
- Brotli provides better compression but higher CPU usage
- Gzip is widely supported and faster
- Don't compress already-compressed content (images, videos)
- Balance compression level with CPU cost
- Test with realistic payloads

### CDN for Static Content

For Swagger UI and other static assets, configure CDN delivery in production.

**Swagger Configuration:**
```csharp
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API V1");
    c.RoutePrefix = "swagger";
    
    // Production: Serve Swagger UI assets from CDN
    if (!env.IsDevelopment())
    {
        c.InjectStylesheet("https://cdn.yourdomain.com/swagger-ui.css");
        c.InjectJavascript("https://cdn.yourdomain.com/swagger-ui-bundle.js");
    }
});
```

**Recommendations:**
- Use Azure CDN or Azure Front Door for static assets
- Configure appropriate cache headers
- Enable CDN compression
- Use versioned URLs for cache busting
- Consider disabling Swagger UI entirely in production

## Security Hardening

### HTTPS Enforcement

The application includes HTTPS redirection but requires additional hardening for production.

**Current Configuration:**
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (!env.IsDevelopment())
    {
        app.UseHsts();
    }
    
    app.UseHttpsRedirection();
}
```

**Enhanced Production Configuration:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
        options.ExcludedHosts.Clear();
    });
    
    services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = 443;
    });
}
```

**TLS Configuration:**
- Use TLS 1.2 or higher only
- Configure strong cipher suites
- Implement certificate pinning for mobile clients
- Use Azure Application Gateway or Front Door for TLS termination
- Monitor certificate expiration

See [/security/best_practices.md](/security/best_practices.md) for comprehensive security guidelines.

### Security Headers

Implement security headers to protect against common web vulnerabilities.

**Middleware Implementation:**
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.Use(async (context, next) =>
    {
        // Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // Prevent MIME sniffing
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
}
```

### Rate Limiting

Implement rate limiting to prevent abuse and ensure fair resource usage.

**AspNetCoreRateLimit Configuration:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMemoryCache();
    services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = 429;
        options.RealIpHeader = "X-Real-IP";
        options.GeneralRules = new List<RateLimitRule>
        {
            new RateLimitRule
            {
                Endpoint = "*",
                Period = "1m",
                Limit = 100
            },
            new RateLimitRule
            {
                Endpoint = "POST:/api/*",
                Period = "1m",
                Limit = 20
            }
        };
    });
    
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddInMemoryRateLimiting();
}
```

### Input Validation

The application uses FluentValidation for request validation. Ensure all endpoints validate input.

**Current Validation Pattern:**
```csharp
[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };
    
    // Validate request model
    await _blackSlopeValidator.AssertValidAsync(request);
    
    // Process request
}
```

**Validation Best Practices:**
- Validate all user input at API boundary
- Use strongly-typed validators
- Return detailed validation errors (400 Bad Request)
- Sanitize input to prevent injection attacks
- Validate file uploads (size, type, content)
- Implement business rule validation in service layer

**Example Validator:**
```csharp
public class CreateMovieRequestValidator : AbstractValidator<CreateMovieRequest>
{
    public CreateMovieRequestValidator()
    {
        RuleFor(x => x.Movie.Title)
            .NotEmpty()
            .MaximumLength(200)
            .Matches("^[a-zA-Z0-9 ]*$"); // Alphanumeric only
        
        RuleFor(x => x.Movie.ReleaseYear)
            .InclusiveBetween(1900, DateTime.Now.Year + 5);
        
        RuleFor(x => x.Movie.Rating)
            .InclusiveBetween(0, 10);
    }
}
```

### Secret Management

**Current Configuration Issues:**
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

**Production Secret Management:**

1. **Azure Key Vault Integration:**
```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            if (!context.HostingEnvironment.IsDevelopment())
            {
                var builtConfig = config.Build();
                var keyVaultEndpoint = builtConfig["KeyVault:Endpoint"];
                
                config.AddAzureKeyVault(
                    new Uri(keyVaultEndpoint),
                    new DefaultAzureCredential());
            }
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

2. **User Secrets for Development:**
```bash
dotnet user-secrets init --project src/BlackSlope.Api
dotnet user-secrets set "AzureAd:Tenant" "your-tenant-id"
dotnet user-secrets set "ApplicationInsights:InstrumentationKey" "your-key"
```

3. **Environment Variables for Containers:**
```dockerfile
# Never hardcode secrets in Dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
# Secrets injected at runtime via Kubernetes secrets or Azure App Service settings
```

**Secret Management Best Practices:**
- Never commit secrets to source control
- Use Azure Key Vault for production secrets
- Rotate secrets regularly
- Use managed identities for Azure resources
- Implement secret scanning in CI/CD pipeline
- Audit secret access

## Reliability

### Health Checks

The application implements comprehensive health checks using the built-in ASP.NET Core health check framework.

**Health Check Implementation:**
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

**Custom Health Check Example:**
```csharp
public class MoviesHealthCheck : IHealthCheck
{
    private readonly IMovieService _movieService;
    
    public MoviesHealthCheck(IMovieService movieService)
    {
        _movieService = movieService;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform lightweight check
            var canConnect = await _movieService.CanConnectAsync();
            
            if (canConnect)
            {
                return HealthCheckResult.Healthy("Movies API is healthy");
            }
            
            return HealthCheckResult.Degraded("Movies API is degraded");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Movies API is unhealthy", 
                ex);
        }
    }
}
```

**Health Check Endpoint:**
- `/health` - Aggregate health status (configured in appsettings.json)

**Integration with Orchestrators:**
- **Kubernetes**: Configure liveness and readiness probes
- **Azure App Service**: Configure health check path
- **Load Balancers**: Configure health probe endpoints

See [/deployment/kubernetes.md](/deployment/kubernetes.md) for Kubernetes-specific configuration.

### Graceful Shutdown

Implement graceful shutdown to ensure in-flight requests complete before termination.

**Configuration:**
```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
            webBuilder.UseShutdownTimeout(TimeSpan.FromSeconds(30));
        });
```

**Shutdown Handling:**
```csharp
public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
{
    lifetime.ApplicationStopping.Register(() =>
    {
        // Stop accepting new requests
        // Allow existing requests to complete
        // Close database connections
        // Flush logs
    });
    
    lifetime.ApplicationStopped.Register(() =>
    {
        // Final cleanup
    });
}
```

**Best Practices:**
- Set appropriate shutdown timeout (30-60 seconds)
- Implement health check degradation during shutdown
- Drain connections before termination
- Log shutdown events
- Handle SIGTERM signals properly in containers

### Retry Policies

The application uses Polly for implementing resilience patterns, including retry policies.

**Current Implementation:**
```csharp
services.AddHttpClient("movies", (provider, client) => 
    provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
```

**Enhanced Retry Configuration:**
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpClient("movies")
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
}

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                // Log retry attempt
            });
}
```

**Entity Framework Retry Configuration:**
```csharp
services.AddDbContext<MoviesContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
});
```

**Retry Best Practices:**
- Use exponential backoff to avoid overwhelming services
- Implement jitter to prevent thundering herd
- Set maximum retry attempts (typically 3-5)
- Only retry transient failures
- Log retry attempts for monitoring
- Consider idempotency for retry safety

See [/features/resilience.md](/features/resilience.md) for detailed resilience patterns.

### Circuit Breakers

Implement circuit breakers to prevent cascading failures.

**Circuit Breaker Configuration:**
```csharp
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
            {
                // Log circuit breaker opened
            },
            onReset: () =>
            {
                // Log circuit breaker reset
            },
            onHalfOpen: () =>
            {
                // Log circuit breaker half-open
            });
}
```

**Circuit Breaker States:**
- **Closed**: Normal operation, requests flow through
- **Open**: Failures exceeded threshold, requests fail immediately
- **Half-Open**: Testing if service recovered, limited requests allowed

**Monitoring Circuit Breakers:**
- Track circuit breaker state changes
- Alert on circuit breaker opens
- Monitor failure rates
- Implement fallback strategies
- Consider bulkhead isolation for critical services

## Operational Excellence

### Automated Deployments

**CI/CD Pipeline Recommendations:**

1. **Build Stage:**
```yaml
# Azure DevOps example
- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    projects: 'src/BlackSlope.Api/BlackSlope.Api.csproj'
    arguments: '--configuration Release'

- task: DotNetCoreCLI@2
  displayName: 'Run Tests'
  inputs:
    command: 'test'
    projects: 'src/**/*Tests.csproj'
    arguments: '--configuration Release --collect:"XPlat Code Coverage"'
```

2. **Docker Build:**
```yaml
- task: Docker@2
  displayName: 'Build Docker Image'
  inputs:
    command: 'build'
    dockerfile: 'src/Dockerfile'
    tags: |
      $(Build.BuildId)
      latest
```

3. **Security Scanning:**
```yaml
- task: WhiteSource@21
  displayName: 'Security Scan'
  inputs:
    cwd: '$(System.DefaultWorkingDirectory)'

- task: SonarCloudAnalyze@1
  displayName: 'Code Quality Analysis'
```

4. **Deployment:**
```yaml
- task: AzureWebApp@1
  displayName: 'Deploy to Azure App Service'
  inputs:
    azureSubscription: 'Production'
    appName: 'blackslope-api-prod'
    package: '$(Build.ArtifactStagingDirectory)/**/*.zip'
```

**Deployment Best Practices:**
- Automate all deployment steps
- Implement approval gates for production
- Run smoke tests post-deployment
- Maintain deployment logs
- Version all artifacts
- Use infrastructure as code (ARM templates, Terraform)

### Blue-Green Deployments

Implement blue-green deployments to minimize downtime and enable quick rollbacks.

**Azure App Service Slots:**
```yaml
- task: AzureAppServiceManage@0
  displayName: 'Swap Slots'
  inputs:
    azureSubscription: 'Production'
    Action: 'Swap Slots'
    WebAppName: 'blackslope-api-prod'
    ResourceGroupName: 'blackslope-rg'
    SourceSlot: 'staging'
    SwapWithProduction: true
```

**Kubernetes Blue-Green:**
```yaml
apiVersion: v1
kind: Service
metadata:
  name: blackslope-api
spec:
  selector:
    app: blackslope-api
    version: blue  # Switch to 'green' for deployment
  ports:
  - port: 80
    targetPort: 80
```

**Deployment Process:**
1. Deploy new version to staging/green environment
2. Run smoke tests and validation
3. Switch traffic to new version
4. Monitor for issues
5. Keep old version running for quick rollback
6. Decommission old version after validation period

### Rollback Procedures

**Automated Rollback Triggers:**
- Health check failures exceeding threshold
- Error rate spike (>5% increase)
- Response time degradation (>50% increase)
- Manual intervention

**Rollback Steps:**

1. **Azure App Service:**
```bash
az webapp deployment slot swap \
  --resource-group blackslope-rg \
  --name blackslope-api-prod \
  --slot staging \
  --target-slot production
```

2. **Kubernetes:**
```bash
kubectl rollout undo deployment/blackslope-api
kubectl rollout status deployment/blackslope-api
```

3. **Docker:**
```bash
docker service update --image blackslope.api:previous-version blackslope-api
```

**Rollback Best Practices:**
- Maintain previous version artifacts
- Test rollback procedures regularly
- Document rollback steps
- Implement automated rollback for critical failures
- Communicate rollback to stakeholders
- Investigate root cause post-rollback

### Incident Response

**Incident Response Plan:**

1. **Detection:**
   - Monitor health check endpoints
   - Track error rates and response times
   - Configure alerts for anomalies
   - Review Application Insights telemetry

2. **Triage:**
   - Assess impact and severity
   - Identify affected components
   - Review recent deployments
   - Check external dependencies

3. **Mitigation:**
   - Implement immediate fixes (rollback, scaling)
   - Enable circuit breakers if needed
   - Communicate status to stakeholders
   - Document actions taken

4. **Resolution:**
   - Deploy permanent fix
   - Verify resolution
   - Update monitoring and alerts
   - Conduct post-mortem

**Incident Severity Levels:**
- **P0 (Critical)**: Complete service outage
- **P1 (High)**: Major functionality impaired
- **P2 (Medium)**: Minor functionality impaired
- **P3 (Low)**: Cosmetic issues, no user impact

### Documentation Maintenance

**Documentation Requirements:**

1. **API Documentation:**
   - Keep Swagger/OpenAPI specs current
   - Document all endpoints, parameters, responses
   - Include authentication requirements
   - Provide example requests/responses

2. **Architecture Documentation:**
   - Maintain architecture diagrams
   - Document design decisions
   - Update dependency information
   - Track technical debt

3. **Operational Documentation:**
   - Deployment procedures
   - Troubleshooting guides
   - Monitoring and alerting setup
   - Incident response procedures

4. **Code Documentation:**
   - XML documentation comments for public APIs
   - README files for each project
   - Inline comments for complex logic
   - Architecture Decision Records (ADRs)

**Documentation Tools:**
- **Swagger UI**: API documentation (configured at `/swagger`)
- **StyleCop**: Enforces documentation standards
- **Markdown**: README and operational docs
- **Wiki**: Centralized knowledge base

**Current Swagger Configuration:**
```csharp
services.AddSwagger(HostConfig.Swagger);

// In production, consider restricting access
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API V1");
    c.RoutePrefix = "swagger";
});
```

**Documentation Best Practices:**
- Review and update documentation with each release
- Include documentation updates in pull request reviews
- Use automated tools to generate documentation where possible
- Maintain version history
- Make documentation easily accessible to team
- Include runbooks for common operational tasks

---

## Additional Resources

- [Security Best Practices](/security/best_practices.md)
- [Monitoring and Observability](/deployment/monitoring.md)
- [Kubernetes Deployment Guide](/deployment/kubernetes.md)
- [Resilience Patterns](/features/resilience.md)

## Checklist Summary

Use this checklist before each production deployment:

- [ ] Authentication enabled on all controllers
- [ ] CORS restricted to specific origins
- [ ] Debug/test endpoints removed
- [ ] Production logging configured
- [ ] Application Insights enabled
- [ ] Health checks configured and tested
- [ ] HTTPS enforcement enabled
- [ ] Security headers implemented
- [ ] Rate limiting configured
- [ ] Input validation on all endpoints
- [ ] Secrets moved to Key Vault
- [ ] Connection pooling optimized
- [ ] Response caching implemented
- [ ] Retry policies configured
- [ ] Circuit breakers implemented
- [ ] Graceful shutdown configured
- [ ] Deployment automation tested
- [ ] Rollback procedures documented
- [ ] Monitoring and alerts configured
- [ ] Documentation updated