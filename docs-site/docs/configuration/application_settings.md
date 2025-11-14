# Application Settings

## Configuration System

The BlackSlope API implements the standard ASP.NET Core configuration system, which provides a flexible, hierarchical approach to managing application settings across different environments. The configuration system follows a layered architecture where settings can be overridden based on the deployment environment.

### ASP.NET Core Configuration

The application leverages the built-in ASP.NET Core configuration framework through the following NuGet packages:

- **Microsoft.Extensions.Configuration.FileExtensions** (6.0.0): Enables file-based configuration providers
- **Microsoft.Extensions.Configuration.Json** (6.0.0): Provides JSON configuration file support

Configuration is injected into the `Startup` class constructor and made available throughout the application via dependency injection:

```csharp
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }
}
```

### Configuration Sources Hierarchy

The ASP.NET Core configuration system reads settings from multiple sources in a specific order, with later sources overriding earlier ones:

1. **appsettings.json** - Base configuration file containing default settings
2. **appsettings.{Environment}.json** - Environment-specific overrides (e.g., `appsettings.Development.json`, `appsettings.docker.json`)
3. **User Secrets** - Local development secrets (Development environment only)
4. **Environment Variables** - System-level configuration overrides
5. **Command-line Arguments** - Runtime parameter overrides

This hierarchy allows developers to maintain secure, environment-specific configurations without modifying the base settings file.

### Environment-Specific Configuration

The application determines the current environment through the `ASPNETCORE_ENVIRONMENT` environment variable. Common values include:

- `Development` - Local development environment
- `Staging` - Pre-production testing environment
- `Production` - Live production environment
- `docker` - Docker containerized deployment

Environment detection is used in the `Configure` method to enable development-specific features:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios
        app.UseHsts();
    }
}
```

## appsettings.json

The base configuration file (`appsettings.json`) contains all default settings for the application. All configuration is nested under a root key matching the assembly name: `BlackSlope.Api`.

### Base Configuration Structure

```json
{
  "BlackSlope.Api": {
    "BaseUrl": "http://localhost:55644",
    "Swagger": { ... },
    "AzureAd": { ... },
    "Serilog": { ... },
    "ApplicationInsights": { ... },
    "HealthChecks": { ... },
    "MoviesConnectionString": "..."
  },
  "AllowedHosts": "*"
}
```

### Common Settings

#### Base URL Configuration

The `BaseUrl` setting defines the default host address for the API:

```json
"BaseUrl": "http://localhost:55644"
```

This setting is used for:
- Local development server binding
- Swagger UI base path configuration
- CORS policy configuration
- Health check endpoint registration

#### Allowed Hosts

The `AllowedHosts` setting controls host filtering middleware:

```json
"AllowedHosts": "*"
```

**⚠️ Security Warning**: The wildcard (`*`) value allows requests from any host. In production environments, this should be restricted to specific domain names:

```json
"AllowedHosts": "api.blackslope.com;*.blackslope.com"
```

### Connection Strings

The application uses a custom connection string configuration approach rather than the standard `ConnectionStrings` section. The database connection string is defined directly within the `BlackSlope.Api` configuration section:

```json
"MoviesConnectionString": "data source=.,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;"
```

**Connection String Components**:

| Component | Value | Description |
|-----------|-------|-------------|
| `data source` | `.,1433` | SQL Server instance (local machine, port 1433) |
| `initial catalog` | `movies` | Database name |
| `Integrated Security` | `true` | Uses Windows Authentication |
| `MultipleActiveResultSets` | `True` | Enables MARS for concurrent query execution |

This connection string is accessed in the repository configuration:

```csharp
services.AddMovieRepository(_configuration);
```

The repository extension method retrieves the connection string from the configuration and configures Entity Framework Core accordingly.

## Environment-Specific Files

### appsettings.Development.json

The Development environment configuration file overrides logging settings for enhanced debugging during local development:

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

**Logging Level Configuration**:

| Category | Level | Purpose |
|----------|-------|---------|
| `Default` | `Debug` | Application code logs at Debug level and above |
| `System` | `Information` | System-level logs at Information level and above |
| `Microsoft` | `Information` | ASP.NET Core framework logs at Information level and above |

This configuration provides verbose logging for application code while reducing noise from framework components.

**Note**: This file uses the standard ASP.NET Core `Logging` section rather than the custom `Serilog` section defined in the base configuration. The application appears to support both logging configurations, with Serilog providing more advanced features (see Logging Configuration section below).

### appsettings.docker.json

The Docker environment configuration file provides container-specific overrides:

```json
{
  "BlackSlope.Api": {
    "Serilog": {
      "WriteToFile": "true",
      "WriteToConsole": "false"
    },
    "MoviesConnectionString": "data source=db,1433;initial catalog=movies;User Id=sa;Password=YourStrong!Passw0rd;MultipleActiveResultSets=True"
  }
}
```

**Docker-Specific Changes**:

1. **Logging Configuration**:
   - Enables file-based logging (`WriteToFile: true`)
   - Disables console logging (`WriteToConsole: false`)
   - File logging is preferred in containers for log aggregation and persistence

2. **Database Connection**:
   - Changes data source from `.` (localhost) to `db` (Docker Compose service name)
   - Switches from Integrated Security (Windows Auth) to SQL Server Authentication
   - Uses SQL Server credentials: `sa` user with password
   - **⚠️ Security Warning**: The hardcoded password should be replaced with environment variables or Docker secrets in production

**Docker Compose Integration**:

The `db` hostname corresponds to a SQL Server container service defined in `docker-compose.yml`. The connection string assumes:
- A SQL Server container named `db`
- SQL Server listening on port 1433
- SA account enabled with the specified password
- The `movies` database created during container initialization

For more information on Docker deployment, see [Docker Deployment Documentation](/deployment/docker.md).

### Environment Variable Overrides

Any configuration value can be overridden using environment variables with a specific naming convention:

**Format**: `{SectionName}__{SubSectionName}__{PropertyName}`

**Examples**:

```bash
# Override BaseUrl
BlackSlope.Api__BaseUrl=https://api.production.com

# Override Azure AD Tenant
BlackSlope.Api__AzureAd__Tenant=production-tenant-id

# Override Serilog minimum level
BlackSlope.Api__Serilog__MinimumLevel=warning

# Override connection string
BlackSlope.Api__MoviesConnectionString="Server=prod-sql;Database=movies;..."
```

**Docker Environment Variables**:

When running in Docker, environment variables can be specified in `docker-compose.yml`:

```yaml
services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=docker
      - BlackSlope.Api__AzureAd__Tenant=${AZURE_TENANT_ID}
      - BlackSlope.Api__MoviesConnectionString=${DB_CONNECTION_STRING}
```

## Configuration Sections

### Host Configuration

The application uses a strongly-typed configuration model through the `HostConfig` class, which aggregates all configuration sections:

```csharp
public class HostConfig
{
    public string BaseUrl { get; set; }
    public SwaggerConfig Swagger { get; set; }
    public AzureAdConfig AzureAd { get; set; }
    public SerilogConfig Serilog { get; set; }
    public ApplicationInsightsConfig ApplicationInsights { get; set; }
    public HealthChecksConfig HealthChecks { get; set; }
}
```

**Configuration Binding**:

The `HostConfig` is bound to the configuration section matching the assembly name and registered as a singleton:

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

This approach provides:
- **Type Safety**: Compile-time checking of configuration properties
- **IntelliSense Support**: IDE autocomplete for configuration values
- **Validation**: Ability to implement validation logic in configuration classes
- **Dependency Injection**: Configuration objects can be injected into services

**Usage Example**:

```csharp
public class MyService
{
    private readonly HostConfig _config;

    public MyService(HostConfig config)
    {
        _config = config;
    }

    public void DoWork()
    {
        var baseUrl = _config.BaseUrl;
        var swaggerVersion = _config.Swagger.Version;
    }
}
```

For detailed service configuration patterns, see [Service Configuration Documentation](/configuration/service_configuration.md).

### Swagger Configuration

Swagger/OpenAPI documentation is configured through the `SwaggerConfig` section:

```json
"Swagger": {
  "Version": "1",
  "ApplicationName": "BlackSlope",
  "XmlFile": "BlackSlope.Api.xml"
}
```

**Configuration Properties**:

| Property | Value | Description |
|----------|-------|-------------|
| `Version` | `"1"` | API version number for Swagger documentation |
| `ApplicationName` | `"BlackSlope"` | Display name in Swagger UI |
| `XmlFile` | `"BlackSlope.Api.xml"` | XML documentation file for API comments |

**Swagger Integration**:

The Swagger configuration is applied in the `ConfigureServices` method:

```csharp
services.AddSwagger(HostConfig.Swagger);
```

And enabled in the request pipeline:

```csharp
app.UseSwagger(HostConfig.Swagger);
```

**XML Documentation**:

The `XmlFile` property references an XML documentation file generated from code comments. To enable XML documentation generation, ensure the project file includes:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <DocumentationFile>BlackSlope.Api.xml</DocumentationFile>
</PropertyGroup>
```

This enables rich API documentation with parameter descriptions, response types, and example values in the Swagger UI.

**Swagger UI Access**:

The Swagger UI is typically available at:
- Development: `http://localhost:55644/swagger`
- Production: `{BaseUrl}/swagger`

The application uses **Swashbuckle.AspNetCore.SwaggerUI** (version 6.3.0) for the interactive API documentation interface.

### Azure AD Configuration

Azure Active Directory authentication is configured through the `AzureAd` section:

```json
"AzureAd": {
  "AadInstance": "https://login.microsoftonline.com/{0}",
  "Tenant": "[tenant-id]",
  "Audience": "https://[host-name]"
}
```

**Configuration Properties**:

| Property | Description | Example |
|----------|-------------|---------|
| `AadInstance` | Azure AD authentication endpoint template | `https://login.microsoftonline.com/{0}` |
| `Tenant` | Azure AD tenant ID or domain name | `contoso.onmicrosoft.com` or GUID |
| `Audience` | Expected audience (resource) in JWT tokens | `https://api.blackslope.com` |

**Placeholder Values**:

The configuration file contains placeholder values that must be replaced:
- `[tenant-id]`: Replace with your Azure AD tenant ID
- `[host-name]`: Replace with your API's public hostname

**Azure AD Integration**:

The Azure AD configuration is applied in the `ConfigureServices` method:

```csharp
services.AddAzureAd(HostConfig.AzureAd);
```

This extension method configures:
- JWT Bearer authentication
- Token validation parameters
- Azure AD authority URL construction
- Audience validation

**Authentication Libraries**:

The application uses multiple authentication libraries:

- **Azure.Identity** (1.14.2): Modern Azure AD authentication with managed identity support
- **Microsoft.IdentityModel.Clients.ActiveDirectory** (5.2.9): Legacy ADAL library for backward compatibility
- **Microsoft.IdentityModel.JsonWebTokens** (7.7.1): JWT token handling
- **System.IdentityModel.Tokens.Jwt** (7.7.1): JWT token validation

**Authentication Middleware**:

Authentication is enabled in the request pipeline:

```csharp
app.UseAuthentication();
```

This middleware must be placed after `UseRouting()` and before `UseEndpoints()` to properly secure API endpoints.

**Security Considerations**:

1. **Token Validation**: The application validates JWT tokens issued by Azure AD
2. **Audience Validation**: Ensures tokens are intended for this API
3. **Issuer Validation**: Verifies tokens are issued by the configured Azure AD tenant
4. **Signature Validation**: Validates token signatures using Azure AD public keys

For comprehensive authentication implementation details, see [Authentication Documentation](/security/authentication.md).

**Environment-Specific Configuration**:

Different environments should use different Azure AD configurations:

```bash
# Development
BlackSlope.Api__AzureAd__Tenant=dev-tenant-id
BlackSlope.Api__AzureAd__Audience=https://dev-api.blackslope.com

# Production
BlackSlope.Api__AzureAd__Tenant=prod-tenant-id
BlackSlope.Api__AzureAd__Audience=https://api.blackslope.com
```

### Logging Configuration

The application implements custom logging configuration through the `Serilog` section:

```json
"Serilog": {
  "MinimumLevel": "information",
  "FileName": "log.txt",
  "WriteToFile": "false",
  "WriteToConsole": "true"
}
```

**Configuration Properties**:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MinimumLevel` | string/int | `"information"` | Minimum log level to capture |
| `FileName` | string | `"log.txt"` | Log file name for file-based logging |
| `WriteToFile` | string | `"false"` | Enable/disable file logging |
| `WriteToConsole` | string | `"true"` | Enable/disable console logging |
| `WriteToAppInsights` | string | `"false"` | Enable/disable Application Insights logging |

**Log Levels**:

The `MinimumLevel` can be specified as either a numeric value or string name:

| Level | Value | Description |
|-------|-------|-------------|
| Verbose | 0 | Most detailed logging |
| Debug | 1 | Debugging information |
| Information | 2 | General informational messages |
| Warning | 3 | Warning messages |
| Error | 4 | Error messages |
| Fatal | 5 | Critical failures |

**Logging Sinks**:

The application supports multiple logging destinations (sinks):

1. **Console Logging**: Outputs logs to the console (stdout)
   - Enabled by default in development
   - Disabled in Docker containers
   - Useful for local debugging and cloud platform log aggregation

2. **File Logging**: Writes logs to a text file
   - Disabled by default in development
   - Enabled in Docker containers
   - File location: Application root directory
   - File name configurable via `FileName` property

3. **Application Insights Logging**: Sends logs to Azure Application Insights
   - Disabled by default
   - Requires valid `InstrumentationKey` in ApplicationInsights section
   - Provides cloud-based log aggregation and analysis

**Environment-Specific Logging**:

Different environments use different logging configurations:

**Development** (appsettings.json):
```json
"Serilog": {
  "MinimumLevel": "information",
  "WriteToConsole": "true",
  "WriteToFile": "false"
}
```

**Docker** (appsettings.docker.json):
```json
"Serilog": {
  "WriteToFile": "true",
  "WriteToConsole": "false"
}
```

**Production** (via environment variables):
```bash
BlackSlope.Api__Serilog__MinimumLevel=warning
BlackSlope.Api__Serilog__WriteToAppInsights=true
BlackSlope.Api__Serilog__WriteToConsole=false
BlackSlope.Api__Serilog__WriteToFile=false
```

**Logging Dependencies**:

The application uses:
- **Microsoft.Extensions.Logging.Debug** (6.0.0): Debug output provider
- Custom Serilog integration (configuration suggests Serilog usage, though not explicitly listed in dependencies)

**Best Practices**:

1. **Development**: Use console logging with `Debug` or `Information` level
2. **Docker**: Use file logging for container log persistence
3. **Production**: Use Application Insights with `Warning` or `Error` level
4. **Sensitive Data**: Never log sensitive information (passwords, tokens, PII)

### Application Insights Configuration

Azure Application Insights integration is configured through the `ApplicationInsights` section:

```json
"ApplicationInsights": {
  "InstrumentationKey": "[instrumentation-key]"
}
```

**Configuration Properties**:

| Property | Description | Format |
|----------|-------------|--------|
| `InstrumentationKey` | Application Insights resource key | GUID format |

**Placeholder Value**:

The `[instrumentation-key]` placeholder must be replaced with an actual Application Insights instrumentation key from the Azure portal.

**Application Insights Features**:

When properly configured, Application Insights provides:
- **Request Tracking**: Automatic HTTP request logging
- **Dependency Tracking**: External service call monitoring
- **Exception Tracking**: Unhandled exception capture
- **Custom Telemetry**: Application-specific metrics and events
- **Performance Monitoring**: Response time and throughput analysis
- **Log Aggregation**: Centralized log collection and querying

**Integration with Serilog**:

The Application Insights integration works in conjunction with the Serilog configuration:

```json
"Serilog": {
  "WriteToAppInsights": "true"
}
```

When `WriteToAppInsights` is enabled, log entries are sent to Application Insights for analysis and alerting.

**Environment-Specific Keys**:

Different environments should use different Application Insights resources:

```bash
# Development
BlackSlope.Api__ApplicationInsights__InstrumentationKey=dev-key-guid

# Staging
BlackSlope.Api__ApplicationInsights__InstrumentationKey=staging-key-guid

# Production
BlackSlope.Api__ApplicationInsights__InstrumentationKey=prod-key-guid
```

**Security Considerations**:

- Instrumentation keys are not secrets but should still be managed carefully
- Use separate Application Insights resources per environment
- Consider using Azure Key Vault for centralized key management
- Rotate keys periodically as part of security best practices

### Health Checks Configuration

The application implements health check endpoints for monitoring and orchestration:

```json
"HealthChecks": {
  "Endpoint": "/health"
}
```

**Configuration Properties**:

| Property | Value | Description |
|----------|-------|-------------|
| `Endpoint` | `"/health"` | URL path for health check endpoint |

**Health Check Implementation**:

Health checks are configured in the `HealthCheckStartup` class:

```csharp
HealthCheckStartup.ConfigureServices(services, _configuration);
```

And registered in the request pipeline:

```csharp
HealthCheckStartup.Configure(app, env, HostConfig);
```

**Health Check Dependencies**:

The application uses the following health check packages:

- **AspNetCore.HealthChecks.SqlServer** (5.0.3): SQL Server connectivity checks
- **Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore** (6.0.1): EF Core database context checks

**Health Check Types**:

The application likely implements multiple health checks:

1. **SQL Server Connectivity**: Verifies database connection
   - Tests connection to the `MoviesConnectionString` database
   - Validates SQL Server is accessible and responsive

2. **Entity Framework Core Context**: Validates EF Core DbContext
   - Ensures database schema is accessible
   - Verifies migrations are applied

**Health Check Response Format**:

Health check endpoints typically return JSON responses:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "SqlServer": {
      "status": "Healthy",
      "duration": "00:00:00.0100000"
    },
    "EntityFrameworkCore": {
      "status": "Healthy",
      "duration": "00:00:00.0023456"
    }
  }
}
```

**Health Check Status Codes**:

| Status | HTTP Code | Description |
|--------|-----------|-------------|
| Healthy | 200 | All checks passed |
| Degraded | 200 | Some checks passed with warnings |
| Unhealthy | 503 | One or more checks failed |

**Usage Scenarios**:

1. **Container Orchestration**: Kubernetes/Docker Swarm liveness and readiness probes
2. **Load Balancers**: Backend health monitoring
3. **Monitoring Systems**: Automated health status polling
4. **DevOps Dashboards**: Real-time application health visualization

**Example Health Check Request**:

```bash
curl http://localhost:55644/health
```

**Production Considerations**:

1. **Security**: Consider authentication for health check endpoints in production
2. **Detailed vs. Simple**: Provide detailed health information for internal monitoring, simple status for external load balancers
3. **Performance**: Ensure health checks execute quickly (< 1 second)
4. **Dependencies**: Include checks for all critical dependencies (database, external APIs, cache)

---

## Configuration Best Practices

### Security

1. **Never commit secrets**: Use User Secrets for local development, environment variables or Azure Key Vault for production
2. **Placeholder values**: Replace all `[placeholder]` values before deployment
3. **Connection strings**: Use managed identities or Azure SQL authentication in production
4. **CORS policy**: Restrict `AllowedHosts` and CORS origins in production

### Environment Management

1. **Environment variables**: Use environment variables for environment-specific overrides
2. **Configuration validation**: Implement startup validation for required configuration values
3. **Default values**: Provide sensible defaults in `appsettings.json`
4. **Documentation**: Document all configuration properties and their expected values

### Deployment

1. **Docker**: Use `appsettings.docker.json` for container-specific settings
2. **Azure**: Leverage Azure App Configuration for centralized configuration management
3. **CI/CD**: Inject configuration values during deployment pipeline
4. **Monitoring**: Enable Application Insights in production environments

### Troubleshooting

**Configuration not loading**:
- Verify file is set to "Copy to Output Directory"
- Check `ASPNETCORE_ENVIRONMENT` variable is set correctly
- Ensure JSON syntax is valid (no trailing commas, proper quotes)

**Environment variables not working**:
- Verify double underscore (`__`) separator syntax
- Check environment variable is set before application starts
- Use `IConfiguration.GetDebugView()` to inspect configuration sources

**Connection string issues**:
- Test connection string independently using SQL Server Management Studio
- Verify SQL Server is accessible from the application host
- Check firewall rules and network connectivity
- Ensure database exists and user has appropriate permissions