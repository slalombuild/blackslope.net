# Introduction

BlackSlope.NET is a modern enterprise-grade reference architecture developed by Slalom Build, demonstrating best practices for building production-ready RESTful APIs using .NET 6.0. This reference implementation serves as a comprehensive template for teams developing cloud-native applications with ASP.NET Core, providing a solid foundation that incorporates industry-standard patterns, security practices, and operational excellence.

## Learn More

For detailed background and implementation guidance, see the following blog posts:

- [Introducing BlackSlope: A DotNet Core Reference Architecture from Slalom Build](https://medium.com/slalom-build/introducing-black-slope-a-dotnet-core-reference-architecture-from-slalom-build-3f1452eb62ef)
- [BlackSlope: A Deeper Look at the Components of our DotNet Reference Architecture](https://medium.com/slalom-build/blackslope-a-deeper-look-at-the-components-of-our-dotnet-reference-architecture-b7b3a9d6e43b)
- [BlackSlope in Action: A Guide to Using our DotNet Reference Architecture](https://medium.com/slalom-build/blackslope-in-action-a-guide-to-using-our-dotnet-reference-architecture-d1e41eea8024)

## Overview

BlackSlope.NET represents a battle-tested approach to building scalable, maintainable web APIs. The architecture emphasizes clean code principles, separation of concerns, and comprehensive middleware pipelines that handle cross-cutting concerns such as authentication, logging, exception handling, and request correlation.

The solution is structured around two primary application types:

- **BlackSlope.Api**: A full-featured ASP.NET Core Web API application providing RESTful endpoints
- **RenameUtility**: A console application for command-line operations and background processing

Both applications share common infrastructure components through the `BlackSlope.Api.Common` library, ensuring consistency across the solution.

### Solution Structure

```
BlackSlope.NET/
├── src/
│   ├── BlackSlope.Api/                    # Main Web API project
│   ├── BlackSlope.Api.Common/             # Shared infrastructure and utilities
│   ├── BlackSlope.Api.Tests/              # API unit tests
│   ├── BlackSlope.Api.Common.Tests/       # Common library unit tests
│   ├── BlackSlope.Hosts.ConsoleApp/       # Console application host
│   └── RenameUtility/                     # Utility tool for project renaming
├── Scripts/                                # Build and deployment scripts
└── Docker/                                 # Container configuration
```

## Key Features

### RESTful API Architecture

BlackSlope.NET implements a comprehensive RESTful API following HTTP standards and conventions:

- **Resource-based routing**: Endpoints organized around business entities (e.g., Movies)
- **HTTP verb semantics**: Proper use of GET, POST, PUT, DELETE operations
- **Status code conventions**: Appropriate HTTP status codes for different scenarios
- **Content negotiation**: Support for JSON serialization with System.Text.Json

The API structure follows a vertical slice architecture pattern, organizing code by feature rather than technical layer:

```csharp
// Example from Program.cs - Entry point configuration
public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseSerilog(Assembly.GetExecutingAssembly().GetName().Name);
            webBuilder.UseStartup<Startup>();
        });
}
```

### Azure AD Authentication Support

The application provides robust authentication capabilities through Azure Active Directory integration:

- **Modern authentication**: Azure.Identity (v1.14.2) for current Azure AD scenarios
- **Legacy support**: Microsoft.IdentityModel.Clients.ActiveDirectory (v5.2.9) for backward compatibility
- **JWT token handling**: Comprehensive JWT validation and generation using System.IdentityModel.Tokens.Jwt (v7.7.1)
- **Secure token management**: Microsoft.IdentityModel.JsonWebTokens (v7.7.1) for enhanced security

```csharp
// From Startup.cs - Authentication configuration
public void ConfigureServices(IServiceCollection services)
{
    // Azure AD authentication setup
    services.AddAzureAd(HostConfig.AzureAd);
    
    // Additional authentication middleware
    app.UseAuthentication();
}
```

### Entity Framework Core with SQL Server

Data access is implemented using Entity Framework Core 6.0.1 with SQL Server:

- **Code-first migrations**: Full support for database schema versioning
- **Connection resilience**: Built-in retry logic for transient failures
- **Performance optimization**: Includes caching strategies with Microsoft.Extensions.Caching.Memory
- **Health monitoring**: EF Core health checks for database connectivity validation

Database setup process:

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update --project=.\src\BlackSlope.Api\BlackSlope.Api.csproj
```

### Docker Containerization

BlackSlope.NET is container-ready with comprehensive Docker support:

- **Windows containers**: Default target OS configured for Windows-based deployments
- **Multi-stage builds**: Optimized Dockerfile for production deployments
- **Visual Studio integration**: Microsoft.VisualStudio.Azure.Containers.Tools.Targets (v1.14.0)
- **Docker Compose**: Orchestration support for multi-container scenarios

```dockerfile
# Example Docker workflow
docker build -t blackslope.api -f Dockerfile .
docker create --name blackslope-container blackslope.api
docker start blackslope-container
```

### Comprehensive Middleware Pipeline

The application implements a sophisticated middleware pipeline handling multiple cross-cutting concerns:

```csharp
// From Startup.cs - Middleware configuration
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

    // Health check endpoints
    HealthCheckStartup.Configure(app, env, HostConfig);
    
    app.UseHttpsRedirection();
    app.UseSwagger(HostConfig.Swagger);
    app.UseRouting();
    app.UseCors("AllowSpecificOrigin");
    app.UseAuthentication();
    
    // Custom middleware
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

**Middleware Components:**

1. **CorrelationIdMiddleware**: Tracks requests across distributed systems
2. **ExceptionHandlingMiddleware**: Centralized exception handling and logging
3. **HTTPS Redirection**: Enforces secure connections
4. **CORS Policy**: Configurable cross-origin resource sharing
5. **Authentication/Authorization**: Azure AD integration
6. **Health Checks**: Database and application health monitoring

### Automated Testing Infrastructure

The solution includes comprehensive testing support:

- **Unit Tests**: Separate test projects for API and Common libraries (`BlackSlope.Api.Tests` and `BlackSlope.Api.Common.Tests`)
- **Integration Tests**: Two SpecFlow-based integration test projects (`BlackSlope.Api.Tests.IntegrationTests` using `System.Net.Http.HttpClient` and `BlackSlope.Api.Tests.RestSharpIntegrationTests` using RestSharp) have been removed from the Solution until SpecFlow adds support for .NET 6
- **Test Organization**: Clear separation between unit and integration test concerns

```bash
# Run all tests
dotnet test ./src/
```

### API Documentation with Swagger

Interactive API documentation is provided through Swashbuckle.AspNetCore.SwaggerUI (v6.3.0):

- **OpenAPI specification**: Auto-generated API documentation
- **Interactive testing**: Try-it-out functionality for all endpoints
- **XML documentation**: Inline code comments included in Swagger UI

Access Swagger UI at: `http://localhost:51385/swagger`

### Resilience Patterns with Polly

HTTP client resilience is implemented using Polly (v7.2.2):

- **Retry policies**: Automatic retry for transient failures
- **Circuit breaker**: Prevents cascading failures
- **Timeout policies**: Request timeout management
- **HttpClientFactory integration**: Proper HttpClient lifecycle management

```csharp
// From Startup.cs - HttpClient with Polly configuration
services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
services.AddHttpClient("movies", (provider, client) => 
    provider.GetRequiredService<IHttpClientDecorator>().Configure(client));
```

### Code Quality & Analysis

BlackSlope.NET enforces code quality through multiple analyzers:

**StyleCop.Analyzers (v1.1.118):**
- Code style consistency
- Naming conventions
- Documentation requirements
- Configured via `stylecop.json`

Currently suppressed StyleCop rules:
- SA1101: Prefix local calls with this
- SA1309: Field names should not begin with an underscore
- SA1600: Elements should be documented
- SA1614: Element parameter documentation must have text
- SA1616: Element return value documentation must have text
- SA1629: Documentation text should end with a period
- SA1633: File should have header

**Microsoft.CodeAnalysis.NetAnalyzers (v6.0.0):**
- Code quality analysis
- Security vulnerability detection
- Performance recommendations
- Configured via `.editorconfig`

Currently suppressed CodeAnalysis rules:
- CA1031: Do not catch general exception types (scoped to `ExceptionHandlingMiddleware.Invoke`)
- CA1710: Identifiers should have correct suffix (scoped to `CompositeValidator<T>`)

All suppressed rules are documented in `BlackSlope.Api.Common.GlobalSuppressions` for transparency.

## Architecture Philosophy

### Clean Architecture Principles

BlackSlope.NET adheres to clean architecture principles, ensuring:

1. **Independence from frameworks**: Business logic is not coupled to ASP.NET Core
2. **Testability**: All components can be tested in isolation
3. **Independence from UI**: API layer is separate from business logic
4. **Independence from database**: Repository pattern abstracts data access
5. **Independence from external agencies**: External services are abstracted behind interfaces

### Separation of Concerns

The solution demonstrates clear separation through:

- **Vertical slicing**: Features organized by business capability (e.g., Movies operations)
- **Layered architecture**: Presentation, business logic, and data access layers
- **Shared kernel**: Common infrastructure in `BlackSlope.Api.Common`

```csharp
// From Startup.cs - Service registration demonstrates separation
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvcService();           // Presentation layer
    services.AddMovieService();         // Business logic layer
    services.AddMovieRepository(_configuration);  // Data access layer
    services.AddValidators();           // Cross-cutting concerns
}
```

### Dependency Injection

Comprehensive dependency injection throughout the application:

- **Constructor injection**: Primary DI pattern used
- **Service lifetimes**: Appropriate scoping (Transient, Scoped, Singleton)
- **Interface-based design**: All dependencies injected via interfaces
- **Extension methods**: Clean service registration patterns

```csharp
// Example service registration patterns
services.AddTransient<IFileSystem, FileSystem>();
services.AddTransient<IVersionService, AssemblyVersionService>();
services.AddTransient<IHttpClientDecorator, HttpClientDecorator>();
```

### SOLID Principles

The codebase demonstrates SOLID principles:

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Extension through interfaces and abstract classes
- **Liskov Substitution**: Interfaces enable polymorphic behavior
- **Interface Segregation**: Focused, role-based interfaces
- **Dependency Inversion**: High-level modules depend on abstractions

### Configuration Management

Flexible configuration through multiple sources:

- **appsettings.json**: Base configuration
- **Environment-specific settings**: appsettings.{Environment}.json
- **User Secrets**: Secure local development (UserSecretsId: eeaaec3a-f784-4d04-8b1d-8fe6d9637231)
- **Environment variables**: Production configuration
- **Azure Key Vault**: Secure cloud configuration (via Azure.Identity)

```csharp
// From Startup.cs - Configuration binding
private void ApplicationConfiguration(IServiceCollection services)
{
    services.AddSingleton(_ => _configuration);
    services.AddSingleton(_configuration
        .GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<HostConfig>());
}
```

## Getting Started

To begin working with BlackSlope.NET, proceed to the following documentation:

- [Prerequisites](/getting_started/prerequisites.md) - Required tools and software
- [Installation](/getting_started/installation.md) - Step-by-step setup guide
- [Architecture Overview](/architecture/overview.md) - Detailed architectural documentation

## Technology Stack Summary

| Category | Technologies |
|----------|-------------|
| **Runtime** | .NET 6.0 |
| **Web Framework** | ASP.NET Core 6.0 |
| **Database** | Microsoft SQL Server |
| **ORM** | Entity Framework Core 6.0.1 |
| **Authentication** | Azure AD, JWT |
| **API Documentation** | Swagger/OpenAPI |
| **Resilience** | Polly 7.2.2 |
| **Mapping** | AutoMapper 10.1.1 |
| **Validation** | FluentValidation 10.3.6 |
| **Containerization** | Docker (Windows) |
| **Code Analysis** | StyleCop, .NET Analyzers |

## Design Patterns Implemented

BlackSlope.NET demonstrates several key design patterns:

- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Inversion of control
- **Middleware Pattern**: Request/response pipeline
- **Factory Pattern**: HttpClientFactory for HTTP clients
- **Strategy Pattern**: Versioning services (Assembly vs. JSON)
- **Decorator Pattern**: HttpClientDecorator for client configuration
- **Options Pattern**: Strongly-typed configuration

## Next Steps

After reviewing this introduction, developers should:

1. Review the [Prerequisites](/getting_started/prerequisites.md) to ensure their development environment is properly configured
2. Follow the [Installation Guide](/getting_started/installation.md) to set up the application locally
3. Study the [Architecture Overview](/architecture/overview.md) to understand the system design in depth
4. Explore the codebase starting with `Startup.cs` to understand service registration and middleware configuration
5. Review the example Movie operations to understand the vertical slice architecture pattern

BlackSlope.NET provides a production-ready foundation for building enterprise applications, incorporating industry best practices and modern development patterns. The reference architecture is designed to be adapted and extended for specific business requirements while maintaining code quality and architectural integrity.