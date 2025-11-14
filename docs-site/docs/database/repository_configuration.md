# Repository Configuration

## Configuration Pattern

The BlackSlope API implements a strongly-typed configuration pattern for repository settings, leveraging ASP.NET Core's configuration system to bind settings from multiple sources into type-safe configuration objects. This approach provides compile-time safety, IntelliSense support, and clear contracts for configuration requirements.

### Configuration Architecture

The repository configuration follows a three-layer pattern:

1. **Interface Definition**: Defines the contract for configuration properties
2. **Implementation Class**: Provides the concrete implementation with settable properties for binding
3. **Service Registration**: Registers the configuration as a singleton in the dependency injection container

This pattern ensures that:
- Configuration requirements are explicitly documented through interfaces
- Configuration values are validated at startup
- Dependencies can be injected with minimal coupling to configuration sources
- Unit testing is simplified through interface-based mocking

### Configuration Binding Process

The configuration system uses the `IConfiguration.GetSection()` method to locate configuration values within the JSON hierarchy, followed by the `.Get<T>()` method to bind values to strongly-typed objects:

```csharp
var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
    .Get<MovieRepositoryConfiguration>();
```

This approach:
- Uses the assembly name as the root configuration section (e.g., "BlackSlope.Api")
- Automatically maps JSON properties to C# properties by name (case-insensitive)
- Supports nested configuration objects
- Handles type conversion for primitive types

## Movie Repository Configuration

### IMovieRepositoryConfiguration Interface

The `IMovieRepositoryConfiguration` interface defines the configuration contract for the Movie repository:

```csharp
namespace BlackSlope.Repositories.Movies.Configuration
{
    public interface IMovieRepositoryConfiguration
    {
        string MoviesConnectionString { get; set; }
    }
}
```

**Key Design Decisions:**

- **Mutable Properties**: The interface uses `{ get; set; }` rather than read-only properties to support configuration binding, which requires settable properties
- **Namespace Organization**: Configuration interfaces are placed in a dedicated `Configuration` namespace within the repository folder structure
- **Single Responsibility**: The interface focuses solely on connection string management, adhering to the Interface Segregation Principle

### Configuration Implementation

The `MovieRepositoryConfiguration` class provides the concrete implementation:

```csharp
namespace BlackSlope.Repositories.Movies.Configuration
{
    public class MovieRepositoryConfiguration : IMovieRepositoryConfiguration
    {
        public string MoviesConnectionString { get; set; }
    }
}
```

**Implementation Characteristics:**

- **POCO Design**: Simple Plain Old CLR Object with no additional logic
- **Auto-Properties**: Uses automatic properties for clean, concise code
- **No Validation Logic**: Validation is handled separately (see Best Practices section)
- **Default Values**: No default values are set; configuration must be provided explicitly

### Dependency Injection

The Movie repository configuration is registered in the DI container through an extension method:

```csharp
public static class MovieRepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddMovieRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Contract.Requires(configuration != null);

        services.TryAddScoped<IMovieRepository, MovieRepository>();

        var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
            .Get<MovieRepositoryConfiguration>();
        services.TryAddSingleton<IMovieRepositoryConfiguration>(config);

        var serviceProvider = services.BuildServiceProvider();
        var movieContext = serviceProvider.GetService<MovieContext>();
        if (movieContext == null)
        {
            services.AddDbContext<MovieContext>(options => 
                options.UseSqlServer(config.MoviesConnectionString));
        }

        return services;
    }
}
```

**Registration Details:**

- **Singleton Lifetime**: Configuration is registered as a singleton since it doesn't change during application lifetime
- **TryAddSingleton**: Uses `TryAddSingleton` to prevent duplicate registrations if called multiple times
- **Interface Registration**: Registers the interface type, not the concrete class, promoting loose coupling
- **DbContext Integration**: Automatically configures Entity Framework Core's `MovieContext` with the connection string

**Usage in Startup.cs:**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... other service registrations
    
    services.AddMovieRepository(_configuration);
    
    // ... additional configurations
}
```

## Connection String Management

### Connection String Format

The Movies database connection string follows the SQL Server connection string format:

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=.,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;"
  }
}
```

**Connection String Components:**

| Component | Value | Purpose |
|-----------|-------|---------|
| `data source` | `.,1433` | SQL Server instance (localhost, port 1433) |
| `initial catalog` | `movies` | Database name |
| `Integrated Security` | `true` | Use Windows Authentication |
| `MultipleActiveResultSets` | `True` | Enable MARS for concurrent queries |

### Security Considerations

**Integrated Security vs. SQL Authentication:**

The default configuration uses Windows Authentication (`Integrated Security=true`), which:
- Eliminates the need to store credentials in configuration files
- Leverages Windows security infrastructure
- Simplifies credential management in development environments

For production deployments, consider:
- **Azure SQL**: Use Azure AD authentication with managed identities
- **SQL Authentication**: Store credentials in Azure Key Vault or environment variables
- **Connection String Encryption**: Encrypt sensitive configuration sections

**Example with SQL Authentication (for reference):**

```json
{
  "MoviesConnectionString": "data source=server.database.windows.net;initial catalog=movies;User ID=username;Password=password;MultipleActiveResultSets=True;"
}
```

⚠️ **Warning**: Never commit connection strings with credentials to source control. Use User Secrets for local development and secure vaults for production.

## Configuration Sources

The ASP.NET Core configuration system supports multiple sources with a defined precedence order. Configuration values are loaded from the following sources (later sources override earlier ones):

### 1. appsettings.json

The base configuration file containing default settings for all environments:

```json
{
  "BlackSlope.Api": {
    "BaseUrl": "http://localhost:55644",
    "MoviesConnectionString": "data source=.,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;",
    "Swagger": {
      "Version": "1",
      "ApplicationName": "BlackSlope",
      "XmlFile": "BlackSlope.Api.xml"
    },
    "AzureAd": {
      "AadInstance": "https://login.microsoftonline.com/{0}",
      "Tenant": "[tenant-id]",
      "Audience": "https://[host-name]"
    },
    "Serilog": {
      "MinimumLevel": "information",
      "FileName": "log.txt",
      "WriteToFile": "false",
      "WriteToAppInsights": "false",
      "WriteToConsole": "true"
    },
    "ApplicationInsights": {
      "InstrumentationKey": "[instrumentation-key]"
    },
    "HealthChecks": {
      "Endpoint": "/health"
    }
  },
  "AllowedHosts": "*"
}
```

**Best Practices for appsettings.json:**

- Store non-sensitive default values
- Use placeholder values (e.g., `[tenant-id]`) for required settings
- Include comments for complex configuration sections
- Commit to source control for team-wide defaults

### 2. Environment Variables

Environment variables override appsettings.json values and are ideal for:
- Container deployments (Docker, Kubernetes)
- Azure App Service configuration
- CI/CD pipeline settings

**Naming Convention:**

Environment variables use double underscores (`__`) to represent JSON hierarchy:

```bash
# Overrides BlackSlope.Api:MoviesConnectionString
BlackSlope.Api__MoviesConnectionString="Server=prod-server;Database=movies;..."

# Overrides BlackSlope.Api:AzureAd:Tenant
BlackSlope.Api__AzureAd__Tenant="production-tenant-id"
```

**Docker Example:**

```dockerfile
ENV BlackSlope.Api__MoviesConnectionString="Server=sql-server;Database=movies;User ID=sa;Password=YourStrong@Passw0rd"
```

### 3. User Secrets

User Secrets provide secure local development configuration without committing sensitive data to source control. The project includes User Secrets support through the SDK.

**Enabling User Secrets:**

```bash
# Initialize user secrets for the project
dotnet user-secrets init --project src/BlackSlope.Api

# Set a connection string
dotnet user-secrets set "BlackSlope.Api:MoviesConnectionString" "Server=localhost;Database=movies_dev;Integrated Security=true" --project src/BlackSlope.Api

# Set Azure AD tenant
dotnet user-secrets set "BlackSlope.Api:AzureAd:Tenant" "your-dev-tenant-id" --project src/BlackSlope.Api
```

**User Secrets Location:**

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

**Important Notes:**

- User Secrets are only loaded in the Development environment
- Each developer maintains their own secrets.json file
- User Secrets are not encrypted; they're simply stored outside the project directory
- For production, use Azure Key Vault or similar secure storage

### Configuration Precedence

Configuration sources are applied in the following order (later sources win):

1. appsettings.json
2. appsettings.{Environment}.json (e.g., appsettings.Development.json)
3. User Secrets (Development environment only)
4. Environment Variables
5. Command-line arguments

**Example Scenario:**

```json
// appsettings.json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "Server=localhost;Database=movies;..."
  }
}

// User Secrets (Development)
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "Server=dev-server;Database=movies_dev;..."
  }
}

// Environment Variable (Production)
BlackSlope.Api__MoviesConnectionString="Server=prod-server;Database=movies_prod;..."
```

Result: Development uses dev-server, Production uses prod-server.

## Best Practices

### Strongly-Typed Configuration

**Benefits:**

- **Compile-Time Safety**: Typos and type mismatches are caught at compile time
- **IntelliSense Support**: IDE provides autocomplete for configuration properties
- **Refactoring Support**: Renaming properties updates all references
- **Documentation**: Interfaces serve as self-documenting contracts

**Implementation Pattern:**

```csharp
// 1. Define interface
public interface IMovieRepositoryConfiguration
{
    string MoviesConnectionString { get; set; }
}

// 2. Implement configuration class
public class MovieRepositoryConfiguration : IMovieRepositoryConfiguration
{
    public string MoviesConnectionString { get; set; }
}

// 3. Register in DI container
services.TryAddSingleton<IMovieRepositoryConfiguration>(config);

// 4. Inject into consumers
public class MovieRepository : IMovieRepository
{
    private readonly IMovieRepositoryConfiguration _config;
    
    public MovieRepository(IMovieRepositoryConfiguration config)
    {
        _config = config;
    }
}
```

### Validation

Configuration should be validated at application startup to fail fast if required settings are missing or invalid.

**Recommended Validation Approaches:**

**1. Options Pattern with Data Annotations:**

```csharp
using System.ComponentModel.DataAnnotations;

public class MovieRepositoryConfiguration : IMovieRepositoryConfiguration
{
    [Required(ErrorMessage = "MoviesConnectionString is required")]
    [MinLength(10, ErrorMessage = "MoviesConnectionString must be at least 10 characters")]
    public string MoviesConnectionString { get; set; }
}

// In Startup.cs
services.AddOptions<MovieRepositoryConfiguration>()
    .Bind(configuration.GetSection("BlackSlope.Api"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

**2. FluentValidation Integration:**

Given the project uses FluentValidation (version 10.3.6), create a validator:

```csharp
using FluentValidation;

public class MovieRepositoryConfigurationValidator : AbstractValidator<MovieRepositoryConfiguration>
{
    public MovieRepositoryConfigurationValidator()
    {
        RuleFor(x => x.MoviesConnectionString)
            .NotEmpty()
            .WithMessage("MoviesConnectionString is required")
            .Must(BeValidConnectionString)
            .WithMessage("MoviesConnectionString must be a valid SQL Server connection string");
    }
    
    private bool BeValidConnectionString(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return !string.IsNullOrEmpty(builder.DataSource) && 
                   !string.IsNullOrEmpty(builder.InitialCatalog);
        }
        catch
        {
            return false;
        }
    }
}

// Register validator
services.AddTransient<IValidator<MovieRepositoryConfiguration>, MovieRepositoryConfigurationValidator>();
```

**3. Manual Validation in Extension Method:**

```csharp
public static IServiceCollection AddMovieRepository(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<MovieRepositoryConfiguration>();
    
    // Validate configuration
    if (string.IsNullOrWhiteSpace(config?.MoviesConnectionString))
    {
        throw new InvalidOperationException(
            "MoviesConnectionString is not configured. Please check appsettings.json or environment variables.");
    }
    
    services.TryAddSingleton<IMovieRepositoryConfiguration>(config);
    
    // ... rest of registration
}
```

### Environment-Specific Settings

**Recommended Structure:**

```
src/BlackSlope.Api/
├── appsettings.json                    # Base settings
├── appsettings.Development.json        # Development overrides
├── appsettings.Staging.json            # Staging overrides
├── appsettings.Production.json         # Production overrides
```

**Example appsettings.Development.json:**

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=localhost;initial catalog=movies_dev;Integrated Security=true;MultipleActiveResultSets=True;",
    "Serilog": {
      "MinimumLevel": "debug",
      "WriteToConsole": "true"
    }
  }
}
```

**Example appsettings.Production.json:**

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "",  // Set via environment variables or Key Vault
    "Serilog": {
      "MinimumLevel": "warning",
      "WriteToAppInsights": "true",
      "WriteToConsole": "false"
    }
  }
}
```

### Configuration Security Checklist

- [ ] Never commit connection strings with credentials to source control
- [ ] Use User Secrets for local development
- [ ] Use Azure Key Vault or similar for production secrets
- [ ] Implement configuration validation at startup
- [ ] Use managed identities for Azure resources when possible
- [ ] Encrypt sensitive configuration sections in production
- [ ] Rotate credentials regularly
- [ ] Audit configuration access in production environments
- [ ] Use separate databases for each environment
- [ ] Implement least-privilege access for database connections

### Related Documentation

For more information on related topics, see:

- [Repositories](/features/repositories.md) - Detailed information on repository pattern implementation
- [Application Settings](/configuration/application_settings.md) - Complete application configuration reference
- [Entity Framework](/database/entity_framework.md) - Database context and Entity Framework Core configuration

### Common Pitfalls and Troubleshooting

**Issue: Configuration not loading**

```csharp
// Problem: Section name doesn't match assembly name
var config = configuration.GetSection("WrongName").Get<MovieRepositoryConfiguration>();

// Solution: Use assembly name or verify JSON structure
var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
    .Get<MovieRepositoryConfiguration>();
```

**Issue: Null configuration values**

```csharp
// Problem: Configuration binding returns null if section doesn't exist
var config = configuration.GetSection("NonExistent").Get<MovieRepositoryConfiguration>();
// config is null, causing NullReferenceException later

// Solution: Validate after binding
var config = configuration.GetSection("BlackSlope.Api").Get<MovieRepositoryConfiguration>();
if (config == null)
{
    throw new InvalidOperationException("Configuration section 'BlackSlope.Api' not found");
}
```

**Issue: Connection string not working in Docker**

```bash
# Problem: Windows-style connection string in Linux container
"MoviesConnectionString": "data source=.\\SQLEXPRESS;..."

# Solution: Use network-accessible server name
"MoviesConnectionString": "data source=sql-server,1433;..."
```

**Issue: BuildServiceProvider warning**

The current implementation calls `BuildServiceProvider()` during service registration, which can cause issues:

```csharp
// Current implementation (not recommended)
var serviceProvider = services.BuildServiceProvider();
var movieContext = serviceProvider.GetService<MovieContext>();
```

**Better approach:**

```csharp
public static IServiceCollection AddMovieRepository(
    this IServiceCollection services,
    IConfiguration configuration)
{
    var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<MovieRepositoryConfiguration>();
    
    services.TryAddSingleton<IMovieRepositoryConfiguration>(config);
    services.TryAddScoped<IMovieRepository, MovieRepository>();
    
    // Register DbContext without checking for existing registration
    services.AddDbContext<MovieContext>(options => 
        options.UseSqlServer(config.MoviesConnectionString));
    
    return services;
}
```

This approach avoids creating an intermediate service provider and lets the DI container handle duplicate registrations naturally.