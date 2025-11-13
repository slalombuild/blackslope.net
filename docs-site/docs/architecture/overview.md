# Architecture Overview

## High-Level Architecture

BlackSlope.NET is a reference architecture implementing a **multi-tier application structure** built on .NET 6.0. The application follows **Clean Architecture principles**, separating concerns into distinct layers with well-defined boundaries and dependencies flowing inward toward the core business logic.

### Architectural Layers

The application is organized into the following architectural layers:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│              (BlackSlope.Api - Controllers)              │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│         (Services, Validators, AutoMapper Profiles)      │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                  │
│    (Repositories, EF Core, External API Integrations)    │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│                      Data Layer                          │
│              (SQL Server via EF Core 6.0)                │
└─────────────────────────────────────────────────────────┘
```

### Key Architectural Characteristics

- **RESTful API Design**: Exposes HTTP endpoints following REST principles
- **Dependency Injection**: Utilizes ASP.NET Core's built-in DI container throughout
- **Middleware Pipeline**: Implements cross-cutting concerns via middleware components
- **Health Monitoring**: Integrated health checks for database and application status
- **API Documentation**: Auto-generated Swagger/OpenAPI documentation
- **Authentication & Authorization**: Azure AD integration with JWT bearer tokens
- **Resilience Patterns**: Polly-based retry and circuit breaker policies
- **Containerization**: Docker support with Windows containers

## Project Structure

The solution is organized into a logical folder structure that separates concerns and promotes maintainability. The solution file (`BlackSlope.NET.sln`) defines the following organization:

### Solution Folders

```
BlackSlope.NET/
├── Api/
│   └── BlackSlope.Api                    # Main Web API project
├── Common/
│   ├── BlackSlope.Api.Common             # Shared infrastructure & utilities
│   └── BlackSlope.Api.Common.Tests       # Common library unit tests
├── Hosts/
│   └── BlackSlope.Hosts.ConsoleApp       # Console application host
├── Tools/
│   └── RenameUtility                     # Project renaming utility
├── Tests/
│   └── BlackSlope.Api.Tests              # API unit tests
├── Common/
│   └── BlackSlope.Api.Common.Tests       # Common library unit tests (nested under Common)
├── Docker/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── .dockerignore
└── Scripts/
    ├── build.sh
    ├── db-update.sh
    ├── docker-image-build.sh
    └── docker-container-run.sh
```

### Core Projects

#### BlackSlope.Api

The **main Web API project** that serves as the application entry point. This project:

- Hosts the ASP.NET Core web application
- Contains API controllers and endpoints
- Defines the application startup configuration
- Implements the middleware pipeline
- Manages dependency registration

**Key Files:**

```csharp
// Program.cs - Application entry point
public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            // Custom Serilog configuration extension
            webBuilder.UseSerilog(Assembly.GetExecutingAssembly().GetName().Name);
            webBuilder.UseStartup<Startup>();
        });
}
```

The `Program.cs` file uses the traditional `IHostBuilder` pattern with custom Serilog integration for structured logging via an extension method from `BlackSlope.Api.Common.Extensions`.

#### BlackSlope.Api.Common

The **shared infrastructure library** containing reusable components, utilities, and cross-cutting concerns:

- **Configuration Models**: Strongly-typed configuration classes
- **Middleware Components**: Custom middleware for correlation IDs, exception handling
- **Extensions**: Service collection and application builder extensions
- **Validators**: FluentValidation validators
- **Services**: Common service implementations
- **Versioning**: API versioning infrastructure

This project is referenced by both the API and console application, promoting code reuse and consistency.

#### BlackSlope.Hosts.ConsoleApp

A **console application** that shares core infrastructure with the Web API. This demonstrates how to:

- Reuse authentication and data access components
- Implement background processing or batch jobs
- Share business logic across different host types

#### RenameUtility

A **command-line tool** for renaming the BlackSlope template to your project name. This utility:

- Performs bulk file and content renaming
- Updates namespaces and project references
- Helps bootstrap new projects from the template

### Test Projects

The solution includes comprehensive test coverage:

- **BlackSlope.Api.Tests**: Unit tests for API controllers and services
- **BlackSlope.Api.Common.Tests**: Unit tests for shared infrastructure components

**Note**: Integration test projects using SpecFlow have been temporarily removed pending .NET 6 support updates.

## Key Design Patterns

BlackSlope.NET implements several industry-standard design patterns to promote maintainability, testability, and scalability.

### Repository Pattern

The **Repository Pattern** abstracts data access logic, providing a collection-like interface for domain entities. This pattern:

- Decouples business logic from data access implementation
- Enables easier unit testing through interface mocking
- Centralizes data access logic and query construction

**Implementation Example:**

```csharp
// Repository interface definition
public interface IMovieRepository
{
    Task<Movie> GetByIdAsync(int id);
    Task<IEnumerable<Movie>> GetAllAsync();
    Task<Movie> AddAsync(Movie movie);
    Task UpdateAsync(Movie movie);
    Task DeleteAsync(int id);
}

// Repository registration in Startup.cs
services.AddMovieRepository(_configuration);
```

Repositories are registered in the DI container and injected into services that require data access.

### Service Pattern

The **Service Pattern** encapsulates business logic and orchestrates operations across multiple repositories or external services. Services:

- Implement business rules and validation
- Coordinate transactions across multiple repositories
- Transform domain entities to DTOs using AutoMapper
- Handle cross-cutting concerns like logging and caching

**Registration Example:**

```csharp
// Service registration in Startup.cs
services.AddMovieService();
```

Services are typically injected into controllers and contain the core business logic of the application.

### Dependency Injection Throughout

The application leverages **ASP.NET Core's built-in Dependency Injection** container extensively:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // MVC and JSON serialization
    services.AddMvcService();
    
    // Configuration binding
    ApplicationConfiguration(services);
    
    // CORS policies
    CorsConfiguration(services);
    
    // Swagger/OpenAPI
    services.AddSwagger(HostConfig.Swagger);
    
    // Azure AD authentication
    services.AddAzureAd(HostConfig.AzureAd);
    
    // AutoMapper profiles
    services.AddAutoMapper(GetAssembliesToScanForMapperProfiles());
    
    // Correlation ID middleware
    services.AddCorrelation();
    
    // File system abstraction (for testability)
    services.AddTransient<IFileSystem, FileSystem>();
    
    // Versioning service
    services.AddTransient<IVersionService, AssemblyVersionService>();
    
    // API controllers
    services.AddMvcApi();
    
    // Business services
    services.AddMovieService();
    
    // Data repositories
    services.AddMovieRepository(_configuration);
    services.AddFakeApiRepository();
    
    // HTTP client with Polly policies
    services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
    services.AddHttpClient("movies", (provider, client) => 
        provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
    
    // Health checks
    HealthCheckStartup.ConfigureServices(services, _configuration);
    
    // FluentValidation validators
    services.AddValidators();
}
```

**Key DI Patterns:**

- **Transient Services**: Created each time they're requested (e.g., `IFileSystem`, `IVersionService`)
- **Scoped Services**: Created once per HTTP request (e.g., repositories, DbContext)
- **Singleton Services**: Created once for the application lifetime (e.g., `IConfiguration`, `IMapper`)

### Middleware Pipeline Architecture

The application implements a **middleware pipeline** for processing HTTP requests. Middleware components execute in order, each having the opportunity to:

- Process the incoming request
- Short-circuit the pipeline
- Pass control to the next middleware
- Process the outgoing response

**Pipeline Configuration:**

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Development-specific error handling
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // Production HSTS for security
        app.UseHsts();
    }
    
    // Health check endpoints
    HealthCheckStartup.Configure(app, env, HostConfig);
    
    // HTTPS redirection
    app.UseHttpsRedirection();
    
    // Swagger UI
    app.UseSwagger(HostConfig.Swagger);
    
    // Routing middleware
    app.UseRouting();
    
    // CORS policy
    app.UseCors("AllowSpecificOrigin");
    
    // Authentication (validates JWT tokens)
    app.UseAuthentication();
    
    // Custom correlation ID middleware
    app.UseMiddleware<CorrelationIdMiddleware>();
    
    // Custom exception handling middleware
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    // Endpoint execution
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

**Middleware Execution Order:**

1. **Exception Handling**: Development error page or HSTS
2. **Health Checks**: Database and application health endpoints
3. **HTTPS Redirection**: Enforces HTTPS
4. **Swagger**: API documentation UI
5. **Routing**: Matches incoming requests to endpoints
6. **CORS**: Cross-origin resource sharing policy
7. **Authentication**: JWT token validation
8. **CorrelationIdMiddleware**: Adds correlation IDs for request tracking
9. **ExceptionHandlingMiddleware**: Global exception handling and logging
10. **Endpoint Execution**: Controller action execution

**Important Considerations:**

- **Order Matters**: Middleware executes in the order registered. Authentication must come before authorization, routing before endpoints, etc.
- **Short-Circuiting**: Middleware can terminate the pipeline early (e.g., authentication failures, health check responses)
- **Exception Handling**: The `ExceptionHandlingMiddleware` is positioned late in the pipeline to catch exceptions from downstream components

### AutoMapper Integration

**AutoMapper** is used for object-to-object mapping, particularly for transforming domain entities to DTOs:

```csharp
// AutoMapper configuration in Startup.cs
services.AddAutoMapper(GetAssembliesToScanForMapperProfiles());

private static IEnumerable<Assembly> GetAssembliesToScanForMapperProfiles() =>
    new Assembly[] { Assembly.GetExecutingAssembly() };
```

The configuration scans specified assemblies for `Profile` classes that define mapping configurations. This approach:

- Eliminates repetitive mapping code
- Centralizes mapping logic
- Supports complex transformations and custom resolvers
- Improves maintainability

### Polly Resilience Patterns

The application integrates **Polly** for implementing resilience patterns on HTTP client calls:

```csharp
// HTTP client with Polly policies
services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
services.AddHttpClient("movies", (provider, client) => 
    provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
```

The `IHttpClientDecorator` can configure:

- **Retry Policies**: Automatically retry failed requests with exponential backoff
- **Circuit Breaker**: Prevent cascading failures by temporarily blocking requests to failing services
- **Timeout Policies**: Enforce maximum wait times for responses
- **Fallback Policies**: Provide alternative responses when services are unavailable

**Example Polly Configuration:**

```csharp
public class HttpClientDecorator : IHttpClientDecorator
{
    public void Configure(HttpClient client)
    {
        // Configure base address, headers, timeout, etc.
        client.BaseAddress = new Uri("https://api.example.com");
        client.Timeout = TimeSpan.FromSeconds(30);
    }
}
```

For detailed Polly policy configuration, refer to the `FakeApiRepository` implementation.

### FluentValidation

**FluentValidation** provides strongly-typed validation rules for request models:

```csharp
// Validator registration
services.AddValidators();
```

This pattern:

- Separates validation logic from business logic
- Provides expressive, readable validation rules
- Integrates with ASP.NET Core model validation
- Supports complex validation scenarios and custom validators

## Component Interaction

### Request Flow

A typical HTTP request flows through the application as follows:

```
1. HTTP Request arrives
   ↓
2. Middleware Pipeline (HTTPS, CORS, Auth, Correlation, Exception Handling)
   ↓
3. Routing matches request to Controller action
   ↓
4. Model Binding and Validation (FluentValidation)
   ↓
5. Controller invokes Service
   ↓
6. Service applies business logic, calls Repository
   ↓
7. Repository queries database via EF Core
   ↓
8. Results mapped to DTOs via AutoMapper
   ↓
9. Response serialized to JSON
   ↓
10. Middleware processes response (logging, correlation headers)
   ↓
11. HTTP Response returned to client
```

### Cross-Cutting Concerns

Several cross-cutting concerns are handled consistently across the application:

- **Logging**: Serilog structured logging configured in `Program.cs`
- **Correlation IDs**: `CorrelationIdMiddleware` adds unique identifiers to requests for distributed tracing
- **Exception Handling**: `ExceptionHandlingMiddleware` provides global exception handling with appropriate HTTP status codes
- **Health Checks**: Dedicated endpoints for monitoring application and database health
- **Authentication**: JWT bearer token validation via Azure AD integration
- **API Documentation**: Swagger/OpenAPI automatically generated from controller attributes and XML comments

## Configuration Management

The application uses **strongly-typed configuration** bound from `appsettings.json`:

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

**Configuration Hierarchy:**

- `appsettings.json`: Base configuration
- `appsettings.{Environment}.json`: Environment-specific overrides
- **User Secrets**: Local development secrets (not committed to source control)
- **Environment Variables**: Production secrets and Azure configuration

**Key Configuration Sections:**

- **AzureAd**: Azure Active Directory authentication settings
- **Swagger**: API documentation configuration
- **ConnectionStrings**: Database connection strings
- **Logging**: Log level and provider configuration

## Related Documentation

For more detailed information on specific architectural components, refer to:

- [Project Structure](/architecture/project_structure.md) - Detailed breakdown of solution organization
- [Dependency Injection](/architecture/dependency_injection.md) - Comprehensive DI patterns and service registration
- [Middleware Pipeline](/architecture/middleware_pipeline.md) - In-depth middleware implementation details
- [Getting Started](/getting_started/introduction.md) - Setup and initial configuration guide

## Best Practices and Considerations

### Code Quality

The solution enforces code quality through:

- **StyleCop.Analyzers**: Enforces consistent code style (configured via `stylecop.json`)
- **Microsoft.CodeAnalysis.NetAnalyzers**: Provides code quality and security analysis
- **.editorconfig**: Defines coding conventions at the solution level

**Suppressed Rules:**

Certain rules are globally suppressed in `BlackSlope.Api.Common.GlobalSuppressions`:

- **SA1101**: Prefix local calls with `this` (suppressed for brevity)
- **SA1309**: Field names should not begin with underscore (allows `_fieldName` convention)
- **SA1600-SA1633**: Documentation requirements (relaxed for internal projects)
- **CA1031**: Do not catch general exception types (allowed in `ExceptionHandlingMiddleware`)

### Security Considerations

- **Azure AD Integration**: Provides enterprise-grade authentication
- **JWT Bearer Tokens**: Stateless authentication suitable for APIs
- **HTTPS Enforcement**: `UseHttpsRedirection()` redirects HTTP to HTTPS
- **HSTS**: HTTP Strict Transport Security enabled in production
- **CORS Configuration**: Currently allows any origin (⚠️ **TODO**: Restrict to specific frontend origins in production)

```csharp
// CORS configuration - REQUIRES PRODUCTION UPDATE
services.AddCors(options =>
{
    options.AddPolicy(
        "AllowSpecificOrigin",
        builder => builder.AllowAnyOrigin() // TODO: Replace with specific origins
            .AllowAnyHeader()
            .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE"));
});
```

### Performance Optimization

- **In-Memory Caching**: `Microsoft.Extensions.Caching.Memory` for frequently accessed data
- **Connection Pooling**: SQL Server connection pooling via `Microsoft.Data.SqlClient`
- **Async/Await**: Asynchronous operations throughout for improved scalability
- **Health Checks**: Proactive monitoring prevents cascading failures

### Docker Deployment

The application includes Docker support with **Windows containers** as the default target:

```dockerfile
# Dockerfile location: src/Dockerfile
# Build context: src/

# Build the Docker image
docker build -t blackslope.api -f Dockerfile .

# Create and run a container
docker create --name blackslope-container blackslope.api
docker start blackslope-container
```

**Docker Compose** is also available for orchestrating multi-container deployments.

### Testing Strategy

The architecture supports comprehensive testing:

- **Unit Tests**: Test individual components in isolation using mocking frameworks
- **Integration Tests**: Test component interactions (SpecFlow support pending .NET 6 update)
- **Health Checks**: Automated monitoring of application health in production

### Migration and Database Management

Entity Framework Core migrations manage database schema:

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update --project=.\src\BlackSlope.Api\BlackSlope.Api.csproj
```

Migrations are stored in the `BlackSlope.Api` project and applied automatically during deployment or manually via the CLI.

---

This architecture provides a solid foundation for building enterprise-grade APIs with .NET 6.0, emphasizing maintainability, testability, and scalability. The clean separation of concerns and extensive use of industry-standard patterns make the codebase approachable for new team members while providing the flexibility needed for complex business requirements.