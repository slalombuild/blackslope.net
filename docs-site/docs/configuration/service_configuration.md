# Service Configuration

## Overview

The BlackSlope API implements a strongly-typed configuration system using the **IOptions pattern** from ASP.NET Core. This approach provides compile-time type safety, validation at startup, and seamless integration with the dependency injection container. Configuration is organized into logical sections that map to specific application concerns such as hosting, authentication, logging, and health monitoring.

The configuration system leverages .NET 6.0's built-in configuration providers, primarily loading settings from JSON files (appsettings.json) with support for environment-specific overrides and Azure Key Vault integration for sensitive data.

## Configuration Classes

### Strongly-Typed Configuration

The application uses Plain Old CLR Objects (POCOs) to represent configuration sections. This approach offers several advantages over accessing configuration values directly through `IConfiguration`:

- **Type Safety**: Compile-time checking prevents typos and type mismatches
- **IntelliSense Support**: IDE autocomplete for configuration properties
- **Testability**: Easy to mock and unit test
- **Refactoring**: Rename operations work across the codebase
- **Validation**: Built-in support for data annotations and custom validation

### IOptions Pattern

The IOptions pattern is implemented through three primary interfaces:

| Interface | Use Case | Lifecycle |
|-----------|----------|-----------|
| `IOptions<T>` | Static configuration that doesn't change during runtime | Singleton |
| `IOptionsSnapshot<T>` | Configuration that may change between requests | Scoped |
| `IOptionsMonitor<T>` | Real-time configuration updates with change notifications | Singleton |

For most scenarios in this application, `IOptions<T>` is sufficient as configuration is loaded at startup and remains static throughout the application lifecycle.

### Configuration Binding

Configuration binding is the process of mapping configuration sections from JSON (or other providers) to strongly-typed C# objects. This is accomplished using the `IConfiguration.GetSection()` method combined with the `Configure<T>()` extension method during service registration:

```csharp
// Typical binding pattern in Startup.cs or Program.cs
services.Configure<HostConfig>(configuration.GetSection("Host"));
services.Configure<AzureAdConfig>(configuration.GetSection("Host:AzureAd"));
```

The binding process:
1. Reads the specified configuration section
2. Creates an instance of the target type
3. Maps JSON properties to C# properties (case-insensitive by default)
4. Registers the configured instance in the DI container
5. Makes it available for injection via `IOptions<T>`

## Configuration Models

### HostConfig

The `HostConfig` class serves as the root configuration model, aggregating all major configuration sections:

```csharp
namespace BlackSlope.Api.Common.Configuration
{
    public class HostConfig
    {
        public string BaseUrl { get; set; }

        public SwaggerConfig Swagger { get; set; }

        public AzureAdConfig AzureAd { get; set; }

        public SerilogConfig Serilog { get; set; }

        public ApplicationInsightsConfig ApplicationInsights { get; set; }

        public HealthChecksConfig HealthChecks { get; set; }
    }
}
```

**Properties:**

- **BaseUrl**: The base URL for the API, used for generating absolute URLs in responses and for CORS configuration
- **Swagger**: Configuration for API documentation (see [SwaggerConfig](#swaggerconfig))
- **AzureAd**: Azure Active Directory authentication settings (see [AzureAdConfig](#azureadconfig))
- **Serilog**: Structured logging configuration (see [SerilogConfig](#serilogconfig))
- **ApplicationInsights**: Azure Application Insights telemetry settings
- **HealthChecks**: Health check endpoint configuration (see [HealthChecksConfig](#healthchecksconfig))

**Corresponding JSON Structure:**

```json
{
  "Host": {
    "BaseUrl": "https://api.example.com",
    "Swagger": { ... },
    "AzureAd": { ... },
    "Serilog": { ... },
    "ApplicationInsights": { ... },
    "HealthChecks": { ... }
  }
}
```

**Usage Example:**

```csharp
public class SomeService
{
    private readonly HostConfig _config;

    public SomeService(IOptions<HostConfig> config)
    {
        _config = config.Value;
    }

    public string GetApiBaseUrl()
    {
        return _config.BaseUrl;
    }
}
```

### SwaggerConfig

The `SwaggerConfig` class configures the Swagger/OpenAPI documentation UI (Swashbuckle.AspNetCore.SwaggerUI 6.3.0):

```csharp
namespace BlackSlope.Api.Common.Configuration
{
    public class SwaggerConfig
    {
        public string Version { get; set; }

        public string ApplicationName { get; set; }

        public string XmlFile { get; set; }
    }
}
```

**Properties:**

- **Version**: API version string (e.g., "v1", "v2.0") displayed in Swagger UI
- **ApplicationName**: Human-readable application name shown in the Swagger documentation header
- **XmlFile**: Path to the XML documentation file generated from code comments, enabling rich API documentation

**Corresponding JSON Structure:**

```json
{
  "Host": {
    "Swagger": {
      "Version": "v1",
      "ApplicationName": "BlackSlope API",
      "XmlFile": "BlackSlope.Api.xml"
    }
  }
}
```

**Integration with Swagger:**

```csharp
// In Startup.cs ConfigureServices
services.AddSwaggerGen(c =>
{
    var swaggerConfig = Configuration.GetSection("Host:Swagger").Get<SwaggerConfig>();
    
    c.SwaggerDoc(swaggerConfig.Version, new OpenApiInfo
    {
        Title = swaggerConfig.ApplicationName,
        Version = swaggerConfig.Version
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, swaggerConfig.XmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});
```

**Best Practices:**

- Enable XML documentation generation in the project file: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- Use XML comments (`///`) on controllers and models to populate Swagger descriptions
- Version your API explicitly to support backward compatibility

### AzureAdConfig

The `AzureAdConfig` class contains Azure Active Directory authentication parameters for JWT bearer token validation:

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

**Properties:**

- **AadInstance**: The Azure AD authority URL (typically `https://login.microsoftonline.com/`)
- **Tenant**: The Azure AD tenant ID (GUID) or domain name (e.g., `contoso.onmicrosoft.com`)
- **Audience**: The expected audience claim in JWT tokens, typically the Application ID URI or Client ID of the API

**Corresponding JSON Structure:**

```json
{
  "Host": {
    "AzureAd": {
      "AadInstance": "https://login.microsoftonline.com/",
      "Tenant": "12345678-1234-1234-1234-123456789abc",
      "Audience": "api://blackslope-api"
    }
  }
}
```

**Security Considerations:**

- **Never commit tenant IDs or audience values for production** to source control
- Use Azure Key Vault or User Secrets for sensitive configuration
- The `Audience` value must match the `aud` claim in incoming JWT tokens
- Validate that `AadInstance` uses HTTPS to prevent token interception

**Integration with Authentication Middleware:**

```csharp
// In Startup.cs ConfigureServices
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var azureAdConfig = Configuration.GetSection("Host:AzureAd").Get<AzureAdConfig>();
        
        options.Authority = $"{azureAdConfig.AadInstance}{azureAdConfig.Tenant}";
        options.Audience = azureAdConfig.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });
```

For more details on authentication implementation, see [Authentication Documentation](/security/authentication.md).

### HealthChecksConfig

The `HealthChecksConfig` class defines the endpoint for health check monitoring:

```csharp
namespace BlackSlope.Api.Common.Configuration
{
    public class HealthChecksConfig
    {
       public string Endpoint { get; set; }
    }
}
```

**Properties:**

- **Endpoint**: The URL path where health checks are exposed (e.g., `/health`, `/api/health`)

**Corresponding JSON Structure:**

```json
{
  "Host": {
    "HealthChecks": {
      "Endpoint": "/health"
    }
  }
}
```

**Health Check Implementation:**

The application uses `AspNetCore.HealthChecks.SqlServer` (5.0.3) and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (6.0.1) to monitor database connectivity:

```csharp
// In Startup.cs ConfigureServices
services.AddHealthChecks()
    .AddSqlServer(
        connectionString: Configuration.GetConnectionString("DefaultConnection"),
        name: "sql-server",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddDbContextCheck<ApplicationDbContext>(
        name: "ef-core-context",
        tags: new[] { "db", "ef-core" });

// In Startup.cs Configure
var healthChecksConfig = Configuration.GetSection("Host:HealthChecks").Get<HealthChecksConfig>();
app.UseHealthChecks(healthChecksConfig.Endpoint, new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

**Monitoring Best Practices:**

- Configure health checks to run on a separate port or path to avoid authentication requirements
- Include checks for all critical dependencies (database, external APIs, caching)
- Set appropriate timeouts to prevent health check endpoints from hanging
- Use health check results in orchestrators (Kubernetes, Azure App Service) for automatic recovery

### SerilogConfig

The `SerilogConfig` class configures structured logging using Serilog:

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

**Properties:**

- **MinimumLevel**: The minimum log level to capture (e.g., "Debug", "Information", "Warning", "Error", "Fatal")
- **FileName**: Path and filename pattern for file-based logging (supports date-based rolling)
- **WriteToConsole**: Enable/disable console output (useful for containerized environments)
- **WriteToFile**: Enable/disable file-based logging
- **WriteToAppInsights**: Enable/disable Azure Application Insights integration

**Corresponding JSON Structure:**

```json
{
  "Host": {
    "Serilog": {
      "MinimumLevel": "Information",
      "FileName": "logs/blackslope-api-.txt",
      "WriteToConsole": true,
      "WriteToFile": true,
      "WriteToAppInsights": true
    }
  }
}
```

**Serilog Configuration Example:**

```csharp
// In Program.cs or Startup.cs
var serilogConfig = Configuration.GetSection("Host:Serilog").Get<SerilogConfig>();

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Is(Enum.Parse<LogEventLevel>(serilogConfig.MinimumLevel))
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName();

if (serilogConfig.WriteToConsole)
{
    loggerConfiguration.WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
}

if (serilogConfig.WriteToFile)
{
    loggerConfiguration.WriteTo.File(
        path: serilogConfig.FileName,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
}

if (serilogConfig.WriteToAppInsights)
{
    var appInsightsKey = Configuration["Host:ApplicationInsights:InstrumentationKey"];
    loggerConfiguration.WriteTo.ApplicationInsights(appInsightsKey, TelemetryConverter.Traces);
}

Log.Logger = loggerConfiguration.CreateLogger();
```

**Log Level Guidelines:**

| Level | Use Case | Production |
|-------|----------|------------|
| Debug | Detailed diagnostic information | ❌ Disabled |
| Information | General application flow | ✅ Enabled |
| Warning | Unexpected but recoverable issues | ✅ Enabled |
| Error | Errors and exceptions | ✅ Enabled |
| Fatal | Critical failures requiring immediate attention | ✅ Enabled |

## Loading Configuration

### GetSection and Binding

Configuration loading follows a consistent pattern throughout the application:

```csharp
// Method 1: Direct binding with Get<T>()
var hostConfig = Configuration.GetSection("Host").Get<HostConfig>();

// Method 2: Binding with IOptions pattern (recommended)
services.Configure<HostConfig>(Configuration.GetSection("Host"));

// Method 3: Nested section binding
services.Configure<AzureAdConfig>(Configuration.GetSection("Host:AzureAd"));
```

**Configuration Hierarchy:**

The configuration system supports multiple sources with a defined precedence order (last wins):

1. appsettings.json (base configuration)
2. appsettings.{Environment}.json (environment-specific overrides)
3. User Secrets (development only)
4. Environment Variables
5. Command-line arguments
6. Azure Key Vault (production)

**Example Configuration Loading:**

```csharp
// In Program.cs
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            var env = context.HostingEnvironment;
            
            config
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

            if (env.IsDevelopment())
            {
                config.AddUserSecrets<Program>();
            }

            config.AddEnvironmentVariables();
            config.AddCommandLine(args);

            if (env.IsProduction())
            {
                var builtConfig = config.Build();
                var keyVaultUrl = builtConfig["KeyVault:Url"];
                if (!string.IsNullOrEmpty(keyVaultUrl))
                {
                    config.AddAzureKeyVault(
                        new Uri(keyVaultUrl),
                        new DefaultAzureCredential());
                }
            }
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

### Configuration Validation

Validation ensures that configuration is correct at application startup, preventing runtime errors:

**Method 1: Data Annotations**

```csharp
using System.ComponentModel.DataAnnotations;

public class AzureAdConfig
{
    [Required(ErrorMessage = "AadInstance is required")]
    [Url(ErrorMessage = "AadInstance must be a valid URL")]
    public string AadInstance { get; set; }

    [Required(ErrorMessage = "Tenant is required")]
    [RegularExpression(@"^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$|^[\w\-\.]+\.onmicrosoft\.com$",
        ErrorMessage = "Tenant must be a valid GUID or domain")]
    public string Tenant { get; set; }

    [Required(ErrorMessage = "Audience is required")]
    public string Audience { get; set; }
}

// In Startup.cs
services.AddOptions<AzureAdConfig>()
    .Bind(Configuration.GetSection("Host:AzureAd"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

**Method 2: FluentValidation**

The application includes FluentValidation.DependencyInjectionExtensions (10.3.6) for more complex validation scenarios:

```csharp
using FluentValidation;

public class AzureAdConfigValidator : AbstractValidator<AzureAdConfig>
{
    public AzureAdConfigValidator()
    {
        RuleFor(x => x.AadInstance)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("AadInstance must be a valid absolute URL");

        RuleFor(x => x.Tenant)
            .NotEmpty()
            .Must(BeValidTenant)
            .WithMessage("Tenant must be a valid GUID or Azure AD domain");

        RuleFor(x => x.Audience)
            .NotEmpty()
            .MinimumLength(5)
            .WithMessage("Audience must be specified");
    }

    private bool BeValidTenant(string tenant)
    {
        return Guid.TryParse(tenant, out _) || tenant.EndsWith(".onmicrosoft.com");
    }
}

// In Startup.cs
services.AddOptions<AzureAdConfig>()
    .Bind(Configuration.GetSection("Host:AzureAd"))
    .Validate(config =>
    {
        var validator = new AzureAdConfigValidator();
        var result = validator.Validate(config);
        return result.IsValid;
    })
    .ValidateOnStart();
```

**Method 3: Custom Validation**

```csharp
services.AddOptions<HostConfig>()
    .Bind(Configuration.GetSection("Host"))
    .Validate(config =>
    {
        if (string.IsNullOrEmpty(config.BaseUrl))
            return false;

        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var uri))
            return false;

        if (config.Swagger == null || string.IsNullOrEmpty(config.Swagger.Version))
            return false;

        return true;
    }, "HostConfig validation failed")
    .ValidateOnStart();
```

### Dependency Injection

Configuration objects are registered in the DI container during application startup:

```csharp
// In Startup.cs ConfigureServices
public void ConfigureServices(IServiceCollection services)
{
    // Register root configuration
    services.Configure<HostConfig>(Configuration.GetSection("Host"));

    // Register nested configurations for direct injection
    services.Configure<SwaggerConfig>(Configuration.GetSection("Host:Swagger"));
    services.Configure<AzureAdConfig>(Configuration.GetSection("Host:AzureAd"));
    services.Configure<SerilogConfig>(Configuration.GetSection("Host:Serilog"));
    services.Configure<HealthChecksConfig>(Configuration.GetSection("Host:HealthChecks"));

    // Alternative: Register as singleton for direct access (not recommended)
    // services.AddSingleton(Configuration.GetSection("Host").Get<HostConfig>());
}
```

**Consuming Configuration in Services:**

```csharp
// Example: Service consuming multiple configuration sections
public class ApiClientService
{
    private readonly string _baseUrl;
    private readonly AzureAdConfig _azureAdConfig;
    private readonly ILogger<ApiClientService> _logger;

    public ApiClientService(
        IOptions<HostConfig> hostConfig,
        IOptions<AzureAdConfig> azureAdConfig,
        ILogger<ApiClientService> logger)
    {
        _baseUrl = hostConfig.Value.BaseUrl;
        _azureAdConfig = azureAdConfig.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var authority = $"{_azureAdConfig.AadInstance}{_azureAdConfig.Tenant}";
        _logger.LogInformation("Acquiring token from {Authority}", authority);
        
        // Token acquisition logic using Azure.Identity (1.14.2)
        var credential = new DefaultAzureCredential();
        var tokenRequestContext = new TokenRequestContext(new[] { $"{_azureAdConfig.Audience}/.default" });
        var token = await credential.GetTokenAsync(tokenRequestContext);
        
        return token.Token;
    }
}
```

For more information on dependency injection patterns, see [Dependency Injection Documentation](/architecture/dependency_injection.md).

## Best Practices

### Type Safety

**✅ DO:**

```csharp
// Use strongly-typed configuration
public class MyService
{
    private readonly AzureAdConfig _config;

    public MyService(IOptions<AzureAdConfig> config)
    {
        _config = config.Value;
    }

    public string GetAuthority()
    {
        return $"{_config.AadInstance}{_config.Tenant}"; // IntelliSense support
    }
}
```

**❌ DON'T:**

```csharp
// Avoid accessing IConfiguration directly in services
public class MyService
{
    private readonly IConfiguration _configuration;

    public MyService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetAuthority()
    {
        // Prone to typos, no compile-time checking
        return $"{_configuration["Host:AzureAd:AadInstance"]}{_configuration["Host:AzureAd:Tenant"]}";
    }
}
```

### Validation at Startup

**✅ DO:**

```csharp
// Validate configuration at startup to fail fast
services.AddOptions<AzureAdConfig>()
    .Bind(Configuration.GetSection("Host:AzureAd"))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fails at startup if invalid

// Or use custom validation
services.AddOptions<HostConfig>()
    .Bind(Configuration.GetSection("Host"))
    .Validate(config => !string.IsNullOrEmpty(config.BaseUrl), "BaseUrl is required")
    .ValidateOnStart();
```

**❌ DON'T:**

```csharp
// Don't defer validation to runtime
services.Configure<AzureAdConfig>(Configuration.GetSection("Host:AzureAd"));
// No validation - errors only discovered when config is first used
```

### Secret Management

**✅ DO:**

```csharp
// Use User Secrets for local development
// dotnet user-secrets set "Host:AzureAd:Tenant" "your-tenant-id"

// Use Azure Key Vault for production
if (env.IsProduction())
{
    var keyVaultUrl = Configuration["KeyVault:Url"];
    config.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

// Use environment variables for containerized deployments
// docker run -e Host__AzureAd__Tenant=your-tenant-id myapp
```

**❌ DON'T:**

```csharp
// Never commit secrets to appsettings.json
{
  "Host": {
    "AzureAd": {
      "Tenant": "12345678-1234-1234-1234-123456789abc", // ❌ Don't do this!
      "ClientSecret": "super-secret-value" // ❌ Never commit secrets!
    }
  }
}
```

### Configuration Organization

**✅ DO:**

- Group related settings into logical configuration classes
- Use nested configuration objects for hierarchical settings
- Keep configuration classes in a dedicated `Configuration` namespace
- Document each property with XML comments

```csharp
/// <summary>
/// Configuration for Azure Active Directory authentication.
/// </summary>
public class AzureAdConfig
{
    /// <summary>
    /// Gets or sets the Azure AD authority URL (e.g., https://login.microsoftonline.com/).
    /// </summary>
    public string AadInstance { get; set; }

    /// <summary>
    /// Gets or sets the Azure AD tenant ID or domain name.
    /// </summary>
    public string Tenant { get; set; }

    /// <summary>
    /// Gets or sets the expected audience claim in JWT tokens.
    /// </summary>
    public string Audience { get; set; }
}
```

**❌ DON'T:**

- Mix configuration concerns in a single flat class
- Use magic strings throughout the codebase
- Create configuration classes with unclear or ambiguous names

### Environment-Specific Configuration

**✅ DO:**

```json
// appsettings.json (base configuration)
{
  "Host": {
    "BaseUrl": "https://localhost:5001",
    "Serilog": {
      "MinimumLevel": "Debug",
      "WriteToConsole": true,
      "WriteToFile": true
    }
  }
}

// appsettings.Production.json (production overrides)
{
  "Host": {
    "BaseUrl": "https://api.blackslope.com",
    "Serilog": {
      "MinimumLevel": "Information",
      "WriteToConsole": false,
      "WriteToFile": true,
      "WriteToAppInsights": true
    }
  }
}
```

### Configuration Caching

The IOptions pattern automatically caches configuration values. However, be aware of the different interfaces:

```csharp
// IOptions<T> - Singleton, cached for application lifetime
public class SingletonService
{
    public SingletonService(IOptions<HostConfig> config)
    {
        // config.Value is cached and never reloads
    }
}

// IOptionsSnapshot<T> - Scoped, reloads per request
public class ScopedService
{
    public ScopedService(IOptionsSnapshot<HostConfig> config)
    {
        // config.Value reloads for each HTTP request
    }
}

// IOptionsMonitor<T> - Singleton with change notifications
public class MonitoredService
{
    public MonitoredService(IOptionsMonitor<HostConfig> config)
    {
        // config.CurrentValue reflects latest configuration
        config.OnChange(newConfig =>
        {
            // React to configuration changes
        });
    }
}
```

### Testing Configuration

**Unit Testing:**

```csharp
[Fact]
public void Service_Should_Use_Configuration_Correctly()
{
    // Arrange
    var config = new AzureAdConfig
    {
        AadInstance = "https://login.microsoftonline.com/",
        Tenant = "test-tenant",
        Audience = "api://test-api"
    };
    var options = Options.Create(config);
    var service = new MyService(options);

    // Act
    var authority = service.GetAuthority();

    // Assert
    Assert.Equal("https://login.microsoftonline.com/test-tenant", authority);
}
```

**Integration Testing:**

```csharp
public class ConfigurationIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;

    public ConfigurationIntegrationTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Configuration_Should_Load_Successfully()
    {
        // Arrange & Act
        var client = _factory.CreateClient();
        var config = _factory.Services.GetRequiredService<IOptions<HostConfig>>();

        // Assert
        Assert.NotNull(config.Value);
        Assert.NotEmpty(config.Value.BaseUrl);
        Assert.NotNull(config.Value.AzureAd);
    }
}
```

## Common Pitfalls and Troubleshooting

### Issue: Configuration Not Loading

**Symptoms:** `NullReferenceException` when accessing configuration properties

**Causes:**
- JSON property names don't match C# property names
- Configuration section path is incorrect
- JSON file is not set to "Copy to Output Directory"

**Solution:**

```csharp
// Verify section path
var section = Configuration.GetSection("Host:AzureAd");
if (!section.Exists())
{
    throw new InvalidOperationException("Configuration section 'Host:AzureAd' not found");
}

// Enable detailed logging
services.Configure<AzureAdConfig>(config =>
{
    Configuration.GetSection("Host:AzureAd").Bind(config);
    Console.WriteLine($"Loaded AzureAd config: Tenant={config.Tenant}");
});
```

### Issue: Validation Fails Silently

**Symptoms:** Invalid configuration doesn't throw exceptions

**Cause:** Missing `.ValidateOnStart()` call

**Solution:**

```csharp
// Always add ValidateOnStart() for startup validation
services.AddOptions<AzureAdConfig>()
    .Bind(Configuration.GetSection("Host:AzureAd"))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // This is critical!
```

### Issue: Configuration Changes Not Reflected

**Symptoms:** Changes to appsettings.json don't take effect

**Causes:**
- Using `IOptions<T>` which caches values
- `reloadOnChange: false` in configuration setup
- Application not restarted after configuration change

**Solution:**

```csharp
// Use IOptionsSnapshot<T> for per-request reload
public class MyController : ControllerBase
{
    private readonly IOptionsSnapshot<HostConfig> _config;

    public MyController(IOptionsSnapshot<HostConfig> config)
    {
        _config = config; // Reloads per request
    }
}

// Or use IOptionsMonitor<T> for real-time updates
public class MyService
{
    private readonly IOptionsMonitor<HostConfig> _config;

    public MyService(IOptionsMonitor<HostConfig> config)
    {
        _config = config;
        _config.OnChange(newConfig =>
        {
            // Handle configuration change
        });
    }
}
```

### Issue: Environment Variables Not Overriding

**Symptoms:** Environment variables don't override appsettings.json values

**Cause:** Incorrect environment variable naming convention

**Solution:**

```bash
# Use double underscore (__) for nested properties
export Host__AzureAd__Tenant="production-tenant-id"
export Host__Serilog__MinimumLevel="Information"

# In Docker
docker run -e Host__AzureAd__Tenant=prod-tenant myapp

# In Kubernetes
env:
  - name: Host__AzureAd__Tenant
    value: "prod-tenant"
```

## Related Documentation

- [Application Settings](/configuration/application_settings.md) - Detailed configuration file structure and examples
- [Dependency Injection](/architecture/dependency_injection.md) - Service registration and lifetime management
- [Authentication](/security/authentication.md) - Azure AD authentication implementation details

## Summary

The BlackSlope API's configuration system provides a robust, type-safe approach to managing application settings. By leveraging the IOptions pattern, strongly-typed configuration classes, and comprehensive validation, the system ensures that configuration errors are caught early and that the application behaves predictably across different environments. Following the best practices outlined in this document will help maintain a secure, maintainable, and testable configuration infrastructure.