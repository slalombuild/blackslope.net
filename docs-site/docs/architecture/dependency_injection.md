# Dependency Injection

## Overview

The BlackSlope API leverages ASP.NET Core's built-in dependency injection (DI) container to manage service lifetimes, promote loose coupling, and facilitate testability. The application follows a modular approach to service registration, utilizing custom extension methods to organize and encapsulate registration logic for different functional areas.

The DI configuration is primarily orchestrated in the `Startup.ConfigureServices` method, with specialized extension methods defined in the `Microsoft.Extensions.DependencyInjection` namespace to maintain clean separation of concerns.

## DI Container Configuration

### Service Registration Patterns

The application employs several patterns for service registration:

#### 1. Extension Method Pattern

All service registrations are encapsulated in extension methods that extend `IServiceCollection`. This pattern provides:
- **Modularity**: Each functional area manages its own dependencies
- **Reusability**: Extension methods can be shared across projects
- **Discoverability**: IntelliSense support for service registration
- **Encapsulation**: Implementation details hidden from the startup class

```csharp
public static class MovieServiceServiceCollectionExtensions
{
    public static IServiceCollection AddMovieService(this IServiceCollection services)
    {
        services.TryAddTransient<IMovieService, MovieService>();
        return services;
    }
}
```

#### 2. TryAdd Pattern

The codebase consistently uses `TryAdd*` methods (`TryAddTransient`, `TryAddScoped`, `TryAddSingleton`) instead of direct `Add*` methods. This pattern prevents duplicate service registrations and allows for service replacement in testing scenarios:

```csharp
// Will only register if IMovieService hasn't been registered previously
services.TryAddTransient<IMovieService, MovieService>();
```

**Benefits:**
- Prevents accidental duplicate registrations
- Enables test projects to register mock implementations first
- Supports plugin architectures where services may be conditionally registered

#### 3. Fluent Interface Pattern

All extension methods return `IServiceCollection`, enabling method chaining:

```csharp
services
    .AddMovieService()
    .AddMovieRepository(_configuration)
    .AddFakeApiRepository();
```

### Service Lifetimes

The application uses all three standard ASP.NET Core service lifetimes:

| Lifetime | Usage | Examples in Codebase |
|----------|-------|---------------------|
| **Transient** | Created each time requested; suitable for lightweight, stateless services | `IMovieService`, `IFileSystem`, `IVersionService`, `IHttpClientDecorator` |
| **Scoped** | Created once per HTTP request; ideal for request-specific operations | `IMovieRepository`, `MovieContext` (DbContext) |
| **Singleton** | Created once for application lifetime; used for stateless, thread-safe services | `IMapper` (AutoMapper), `IMovieRepositoryConfiguration`, `IConfiguration`, `HostConfig` |

**Key Considerations:**

- **DbContext as Scoped**: Entity Framework Core contexts are registered as scoped to ensure each HTTP request gets its own database connection and change tracker:
  ```csharp
  services.AddDbContext<MovieContext>(options => 
      options.UseSqlServer(config.MoviesConnectionString));
  ```

- **Configuration as Singleton**: Configuration objects are immutable and thread-safe, making them ideal singleton candidates:
  ```csharp
  services.TryAddSingleton<IMovieRepositoryConfiguration>(config);
  ```

- **Services as Transient**: Business logic services are typically transient to avoid state management issues and support concurrent request handling.

## Core Services

### Swagger Configuration

Swagger/OpenAPI documentation is configured with security definitions and health endpoint documentation:

```csharp
public static IServiceCollection AddSwagger(
    this IServiceCollection services, 
    SwaggerConfig swaggerConfig) =>
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(swaggerConfig.Version, 
            new OpenApiInfo { Title = swaggerConfig.ApplicationName, Version = swaggerConfig.Version });
        options.DocumentFilter<DocumentFilterAddHealth>();
        AddSecurityDefinition(options);
        AddSecurityRequirement(options);
        SetDocumentPath(swaggerConfig, options);
    });
```

**Key Configuration Elements:**

1. **OpenAPI Document**: Configures title and version from `SwaggerConfig`
2. **Health Endpoint Filter**: Adds health check endpoints to Swagger documentation via `DocumentFilterAddHealth`
3. **Security Definition**: Configures OAuth2/JWT bearer token authentication
4. **XML Comments**: Includes XML documentation comments from the compiled assembly

**Security Configuration:**
```csharp
private static void AddSecurityDefinition(SwaggerGenOptions options) =>
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Please insert JWT with Bearer into field",
    });

private static void AddSecurityRequirement(SwaggerGenOptions options) =>
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
            },
            new[] { "readAccess", "writeAccess" }
        },
    });
```

**XML Documentation Path:**
```csharp
private static void SetDocumentPath(SwaggerConfig swaggerConfig, SwaggerGenOptions options)
{
    var xmlFile = swaggerConfig.XmlFile;
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
}
```

### MVC and JSON Serialization Configuration

The MVC service is configured with custom JSON serialization settings:

```csharp
public static IMvcBuilder AddMvcService(this IServiceCollection services) =>
    services.AddMvc()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new VersionJsonConverter());
    });
```

**JSON Serialization Settings:**

| Setting | Configuration | Purpose |
|---------|--------------|----------|
| **Null Handling** | `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` | Omits null properties from JSON responses |
| **Case Sensitivity** | `PropertyNameCaseInsensitive = true` | Allows case-insensitive property name matching during deserialization |
| **Enum Serialization** | `JsonStringEnumConverter` | Serializes enums as strings instead of integers |
| **Version Serialization** | `VersionJsonConverter` | Custom converter for `System.Version` objects |

**Benefits:**
- Cleaner API responses (no null clutter)
- More flexible client implementations (case-insensitive parsing)
- Human-readable enum values in JSON
- Proper version string formatting

### Azure AD Authentication Configuration

Azure Active Directory authentication is configured using JWT bearer tokens:

```csharp
public static AuthenticationBuilder AddAzureAd(
    this IServiceCollection services, 
    AzureAdConfig azureAdConfig) =>
    services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = string.Format(
            System.Globalization.CultureInfo.InvariantCulture, 
            azureAdConfig.AadInstance, 
            azureAdConfig.Tenant);
        options.Audience = azureAdConfig.Audience;
    });
```

**Configuration Structure:**

The method expects an `AzureAdConfig` object with the following properties:
- `AadInstance`: Azure AD instance URL template (e.g., `"https://login.microsoftonline.com/{0}"`)
- `Tenant`: Azure AD tenant ID or domain name
- `Audience`: Expected audience claim (typically the application/client ID)

**Example Configuration:**
```json
{
  "AzureAd": {
    "AadInstance": "https://login.microsoftonline.com/{0}",
    "Tenant": "contoso.onmicrosoft.com",
    "Audience": "api://12345678-1234-1234-1234-123456789012"
  }
}
```

**Authentication Flow:**
1. Client acquires JWT token from Azure AD
2. Client includes token in `Authorization: Bearer {token}` header
3. ASP.NET Core validates token against configured authority and audience
4. User claims are populated from validated token

### AutoMapper Configuration

AutoMapper is configured as a singleton service with profile scanning across specified assemblies:

```csharp
public static IServiceCollection AddAutoMapper(
    this IServiceCollection services, 
    IEnumerable<Assembly> assemblyProfilesToScan)
{
    services.TryAddSingleton(GenerateMapperConfiguration(assemblyProfilesToScan));
    return services;
}

private static IMapper GenerateMapperConfiguration(IEnumerable<Assembly> assemblyProfilesToScan)
{
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddMaps(assemblyProfilesToScan);
    });

    return config.CreateMapper();
}
```

**Usage in Startup:**
```csharp
services.AddAutoMapper(GetAssembliesToScanForMapperProfiles());

private static IEnumerable<Assembly> GetAssembliesToScanForMapperProfiles() =>
    new Assembly[] { Assembly.GetExecutingAssembly() };
```

**Key Points:**
- The `IMapper` instance (not `IMapperConfiguration`) is registered as a singleton
- AutoMapper profiles are automatically discovered in specified assemblies
- The mapper is registered as a singleton for performance (configuration is expensive)
- To add profiles from additional assemblies, extend the `GetAssembliesToScanForMapperProfiles` method
- The configuration is validated at startup, failing fast if mapping configurations are invalid

### HTTP Client Factory

The application uses the typed HTTP client factory pattern with custom configuration:

```csharp
services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
services.AddHttpClient("movies", (provider, client) => 
    provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
```

**Architecture:**
- Named HTTP client ("movies") for external API communication
- `IHttpClientDecorator` abstracts client configuration (headers, timeouts, base addresses)
- Integrates with Polly for resilience policies (via `Microsoft.Extensions.Http.Polly`)

**Benefits:**
- Automatic connection pooling and DNS refresh
- Centralized configuration management
- Support for resilience patterns (retry, circuit breaker, timeout)
- Simplified testing through decorator pattern

**Usage Example:**
```csharp
public class MovieService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public MovieService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<Movie> GetMovieAsync(int id)
    {
        var client = _httpClientFactory.CreateClient("movies");
        var response = await client.GetAsync($"/api/movies/{id}");
        // ...
    }
}
```

### Repository Registration

Repository registration follows a comprehensive pattern that includes configuration binding, DbContext setup, and repository interface registration:

```csharp
public static IServiceCollection AddMovieRepository(
    this IServiceCollection services,
    IConfiguration configuration)
{
    Contract.Requires(configuration != null);

    // Register repository interface
    services.TryAddScoped<IMovieRepository, MovieRepository>();

    // Bind and register configuration
    var config = configuration
        .GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<MovieRepositoryConfiguration>();
    services.TryAddSingleton<IMovieRepositoryConfiguration>(config);

    // Register DbContext if not already registered
    var serviceProvider = services.BuildServiceProvider();
    var movieContext = serviceProvider.GetService<MovieContext>();
    if (movieContext == null)
    {
        services.AddDbContext<MovieContext>(options => 
            options.UseSqlServer(config.MoviesConnectionString));
    }

    return services;
}
```

**Configuration Structure:**

The method expects configuration in the following format:
```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "Server=...;Database=...;..."
  }
}
```

**Important Considerations:**

⚠️ **Anti-Pattern Warning**: The code builds an intermediate service provider to check for existing DbContext registration:
```csharp
var serviceProvider = services.BuildServiceProvider();
var movieContext = serviceProvider.GetService<MovieContext>();
```

This pattern should be used cautiously as:
- It creates a temporary service provider that is immediately discarded
- Can lead to memory leaks if services with disposable dependencies are resolved
- May not reflect the final service provider configuration
- Better alternatives include using `TryAddDbContext` or conditional registration flags

**Recommended Refactoring:**
```csharp
// Simply use TryAdd pattern - DbContext registration is idempotent
services.TryAddDbContext<MovieContext>(options => 
    options.UseSqlServer(config.MoviesConnectionString));
```

### Service Layer Registration

Service layer components follow a simple, focused registration pattern:

```csharp
public static class MovieServiceServiceCollectionExtensions
{
    public static IServiceCollection AddMovieService(this IServiceCollection services)
    {
        services.TryAddTransient<IMovieService, MovieService>();
        return services;
    }
}
```

**Design Principles:**
- One extension method per service or logical service group
- Transient lifetime for stateless business logic services
- Clear naming convention: `Add{ServiceName}` pattern
- Minimal dependencies in extension methods

## Custom Service Extensions

### Creating Reusable Service Extensions

When creating custom service extensions, follow these established patterns:

#### 1. Namespace Convention

Place extension methods in the `Microsoft.Extensions.DependencyInjection` namespace for automatic discovery:

```csharp
namespace Microsoft.Extensions.DependencyInjection
{
    public static class YourServiceServiceCollectionExtensions
    {
        // Extension methods here
    }
}
```

**Rationale**: This namespace is automatically imported in `Startup.cs`, making extensions immediately available without additional using statements.

#### 2. Method Signature Pattern

```csharp
public static IServiceCollection Add{ServiceName}(
    this IServiceCollection services,
    IConfiguration configuration = null,
    Action<{Options}> configureOptions = null)
{
    // Validation
    Contract.Requires(services != null);
    
    // Service registration
    services.TryAdd{Lifetime}<IService, ServiceImplementation>();
    
    // Configuration binding (if needed)
    if (configuration != null)
    {
        var config = configuration.GetSection("SectionName").Get<ConfigType>();
        services.TryAddSingleton<IConfigType>(config);
    }
    
    // Options configuration (if needed)
    if (configureOptions != null)
    {
        services.Configure(configureOptions);
    }
    
    return services;
}
```

#### 3. Complex Service Registration Example

For services with multiple dependencies:

```csharp
public static class ComplexServiceServiceCollectionExtensions
{
    public static IServiceCollection AddComplexService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register primary service
        services.TryAddScoped<IComplexService, ComplexService>();
        
        // Register dependencies
        services.TryAddTransient<IDependency1, Dependency1>();
        services.TryAddTransient<IDependency2, Dependency2>();
        
        // Configure options
        services.Configure<ComplexServiceOptions>(
            configuration.GetSection("ComplexService"));
        
        // Register health checks
        services.AddHealthChecks()
            .AddCheck<ComplexServiceHealthCheck>("complex_service");
        
        return services;
    }
}
```

### Organizing Service Registrations

The codebase demonstrates a clear organizational structure:

```
src/
├── BlackSlope.Api/
│   ├── Extensions/
│   │   └── ApiServiceCollectionExtensions.cs          # API-specific services
│   ├── Repositories/
│   │   └── Movies/
│   │       └── Extensions/
│   │           └── MovieRepositoryServiceCollectionExtensions.cs
│   └── Services/
│       └── Movies/
│           └── Extensions/
│               └── MovieServiceServiceCollectionExtensions.cs
└── BlackSlope.Api.Common/
    └── Extensions/
        └── BlackSlopeServiceCollectionExtensions.cs   # Shared/common services
```

**Organizational Principles:**

1. **Co-location**: Extension methods are placed near the services they register
2. **Layering**: Common/shared extensions in `.Common` project, specific extensions in feature folders
3. **Single Responsibility**: Each extension class focuses on a specific functional area
4. **Naming**: `{Feature}ServiceCollectionExtensions` naming pattern

### Best Practices

#### 1. Use TryAdd Methods

Always prefer `TryAdd*` over `Add*` to prevent duplicate registrations:

```csharp
// ✅ Good - Prevents duplicates
services.TryAddTransient<IService, ServiceImpl>();

// ❌ Avoid - Can cause duplicate registrations
services.AddTransient<IService, ServiceImpl>();
```

#### 2. Validate Input Parameters

Use Code Contracts or guard clauses for required parameters:

```csharp
public static IServiceCollection AddMovieRepository(
    this IServiceCollection services,
    IConfiguration configuration)
{
    Contract.Requires(configuration != null);
    // or
    if (configuration == null) throw new ArgumentNullException(nameof(configuration));
    
    // Registration logic
}
```

#### 3. Return IServiceCollection for Chaining

Always return the service collection to enable fluent configuration:

```csharp
public static IServiceCollection AddMyService(this IServiceCollection services)
{
    services.TryAddTransient<IMyService, MyService>();
    return services; // ✅ Enables chaining
}
```

#### 4. Group Related Registrations

Register related services together in a single extension method:

```csharp
public static IServiceCollection AddHealthChecksService(this IServiceCollection services)
{
    services.AddHealthChecks()
        .AddDbContextCheck<MovieContext>();
        // Add more health checks as needed

    return services;
}
```

#### 5. Configuration Binding Pattern

Follow the established pattern for configuration binding:

```csharp
// Bind configuration section to strongly-typed object
var config = configuration
    .GetSection(Assembly.GetExecutingAssembly().GetName().Name)
    .Get<ConfigurationType>();

// Register as singleton for immutable configuration
services.TryAddSingleton<IConfigurationType>(config);
```

#### 6. Avoid Building Service Provider in Extensions

⚠️ **Warning**: Building intermediate service providers can cause issues:

```csharp
// ❌ Avoid - Creates temporary service provider
var serviceProvider = services.BuildServiceProvider();
var service = serviceProvider.GetService<IService>();

// ✅ Better - Use TryAdd pattern or conditional flags
services.TryAddScoped<IService, ServiceImpl>();
```

#### 7. Document Complex Registrations

Add XML documentation for non-obvious registration logic:

```csharp
/// <summary>
/// Adds the Movie repository and its dependencies to the service collection.
/// Registers MovieContext with SQL Server provider using connection string from configuration.
/// </summary>
/// <param name="services">The service collection</param>
/// <param name="configuration">Application configuration containing connection strings</param>
/// <returns>The service collection for chaining</returns>
public static IServiceCollection AddMovieRepository(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Implementation
}
```

## Integration with Startup

The `Startup.ConfigureServices` method orchestrates all service registrations:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Core ASP.NET services
    services.AddMvcService();
    
    // Application configuration
    ApplicationConfiguration(services);
    CorsConfiguration(services);

    // Infrastructure services
    services.AddSwagger(HostConfig.Swagger);
    services.AddAzureAd(HostConfig.AzureAd);
    services.AddAutoMapper(GetAssembliesToScanForMapperProfiles());
    services.AddCorrelation();
    
    // Framework abstractions
    services.AddTransient<IFileSystem, FileSystem>();
    
    // NOTE: Pick one of the below versioning services
    services.AddTransient<IVersionService, AssemblyVersionService>(); // For Version parsing via Assembly ref
    // services.AddTransient<IVersionService, JsonVersionService>();   // For Version parsing via JSON

    // API services
    services.AddMvcApi();

    // Business services
    services.AddMovieService();

    // Data access
    services.AddMovieRepository(_configuration);
    services.AddFakeApiRepository();

    // HTTP clients
    services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
    services.AddHttpClient("movies", (provider, client) => 
        provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
    
    // Health checks
    HealthCheckStartup.ConfigureServices(services, _configuration);

    // Validation
    services.AddValidators();
}
```

**Registration Order Considerations:**

1. **Configuration First**: Register configuration services before services that depend on them (via `ApplicationConfiguration(services)` method)
2. **Infrastructure Before Application**: Register framework services before business services
3. **Dependencies Before Dependents**: Register dependencies before services that consume them
4. **Health Checks Last**: Register health checks after all services they monitor

**ApplicationConfiguration Method:**

The `Startup` class includes a private `ApplicationConfiguration` method that registers configuration objects:

```csharp
private void ApplicationConfiguration(IServiceCollection services)
{
    services.AddSingleton(_ => _configuration);
    services.AddSingleton(_configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name).Get<HostConfig>());

    var serviceProvider = services.BuildServiceProvider();
    HostConfig = serviceProvider.GetService<HostConfig>();
}
```

⚠️ **Note**: This method builds an intermediate service provider to retrieve the `HostConfig` for use in `ConfigureServices`. While this works, it's an anti-pattern that should be used cautiously. The HostConfig is stored as a property and used by other configuration methods like `AddSwagger(HostConfig.Swagger)` and `AddAzureAd(HostConfig.AzureAd)`.

## Common Patterns and Gotchas

### Pattern: Configuration Section Naming

The codebase uses assembly name as configuration section key:

```csharp
var config = configuration
    .GetSection(Assembly.GetExecutingAssembly().GetName().Name)
    .Get<MovieRepositoryConfiguration>();
```

**Configuration File:**
```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "...",
    "Swagger": { ... },
    "AzureAd": { ... }
  }
}
```

### Gotcha: Service Provider Building

⚠️ Avoid building service providers during configuration:

```csharp
// ❌ Anti-pattern - can cause memory leaks
var serviceProvider = services.BuildServiceProvider();
var config = serviceProvider.GetService<IConfiguration>();
```

**Solution**: Pass dependencies as parameters to extension methods.

### Pattern: Conditional Service Registration

For services that may be registered by tests or other modules:

```csharp
if (!services.Any(x => x.ServiceType == typeof(IMyService)))
{
    services.AddScoped<IMyService, MyService>();
}

// Or use TryAdd pattern (preferred)
services.TryAddScoped<IMyService, MyService>();
```

### Gotcha: DbContext Lifetime

Always register DbContext as **Scoped**, never Singleton or Transient:

```csharp
// ✅ Correct - Scoped lifetime
services.AddDbContext<MovieContext>(options => ...);

// ❌ Wrong - Will cause threading issues
services.AddSingleton<MovieContext>();

// ❌ Wrong - Will cause performance issues
services.AddTransient<MovieContext>();
```

## Related Documentation

- [Architecture Overview](/architecture/overview.md) - System architecture and design principles
- [Service Configuration](/configuration/service_configuration.md) - Configuration management and binding
- [Services](/features/services.md) - Business service implementations and patterns

## Summary

The BlackSlope API's dependency injection architecture provides:

- **Modularity** through extension method patterns
- **Testability** via interface-based registration and TryAdd patterns
- **Maintainability** through clear organizational structure
- **Flexibility** for service replacement and configuration
- **Performance** through appropriate lifetime management

By following the established patterns and best practices outlined in this document, developers can extend the application's service registration infrastructure while maintaining consistency and reliability.