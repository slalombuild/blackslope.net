# Project Structure

## Solution Organization

The BlackSlope solution is organized into a multi-project architecture that follows separation of concerns and clean architecture principles. The solution file (`BlackSlope.NET.sln`) defines the following logical groupings:

### Solution Folders

| Folder | Purpose | Projects |
|--------|---------|----------|
| **Api** | Core API implementation | `BlackSlope.Api` |
| **Common** | Shared infrastructure and utilities | `BlackSlope.Api.Common`, `BlackSlope.Api.Common.Tests` |
| **Hosts** | Application entry points | `BlackSlope.Hosts.ConsoleApp` |
| **Tests** | Test projects | `BlackSlope.Api.Tests`, `BlackSlope.Api.IntegrationTests` |
| **Tools** | Development utilities | `RenameUtility` |
| **Scripts** | Build and deployment automation | Shell scripts for build, Docker, and database operations |
| **Docker** | Containerization configuration | Dockerfile, docker-compose.yml, .dockerignore |

### Build Configurations

The solution supports three build configurations:
- **Debug**: Development builds with full debugging symbols
- **Development**: Intermediate configuration for development environment testing
- **Release**: Optimized production builds

All projects target **.NET 6.0** (`net6.0` framework) and are configured to build across all three configurations.

### Project Dependencies

```
BlackSlope.Api
├── BlackSlope.Api.Common (Project Reference)
└── Multiple NuGet packages

BlackSlope.Api.Tests
└── BlackSlope.Api (Project Reference)

BlackSlope.Api.Common.Tests
└── BlackSlope.Api.Common (Project Reference)

BlackSlope.Hosts.ConsoleApp
└── (Independent console application)

RenameUtility
└── (Independent utility tool)
```

## BlackSlope.Api Project

The `BlackSlope.Api` project is the primary web API application built using **ASP.NET Core 6.0** with the `Microsoft.NET.Sdk.Web` SDK. This project serves as the main entry point for the RESTful API services.

### Project Configuration

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <DockerDefaultTargetOS>Windows</DockerDefaultTargetOS>
  <UserSecretsId>eeaaec3a-f784-4d04-8b1d-8fe6d9637231</UserSecretsId>
</PropertyGroup>
```

**Key Configuration Details:**
- **Docker Support**: Configured for Windows containers by default
- **User Secrets**: Enabled for secure local development configuration (ID: `eeaaec3a-f784-4d04-8b1d-8fe6d9637231`)
- **XML Documentation**: Generated for both Debug and Release configurations
  - **Note**: The project targets .NET 6.0 (`net6.0`) but the documentation output path is configured for `net5.0` (this appears to be a legacy configuration that should be updated to `net6.0`)
- **Warning Suppression**: CS1591 (missing XML comments) suppressed to allow gradual documentation adoption

### Controllers and Operations

The API follows an operation-based organization pattern rather than traditional CRUD controllers. The project structure includes:

```
BlackSlope.Api/
├── Operations/
│   └── Movies/
│       └── Requests/  (placeholder for request DTOs)
```

This structure suggests the API implements a **CQRS-like pattern** where operations are organized by domain entities (e.g., Movies) with separate request/response models. The empty `Requests` folder indicates this is a template or the requests are defined elsewhere (likely in the Common project).

**Design Pattern**: The operation-based approach provides:
- Clear separation between commands and queries
- Easier unit testing of individual operations
- Better alignment with business use cases
- Simplified API versioning and evolution

### Services Layer

The services layer implements business logic and orchestrates interactions between controllers and repositories. Services typically:
- Validate business rules using FluentValidation
- Transform data using AutoMapper
- Implement caching strategies with `IMemoryCache`
- Handle transient faults using Polly resilience policies
- Coordinate database operations through repositories

**Service Registration Pattern**: Services are registered in the dependency injection container with appropriate lifetimes:
```csharp
// Typical service registration in Startup.cs or Program.cs
services.AddScoped<IMovieService, MovieService>();
services.AddSingleton<ICacheService, MemoryCacheService>();
services.AddHttpClient<IExternalApiService, ExternalApiService>()
    .AddPolicyHandler(GetRetryPolicy());
```

### Repositories Layer

The repository layer abstracts data access using **Entity Framework Core 6.0.1** with SQL Server provider. Key characteristics:

**Database Provider Configuration**:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="6.0.1">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

**Repository Pattern Benefits**:
- Abstraction over EF Core DbContext for easier testing
- Centralized query logic and data access patterns
- Support for unit of work pattern
- Simplified migration to alternative data stores if needed

**Typical Repository Interface**:
```csharp
public interface IMovieRepository
{
    Task<Movie> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Movie>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Movie> AddAsync(Movie movie, CancellationToken cancellationToken = default);
    Task UpdateAsync(Movie movie, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
```

### Configuration Files

The project uses multiple configuration sources:

1. **appsettings.json**: Base configuration for all environments
2. **appsettings.{Environment}.json**: Environment-specific overrides
3. **User Secrets**: Local development secrets (connection strings, API keys)
4. **Environment Variables**: Production configuration in Azure/Docker

**Configuration Loading Order** (later sources override earlier):
```
appsettings.json → appsettings.{Environment}.json → User Secrets → Environment Variables
```

### Health Checks

The API implements comprehensive health monitoring:

```xml
<PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="5.0.3" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="6.0.1" />
```

**Health Check Endpoints** (typical configuration):
- `/health`: Overall application health
- `/health/ready`: Readiness probe for Kubernetes/container orchestration
- `/health/live`: Liveness probe for container health

**Monitored Components**:
- SQL Server database connectivity
- Entity Framework Core DbContext availability
- External service dependencies (via custom health checks)

### Resilience and HTTP Client Configuration

The API uses **Polly** for implementing resilience patterns:

```xml
<PackageReference Include="Polly" Version="7.2.2" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="6.0.1" />
```

**Common Resilience Patterns Implemented**:
- **Retry Policy**: Automatic retry for transient HTTP failures
- **Circuit Breaker**: Prevents cascading failures to downstream services
- **Timeout Policy**: Ensures requests don't hang indefinitely
- **Bulkhead Isolation**: Limits concurrent requests to protect resources

**Example Polly Configuration**:
```csharp
// Retry policy with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Circuit breaker policy
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Combine policies
services.AddHttpClient<IExternalService, ExternalService>()
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);
```

### Code Quality and Analysis

The project enforces code quality through multiple analyzers:

```xml
<PackageReference Include="StyleCop.Analyzers" Version="1.1.118">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="6.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**StyleCop Configuration**: The project includes a `stylecop.json` file (marked as `AdditionalFiles`) that defines coding standards:
- Naming conventions
- Documentation requirements
- Code organization rules
- Formatting standards

**Gotcha**: StyleCop warnings are treated as errors in Release builds but may be suppressed in Debug builds for faster development iteration.

## BlackSlope.Api.Common Project

The `BlackSlope.Api.Common` project is a class library (`Microsoft.NET.Sdk`) that provides shared infrastructure components, middleware, and utilities used across the solution.

### Project Configuration

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>1591</NoWarn>
</PropertyGroup>
```

**Key Features**:
- **Framework Reference**: Includes `Microsoft.AspNetCore.App` for ASP.NET Core functionality without being a web project
- **XML Documentation**: Automatically generated for IntelliSense support
- **Reusable Components**: Designed for sharing across multiple API projects

### Shared Infrastructure Components

The Common project provides foundational infrastructure:

#### Authentication and Authorization

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.1" />
<PackageReference Include="Azure.Identity" Version="1.14.2" />
```

**JWT Authentication Infrastructure**:
- Base JWT bearer authentication configuration
- Token validation parameters
- Claims transformation logic
- Azure AD integration support

**Typical Base Authentication Configuration**:
```csharp
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
                };
            });
        
        return services;
    }
}
```

#### Logging Infrastructure

```xml
<PackageReference Include="Serilog.AspNetCore" Version="4.1.0" />
<PackageReference Include="Serilog.Settings.Configuration" Version="3.3.0" />
<PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="3.1.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="4.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

**Serilog Configuration**: The Common project provides centralized logging setup with multiple sinks:
- **Console Sink**: Development debugging and container logs
- **File Sink**: Persistent local logging with rolling file support
- **Application Insights Sink**: Azure monitoring and telemetry

**Structured Logging Pattern**:
```csharp
public static class LoggingExtensions
{
    public static IHostBuilder UseCommonLogging(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.ApplicationInsights(
                    services.GetRequiredService<TelemetryConfiguration>(),
                    TelemetryConverter.Traces);
        });
    }
}
```

### Common Middleware

The Common project likely includes reusable middleware components:

**Typical Middleware Components**:
- **Exception Handling Middleware**: Global error handling and standardized error responses
- **Request Logging Middleware**: Structured logging of HTTP requests/responses
- **Correlation ID Middleware**: Request tracking across distributed services
- **Performance Monitoring Middleware**: Request duration and performance metrics

**Example Exception Handling Middleware**:
```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error occurred");
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleValidationExceptionAsync(
        HttpContext context, 
        ValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            errors = exception.Errors.Select(e => new
            {
                property = e.PropertyName,
                message = e.ErrorMessage
            })
        };
        
        return context.Response.WriteAsJsonAsync(response);
    }
}
```

### Base Controllers and Validators

The Common project provides base classes for consistent API behavior:

#### Base API Controller

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Value == null 
                ? NotFound() 
                : Ok(result.Value);
        }
        
        return result.Error switch
        {
            ValidationError => BadRequest(result.Error),
            NotFoundError => NotFound(result.Error),
            UnauthorizedError => Unauthorized(result.Error),
            _ => StatusCode(500, result.Error)
        };
    }
}
```

#### Base Validator

```xml
<PackageReference Include="FluentValidation" Version="10.3.6" />
```

**Note**: The Common project references `FluentValidation` directly, while the API project may use `FluentValidation.DependencyInjectionExtensions` for dependency injection integration.

**FluentValidation Base Classes**:
```csharp
public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected void ValidateId(Expression<Func<T, int>> expression)
    {
        RuleFor(expression)
            .GreaterThan(0)
            .WithMessage("ID must be greater than 0");
    }

    protected void ValidateRequiredString(
        Expression<Func<T, string>> expression, 
        int maxLength = 255)
    {
        RuleFor(expression)
            .NotEmpty()
            .MaximumLength(maxLength);
    }
}
```

### Versioning Infrastructure

The Common project provides API versioning support:

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="6.0.1" />
```

**API Versioning Configuration**:
```csharp
public static class VersioningExtensions
{
    public static IServiceCollection AddApiVersioningConfiguration(
        this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
```

### Swagger/OpenAPI Configuration

```xml
<PackageReference Include="Swashbuckle.AspNetCore.Swagger" Version="6.2.3" />
<PackageReference Include="Swashbuckle.AspNetCore.SwaggerGen" Version="6.2.3" />
<PackageReference Include="Swashbuckle.AspNetCore.SwaggerUI" Version="6.3.0" />
```

**Centralized Swagger Configuration**:
```csharp
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BlackSlope API",
                Version = "v1",
                Description = "RESTful API for BlackSlope application"
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            // JWT Authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
```

### File System Abstraction

```xml
<PackageReference Include="System.IO.Abstractions" Version="14.0.13" />
```

**Purpose**: Provides testable file system operations through the `IFileSystem` interface, enabling:
- Unit testing of file operations without actual file I/O
- Mocking file system behavior in tests
- Cross-platform file path handling

**Usage Pattern**:
```csharp
public class FileService
{
    private readonly IFileSystem _fileSystem;

    public FileService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<string> ReadFileAsync(string path)
    {
        return await _fileSystem.File.ReadAllTextAsync(path);
    }
}
```

### Data Mapping Configuration

```xml
<PackageReference Include="AutoMapper" Version="10.1.1" />
```

The Common project provides shared AutoMapper profiles:

```csharp
public class CommonMappingProfile : Profile
{
    public CommonMappingProfile()
    {
        // Common mappings used across all API projects
        CreateMap<DateTime, string>()
            .ConvertUsing(dt => dt.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        
        CreateMap<string, DateTime>()
            .ConvertUsing(s => DateTime.Parse(s));
    }
}
```

## Test Projects

The solution includes comprehensive test coverage through multiple test projects using different testing frameworks and approaches.

### BlackSlope.Api.Tests Project

**Unit Test Project Configuration**:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <IsPackable>false</IsPackable>
</PropertyGroup>
```

#### Testing Framework and Tools

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
<PackageReference Include="xunit" Version="2.4.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.3">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Testing Stack**:
- **xUnit**: Primary testing framework for unit tests
- **Visual Studio Test Runner**: Integration with Visual Studio Test Explorer
- **Test SDK**: Microsoft test platform for test discovery and execution

#### Test Utilities and Mocking

```xml
<PackageReference Include="Moq" Version="4.16.1" />
<PackageReference Include="AutoFixture" Version="4.17.0" />
```

**Moq Framework**: Used for creating mock objects and verifying interactions:
```csharp
[Fact]
public async Task GetMovieById_ReturnsMovie_WhenMovieExists()
{
    // Arrange
    var mockRepository = new Mock<IMovieRepository>();
    var expectedMovie = new Movie { Id = 1, Title = "Test Movie" };
    mockRepository
        .Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedMovie);
    
    var service = new MovieService(mockRepository.Object);
    
    // Act
    var result = await service.GetMovieByIdAsync(1);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedMovie.Id, result.Id);
    mockRepository.Verify(
        repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()), 
        Times.Once);
}
```

**AutoFixture**: Generates test data automatically:
```csharp
[Fact]
public void CreateMovie_ValidatesRequiredFields()
{
    // Arrange
    var fixture = new Fixture();
    var movie = fixture.Build<Movie>()
        .Without(m => m.Title) // Omit title to test validation
        .Create();
    
    var validator = new MovieValidator();
    
    // Act
    var result = validator.Validate(movie);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == nameof(Movie.Title));
}
```

#### Test Dependencies

The test project includes the same security and data packages as the main API to enable testing of:
- JWT token generation and validation
- SQL Server database interactions (for integration-style unit tests)
- Caching behavior
- JSON serialization/deserialization

**Project Reference**:
```xml
<ProjectReference Include="..\BlackSlope.Api\BlackSlope.Api.csproj" />
```

This reference allows testing of all public APIs and internal components (via `InternalsVisibleTo` attribute if configured).

### Unit Tests Structure

**Recommended Organization**:

```
BlackSlope.Api.Tests/
├── Controllers/
│   ├── MoviesControllerTests.cs
│   └── HealthControllerTests.cs
├── Services/
│   ├── MovieServiceTests.cs
│   └── CacheServiceTests.cs
├── Repositories/
│   ├── MovieRepositoryTests.cs
│   └── DatabaseContextTests.cs
├── Validators/
│   ├── MovieValidatorTests.cs
│   └── RequestValidatorTests.cs
├── Helpers/
│   ├── TestDataBuilder.cs
│   └── MockFactory.cs
└── Fixtures/
    └── DatabaseFixture.cs
```

**Test Naming Convention**:
```csharp
// Pattern: MethodName_StateUnderTest_ExpectedBehavior
[Fact]
public void GetMovieById_WithInvalidId_ThrowsArgumentException()
{
    // Test implementation
}

[Theory]
[InlineData(0)]
[InlineData(-1)]
[InlineData(int.MinValue)]
public void GetMovieById_WithNonPositiveId_ThrowsArgumentException(int invalidId)
{
    // Test implementation
}
```

### BlackSlope.Api.Common.Tests Project

**Unit Test Project Configuration**:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <IsPackable>false</IsPackable>
</PropertyGroup>
```

This project contains unit tests for the shared infrastructure components in the `BlackSlope.Api.Common` project, testing middleware, validators, extensions, and other common utilities.

**Project Reference**:
```xml
<ProjectReference Include="..\BlackSlope.Api.Common\BlackSlope.Api.Common.csproj" />
```

### Test Utilities and Helpers

**Common Test Helpers**:

```csharp
public static class TestDataBuilder
{
    public static Movie CreateTestMovie(int id = 1, string title = "Test Movie")
    {
        return new Movie
        {
            Id = id,
            Title = title,
            ReleaseYear = 2020,
            Director = "Test Director",
            Genre = "Action"
        };
    }

    public static List<Movie> CreateTestMovies(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestMovie(i, $"Test Movie {i}"))
            .ToList();
    }
}

public static class MockFactory
{
    public static Mock<IMovieRepository> CreateMovieRepositoryMock()
    {
        var mock = new Mock<IMovieRepository>();
        
        // Setup common behaviors
        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie>());
        
        return mock;
    }
}
```

**Database Fixture for xUnit**:
```csharp
public class DatabaseFixture : IDisposable
{
    public ApplicationDbContext Context { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=BlackSlope_Test;")
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}

[Collection("Database collection")]
public class MovieRepositoryTests
{
    private readonly DatabaseFixture _fixture;

    public MovieRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllMovies()
    {
        // Test implementation using _fixture.Context
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition]
}
```

### BlackSlope.Api.IntegrationTests Project

**Integration Test Project Configuration**:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
</PropertyGroup>
```

This project contains **acceptance tests** using **Behavior-Driven Development (BDD)** with SpecFlow, enabling testing of complete API workflows and integration scenarios.

#### Testing Framework and Tools

```xml
<PackageReference Include="SpecFlow" Version="3.9.40" />
<PackageReference Include="SpecFlow.Tools.MsBuild.Generation" Version="3.9.40" />
<PackageReference Include="SpecFlow.xUnit" Version="3.9.40" />
<PackageReference Include="xunit" Version="2.4.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.3" />
```

**Testing Stack**:
- **SpecFlow**: BDD framework for writing human-readable acceptance tests using Gherkin syntax
- **xUnit**: Test execution framework (SpecFlow integrates with xUnit as test runner)
- **Visual Studio Test Runner**: Integration with Visual Studio Test Explorer
- **SpecFlow.Tools.MsBuild.Generation**: Generates test code from `.feature` files during build

#### Test Utilities and Configuration

```xml
<PackageReference Include="AutoFixture" Version="4.17.0" />
<PackageReference Include="Microsoft.AspNetCore.Hosting.Abstractions" Version="2.2.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="6.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="6.0.0" />
```

**AutoFixture**: Generates test data for integration scenarios
**ASP.NET Core Hosting**: Enables in-memory test server hosting for integration testing
**Configuration**: Supports environment-specific test configuration via `appsettings.test.json`

**Test Configuration File**:
```xml
<Content Include="appsettings.test.json">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  <CopyToPublishDirectory>Always</CopyToPublishDirectory>
</Content>
```

#### Reporting Integration

```xml
<PackageReference Include="ReportPortal.SpecFlow" Version="3.2.1" />
```

**ReportPortal**: Provides centralized test execution reporting and analytics, enabling teams to:
- Track test execution history across builds
- Analyze failure trends and patterns
- Generate visual dashboards of test results
- Integrate with CI/CD pipelines for test monitoring

#### Project Structure

The project excludes certain folders from compilation:

```xml
<ItemGroup>
  <Compile Remove="Drivers\**" />
  <Compile Remove="Hooks\**" />
  <SpecFlowFeatureFiles Remove="Drivers\**" />
  <SpecFlowFeatureFiles Remove="Hooks\**" />
</ItemGroup>
```

**Note**: This configuration suggests that `Drivers\` and `Hooks\` folders may have been removed or relocated during project restructuring.

**Recommended Organization**:

```
BlackSlope.Api.IntegrationTests/
├── Features/
│   ├── Movies.feature
│   └── Authentication.feature
├── StepDefinitions/
│   ├── MovieSteps.cs
│   └── AuthenticationSteps.cs
├── Support/
│   ├── TestContext.cs
│   └── ApiClient.cs
├── appsettings.test.json
└── specflow.json
```

#### SpecFlow Feature Example

```gherkin
Feature: Movie Management
    As an API consumer
    I want to manage movies through the API
    So that I can create, read, update, and delete movie records

Scenario: Get all movies
    Given the API is running
    And there are 5 movies in the database
    When I send a GET request to "/api/v1/movies"
    Then the response status code should be 200
    And the response should contain 5 movies

Scenario: Create a new movie
    Given the API is running
    And I have a valid authentication token
    When I send a POST request to "/api/v1/movies" with:
        | Field       | Value           |
        | Title       | Inception       |
        | Director    | Christopher Nolan |
        | ReleaseYear | 2010            |
    Then the response status code should be 201
    And the response should contain the created movie
    And the movie should exist in the database
```

#### Step Definitions Example

```csharp
[Binding]
public class MovieSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly HttpClient _httpClient;
    private HttpResponseMessage _response;

    public MovieSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
    }

    [Given(@"there are (.*) movies in the database")]
    public async Task GivenThereAreMoviesInTheDatabase(int movieCount)
    {
        // Seed test database with movies
        var fixture = new Fixture();
        for (int i = 0; i < movieCount; i++)
        {
            var movie = fixture.Create<Movie>();
            await SeedMovieAsync(movie);
        }
    }

    [When(@"I send a GET request to ""(.*)""")]
    public async Task WhenISendAGetRequestTo(string endpoint)
    {
        _response = await _httpClient.GetAsync(endpoint);
        _scenarioContext["Response"] = _response;
    }

    [Then(@"the response status code should be (.*)")]
    public void ThenTheResponseStatusCodeShouldBe(int expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, (int)_response.StatusCode);
    }
}
```

**Project Reference**:
```xml
<ProjectReference Include="..\..\BlackSlope.Api\BlackSlope.Api.csproj" />
```

This reference allows integration tests to start the actual API application and test complete request/response workflows.

## Related Documentation

For more information about the architecture and testing strategies, see:

- [Architecture Overview](/architecture/overview.md) - High-level system architecture and design principles
- [Architecture Layers](/architecture/layers.md) - Detailed explanation of the layered architecture pattern
- [Unit Tests](/testing/unit_tests.md) - Comprehensive guide to writing and organizing unit tests

## Additional Projects

### BlackSlope.Hosts.ConsoleApp

A console application host for background processing, scheduled tasks, or command-line operations. This project shares the same infrastructure components (authentication, data access, logging) as the API but provides a different execution model.

**Typical Use Cases**:
- Database migrations and seeding
- Batch processing jobs
- Scheduled maintenance tasks
- Data import/export operations

### RenameUtility

A standalone utility tool for project renaming and refactoring operations. This tool is useful for:
- Creating new projects from the BlackSlope template
- Renaming namespaces and project files
- Updating configuration references

**Usage**: Typically invoked via the `rename.sh` script in the solution root.

## Build and Deployment Scripts

The solution includes several shell scripts for automation:

| Script | Purpose |
|--------|---------|
| `build.sh` | Compiles the solution and runs tests |
| `publish.sh` | Creates production-ready deployment packages |
| `docker-image-build.sh` | Builds Docker images for containerized deployment |
| `docker-container-run.sh` | Runs the application in Docker containers |
| `db-update.sh` | Applies Entity Framework migrations to the database |
| `run.sh` | Starts the application in development mode |
| `rename.sh` | Executes the RenameUtility for project customization |

**Docker Support**: The solution includes:
- `Dockerfile`: Multi-stage build configuration for optimized container images
- `docker-compose.yml`: Orchestration configuration for running the full stack
- `.dockerignore`: Excludes unnecessary files from Docker build context