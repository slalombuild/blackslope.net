# Version API

## Overview

The Version API provides a RESTful endpoint for retrieving application version information. This API is part of the `BlackSlope.Api.Common` shared library and can be integrated into any ASP.NET Core 6.0 web application within the solution. The endpoint returns structured version data that can be used for deployment verification, troubleshooting, and monitoring purposes.

**Key Features:**
- Simple HTTP GET endpoint for version retrieval
- Pluggable version service architecture supporting multiple version sources
- Standardized JSON response format
- Integration with Swagger/OpenAPI documentation
- HTTP 200 (OK) and 500 (Internal Server Error) response codes

**Related Documentation:**
- [Versioning Features](/features/versioning.md) - Comprehensive versioning strategy and implementation details
- [Health API Reference](/api_reference/health_api.md) - Related monitoring and diagnostics endpoints

## GET /api/version

### Endpoint Details

**HTTP Method:** `GET`  
**Route:** `/api/version`  
**Authentication:** Not required (public endpoint)  
**Content-Type:** `application/json`

### Request

This endpoint accepts no parameters, query strings, or request body.

```http
GET /api/version HTTP/1.1
Host: api.example.com
Accept: application/json
```

### Response Codes

| Status Code | Description | Scenario |
|-------------|-------------|----------|
| 200 OK | Successfully retrieved version information | Normal operation |
| 500 Internal Server Error | Failed to retrieve version information | File system errors, JSON parsing failures, or service exceptions |

### Response Schema

The endpoint returns a `Version` object with the following structure:

```json
{
  "version": "1.2.3.4"
}
```

**Response Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `version` | string | The build version number in semantic versioning or assembly version format |

### Example Response

```json
{
  "version": "1.0.0.0"
}
```

### Controller Implementation

The `VersionController` inherits from `BaseController` and follows standard ASP.NET Core MVC patterns:

```csharp
public class VersionController : BaseController
{
    private readonly IVersionService _versionService;

    public VersionController(IVersionService versionService)
    {
        _versionService = versionService;
    }

    /// <summary>
    /// Current Build Version Number
    /// </summary>
    /// <remarks>
    /// Use this operation to return the current API Build Version number
    /// </remarks>
    /// <response code="200">Returns the current build version number</response>
    /// <response code="500">Internal Server Error</response>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet]
    [Route("api/version")]
    [Produces(typeof(Version))]
    public ActionResult<Version> Get()
    {
        var response = _versionService.GetVersion();
        return StatusCode((int)HttpStatusCode.OK, response);
    }
}
```

**Implementation Notes:**
- The controller uses constructor-based dependency injection to receive an `IVersionService` implementation
- XML documentation comments enable Swagger/OpenAPI documentation generation via Swashbuckle.AspNetCore.SwaggerUI (6.3.0)
- The `ProducesResponseType` attributes provide metadata for API documentation and client code generation
- The `Produces` attribute specifies the return type for strongly-typed API clients
- Explicit `StatusCode` return ensures consistent HTTP status code handling

## Version Service Implementations

The Version API uses a strategy pattern through the `IVersionService` interface, allowing different version retrieval mechanisms to be swapped at runtime via dependency injection. This design supports various deployment scenarios and version management strategies.

### IVersionService Interface

```csharp
namespace BlackSlope.Api.Common.Versioning.Interfaces
{
    public interface IVersionService
    {
        Version GetVersion();
    }
}
```

The interface defines a single method contract that all version service implementations must fulfill. This abstraction enables:
- **Testability:** Mock implementations for unit testing
- **Flexibility:** Easy switching between version sources
- **Extensibility:** Custom version services without modifying existing code

### Assembly-Based Versioning

The `AssemblyVersionService` retrieves version information from the executing assembly's metadata. This approach is ideal for traditional .NET deployment scenarios where version numbers are embedded during compilation.

```csharp
public class AssemblyVersionService : IVersionService
{
    public Version GetVersion()
    {
        var buildVersion = typeof(VersionController).Assembly.GetName().Version.ToString();
        return new Version(buildVersion);
    }
}
```

**How It Works:**
1. Uses reflection to access the `VersionController` type's assembly
2. Retrieves the `AssemblyVersion` attribute value via `GetName().Version`
3. Converts the `System.Version` object to a string
4. Wraps the version string in the custom `Version` response model

**Configuration:**

The assembly version is typically set in the `.csproj` file:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <Version>1.2.3.4</Version>
  <AssemblyVersion>1.2.3.4</AssemblyVersion>
  <FileVersion>1.2.3.4</FileVersion>
</PropertyGroup>
```

**Advantages:**
- No external file dependencies
- Version is embedded in the compiled assembly
- Works in all deployment environments (IIS, Docker, Azure App Service)
- Automatically updated during build process

**Disadvantages:**
- Requires recompilation to change version
- Version number must be managed in project files or build scripts
- All assemblies in a multi-project solution may need version synchronization

**Registration Example:**

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IVersionService, AssemblyVersionService>();
```

### JSON File-Based Versioning

The `JsonVersionService` reads version information from a JSON file on the file system. This approach is suitable for containerized deployments, CI/CD pipelines, or scenarios where version information is injected at deployment time.

```csharp
public class JsonVersionService : IVersionService
{
    private readonly IFileSystem _fileSystem;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public JsonVersionService(IFileSystem fileSystem, IWebHostEnvironment hostingEnvironment)
    {
        _fileSystem = fileSystem;
        _hostingEnvironment = hostingEnvironment;
    }

    public Version GetVersion()
    {
        var filepath = _fileSystem.Path.Combine(
            _hostingEnvironment.ContentRootPath, 
            "..", 
            "Blackslope.Api.Common", 
            "Versioning", 
            "version.json");
        var fileContents = _fileSystem.File.ReadAllText(filepath);
        return JsonSerializer.Deserialize<Version>(fileContents);
    }
}
```

**How It Works:**
1. Constructs the file path relative to the application's content root directory
2. Navigates up one directory level and into the `Blackslope.Api.Common/Versioning` folder
3. Reads the entire `version.json` file contents as a string
4. Deserializes the JSON into a `Version` object using `System.Text.Json` (6.0.10)

**File System Abstraction:**

The service uses `System.IO.Abstractions.IFileSystem` instead of direct `System.IO` calls. This abstraction provides:
- **Testability:** Mock file system operations in unit tests
- **Cross-platform compatibility:** Consistent path handling across Windows and Linux
- **Dependency injection:** File system behavior can be customized or replaced

**Expected JSON Format:**

```json
{
  "version": "2.1.0.5"
}
```

**Advantages:**
- Version can be updated without recompilation
- Supports dynamic version injection in CI/CD pipelines
- Ideal for Docker containers where version files can be mounted or copied during build
- Enables environment-specific versioning (dev, staging, production)

**Disadvantages:**
- Requires file system access at runtime
- File path dependencies can break if directory structure changes
- Potential for file not found or permission errors
- JSON parsing errors if file format is invalid

**Registration Example:**

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<IVersionService, JsonVersionService>();
```

**Docker Integration:**

In a containerized deployment, the version file can be injected during the Docker build:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/YourApi/YourApi.csproj", "src/YourApi/"]
RUN dotnet restore "src/YourApi/YourApi.csproj"
COPY . .

# Inject version file during build
ARG BUILD_VERSION=1.0.0.0
RUN echo "{\"version\":\"${BUILD_VERSION}\"}" > /src/BlackSlope.Api.Common/Versioning/version.json

WORKDIR "/src/src/YourApi"
RUN dotnet build "YourApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "YourApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YourApi.dll"]
```

### Choosing a Version Service

| Scenario | Recommended Service | Rationale |
|----------|---------------------|-----------|
| Traditional IIS deployment | `AssemblyVersionService` | Simple, no external dependencies |
| Docker containers | `JsonVersionService` | Version can be injected at build time |
| Kubernetes deployments | `JsonVersionService` | Version file can be mounted via ConfigMap |
| CI/CD with semantic versioning | `JsonVersionService` | Build pipeline can generate version file |
| Development/testing | `AssemblyVersionService` | Simpler setup, fewer moving parts |
| Multi-environment deployments | `JsonVersionService` | Different versions per environment |

## Response Format

### Version Model

The `Version` class is a simple data transfer object (DTO) that encapsulates the version string:

```csharp
[JsonConverter(typeof(VersionJsonConverter))]
public class Version
{
    public Version(string buildVersion)
    {
        BuildVersion = buildVersion;
    }

    [JsonPropertyName("version")]
    public string BuildVersion { get; }
}
```

**Design Characteristics:**
- **Immutability:** The `BuildVersion` property is read-only (get-only), ensuring the version cannot be modified after construction
- **Constructor injection:** Version string must be provided at instantiation
- **JSON serialization:** Uses `System.Text.Json` attributes for serialization control
- **Custom converter:** The `VersionJsonConverter` attribute indicates custom serialization logic (implementation not shown in provided files)

### JSON Property Mapping

The `[JsonPropertyName("version")]` attribute maps the C# property `BuildVersion` to the JSON property `version`. This provides:
- **API contract stability:** Internal property names can change without breaking the API
- **Naming convention alignment:** JSON uses lowercase naming while C# uses PascalCase
- **Backward compatibility:** API consumers depend on the JSON property name, not the C# implementation

### Serialization Behavior

The `Version` class uses `System.Text.Json` (6.0.10) for serialization, which is the default JSON library in .NET 6.0. Key behaviors include:

**Serialization Example:**
```csharp
var version = new Version("1.2.3.4");
var json = JsonSerializer.Serialize(version);
// Result: {"version":"1.2.3.4"}
```

**Deserialization Example:**
```csharp
var json = "{\"version\":\"1.2.3.4\"}";
var version = JsonSerializer.Deserialize<Version>(json);
// Result: Version object with BuildVersion = "1.2.3.4"
```

### Custom JSON Converter

The `[JsonConverter(typeof(VersionJsonConverter))]` attribute indicates that a custom `JsonConverter<Version>` implementation handles serialization. While the converter implementation is not provided in the source files, typical use cases include:

- **Simplified JSON output:** Serializing directly as a string instead of an object
- **Version format validation:** Ensuring version strings match expected patterns
- **Backward compatibility:** Supporting multiple JSON formats for the same model

**Potential Custom Converter Implementation:**

```csharp
public class VersionJsonConverter : JsonConverter<Version>
{
    public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new Version(reader.GetString());
        }
        
        // Handle object format: {"version":"1.0.0"}
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            if (doc.RootElement.TryGetProperty("version", out JsonElement versionElement))
            {
                return new Version(versionElement.GetString());
            }
        }
        
        throw new JsonException("Invalid version format");
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("version", value.BuildVersion);
        writer.WriteEndObject();
    }
}
```

## Integration and Configuration

### Dependency Injection Setup

To use the Version API in your ASP.NET Core 6.0 application, register the appropriate version service in your dependency injection container:

**Using AssemblyVersionService:**

```csharp
// Program.cs (.NET 6 minimal hosting model)
var builder = WebApplication.CreateBuilder(args);

// Register version service
builder.Services.AddSingleton<IVersionService, AssemblyVersionService>();

// Register controllers
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Using JsonVersionService:**

```csharp
// Program.cs (.NET 6 minimal hosting model)
var builder = WebApplication.CreateBuilder(args);

// Register file system abstraction
builder.Services.AddSingleton<IFileSystem, FileSystem>();

// Register version service
builder.Services.AddSingleton<IVersionService, JsonVersionService>();

// Register controllers
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

**Legacy Startup.cs Pattern:**

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Choose one version service implementation
        services.AddSingleton<IVersionService, AssemblyVersionService>();
        // OR
        // services.AddSingleton<IFileSystem, FileSystem>();
        // services.AddSingleton<IVersionService, JsonVersionService>();
        
        services.AddControllers();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

### Swagger/OpenAPI Integration

The Version API automatically integrates with Swagger UI (Swashbuckle.AspNetCore.SwaggerUI 6.3.0) when configured in your application:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BlackSlope API",
        Version = "v1",
        Description = "API for BlackSlope application"
    });
    
    // Include XML comments for better documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API v1");
    });
}

app.MapControllers();
app.Run();
```

**Enable XML Documentation:**

Add to your `.csproj` file:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### Health Check Integration

While the Version API is separate from health checks, it's common to use both for monitoring. The version endpoint can be called alongside health check endpoints:

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "sql-server",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddDbContextCheck<YourDbContext>(
        name: "ef-core-context",
        tags: new[] { "db", "ef-core" });

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapControllers(); // Includes /api/version
```

**Monitoring Script Example:**

```bash
#!/bin/bash
# Check both health and version endpoints

HEALTH_URL="https://api.example.com/health"
VERSION_URL="https://api.example.com/api/version"

# Check health
HEALTH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)
echo "Health Status: $HEALTH_STATUS"

# Get version
VERSION=$(curl -s $VERSION_URL | jq -r '.version')
echo "Current Version: $VERSION"

if [ "$HEALTH_STATUS" == "200" ]; then
    echo "Application is healthy and running version $VERSION"
else
    echo "Application health check failed!"
    exit 1
fi
```

## Error Handling and Edge Cases

### Common Error Scenarios

#### 1. JsonVersionService File Not Found

**Scenario:** The `version.json` file doesn't exist at the expected path.

**Exception:**
```
System.IO.FileNotFoundException: Could not find file 'C:\path\to\Blackslope.Api.Common\Versioning\version.json'
```

**Solution:**
- Ensure the `version.json` file exists in the correct location
- Verify the relative path calculation in `JsonVersionService`
- Check file permissions in production environments
- Consider implementing a fallback mechanism:

```csharp
public class JsonVersionService : IVersionService
{
    private readonly IFileSystem _fileSystem;
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly ILogger<JsonVersionService> _logger;

    public JsonVersionService(
        IFileSystem fileSystem, 
        IWebHostEnvironment hostingEnvironment,
        ILogger<JsonVersionService> logger)
    {
        _fileSystem = fileSystem;
        _hostingEnvironment = hostingEnvironment;
        _logger = logger;
    }

    public Version GetVersion()
    {
        try
        {
            var filepath = _fileSystem.Path.Combine(
                _hostingEnvironment.ContentRootPath, 
                "..", 
                "Blackslope.Api.Common", 
                "Versioning", 
                "version.json");
                
            if (!_fileSystem.File.Exists(filepath))
            {
                _logger.LogWarning("Version file not found at {FilePath}, using default version", filepath);
                return new Version("0.0.0.0");
            }
            
            var fileContents = _fileSystem.File.ReadAllText(filepath);
            return JsonSerializer.Deserialize<Version>(fileContents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading version file");
            return new Version("0.0.0.0");
        }
    }
}
```

#### 2. Invalid JSON Format

**Scenario:** The `version.json` file contains malformed JSON.

**Exception:**
```
System.Text.Json.JsonException: The JSON value could not be converted to BlackSlope.Api.Common.Versioning.Version
```

**Solution:**
- Validate JSON format before deployment
- Implement JSON schema validation in CI/CD pipeline
- Add error handling with logging:

```csharp
public Version GetVersion()
{
    try
    {
        var filepath = _fileSystem.Path.Combine(/* ... */);
        var fileContents = _fileSystem.File.ReadAllText(filepath);
        
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        
        return JsonSerializer.Deserialize<Version>(fileContents, options);
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Invalid JSON format in version file");
        throw new InvalidOperationException("Version file contains invalid JSON", ex);
    }
}
```

#### 3. Assembly Version Not Set

**Scenario:** The assembly version is not explicitly set in the project file.

**Result:** Returns default version `1.0.0.0`

**Solution:**
- Always set explicit version numbers in `.csproj`:

```xml
<PropertyGroup>
  <Version>1.2.3.4</Version>
  <AssemblyVersion>1.2.3.4</AssemblyVersion>
  <FileVersion>1.2.3.4</FileVersion>
</PropertyGroup>
```

- Or use MSBuild properties for dynamic versioning:

```xml
<PropertyGroup>
  <Version>$(GitVersion_SemVer)</Version>
  <AssemblyVersion>$(GitVersion_AssemblySemVer)</AssemblyVersion>
</PropertyGroup>
```

#### 4. Path Traversal Issues in Docker

**Scenario:** The relative path `..` doesn't work as expected in containerized environments.

**Solution:**
- Use absolute paths or environment variables:

```csharp
public Version GetVersion()
{
    var versionFilePath = Environment.GetEnvironmentVariable("VERSION_FILE_PATH") 
        ?? _fileSystem.Path.Combine(_hostingEnvironment.ContentRootPath, "version.json");
        
    var fileContents = _fileSystem.File.ReadAllText(versionFilePath);
    return JsonSerializer.Deserialize<Version>(fileContents);
}
```

- Configure in Docker:

```dockerfile
ENV VERSION_FILE_PATH=/app/version.json
COPY version.json /app/version.json
```

### Testing Considerations

#### Unit Testing with Mocks

**Testing AssemblyVersionService:**

```csharp
[Fact]
public void GetVersion_ReturnsAssemblyVersion()
{
    // Arrange
    var service = new AssemblyVersionService();
    
    // Act
    var result = service.GetVersion();
    
    // Assert
    Assert.NotNull(result);
    Assert.NotNull(result.BuildVersion);
    Assert.Matches(@"\d+\.\d+\.\d+\.\d+", result.BuildVersion);
}
```

**Testing JsonVersionService:**

```csharp
[Fact]
public void GetVersion_ReadsFromJsonFile_ReturnsVersion()
{
    // Arrange
    var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { @"C:\app\Blackslope.Api.Common\Versioning\version.json", 
          new MockFileData("{\"version\":\"2.1.0.5\"}") }
    });
    
    var mockEnvironment = new Mock<IWebHostEnvironment>();
    mockEnvironment.Setup(e => e.ContentRootPath).Returns(@"C:\app\YourApi");
    
    var service = new JsonVersionService(mockFileSystem, mockEnvironment.Object);
    
    // Act
    var result = service.GetVersion();
    
    // Assert
    Assert.Equal("2.1.0.5", result.BuildVersion);
}
```

**Testing VersionController:**

```csharp
[Fact]
public void Get_ReturnsOkResultWithVersion()
{
    // Arrange
    var mockVersionService = new Mock<IVersionService>();
    mockVersionService.Setup(s => s.GetVersion())
        .Returns(new Version("1.2.3.4"));
    
    var controller = new VersionController(mockVersionService.Object);
    
    // Act
    var result = controller.Get();
    
    // Assert
    var okResult = Assert.IsType<ObjectResult>(result.Result);
    Assert.Equal(200, okResult.StatusCode);
    
    var version = Assert.IsType<Version>(okResult.Value);
    Assert.Equal("1.2.3.4", version.BuildVersion);
}
```

## Best Practices and Recommendations

### 1. Version Number Format

Use semantic versioning (SemVer) format: `MAJOR.MINOR.PATCH.BUILD`

```
1.2.3.4
│ │ │ └─ Build number (auto-incremented by CI/CD)
│ │ └─── Patch version (bug fixes)
│ └───── Minor version (new features, backward compatible)
└─────── Major version (breaking changes)
```

### 2. CI/CD Integration

**Azure DevOps Pipeline Example:**

```yaml
trigger:
  branches:
    include:
    - main
    - develop

variables:
  buildVersion: '1.2.$(Rev:r)'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '/p:Version=$(buildVersion)'
    
- script: |
    echo '{"version":"$(buildVersion)"}' > $(Build.SourcesDirectory)/src/BlackSlope.Api.Common/Versioning/version.json
  displayName: 'Generate version.json'
  
- task: Docker@2
  displayName: 'Build Docker Image'
  inputs:
    command: 'build'
    arguments: '--build-arg BUILD_VERSION=$(buildVersion)'
```

### 3. Caching Considerations

For high-traffic APIs, consider caching the version response:

```csharp
public class CachedVersionService : IVersionService
{
    private readonly IVersionService _innerService;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "ApplicationVersion";
    
    public CachedVersionService(IVersionService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }
    
    public Version GetVersion()
    {
        return _cache.GetOrCreate(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return _innerService.GetVersion();
        });
    }
}
```

### 4. Monitoring and Alerting

Integrate version checking into monitoring systems:

```csharp
// Custom health check that includes version
public class VersionHealthCheck : IHealthCheck
{
    private readonly IVersionService _versionService;
    
    public VersionHealthCheck(IVersionService versionService)
    {
        _versionService = versionService;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var version = _versionService.GetVersion();
            var data = new Dictionary<string, object>
            {
                { "version", version.BuildVersion }
            };
            
            return Task.FromResult(
                HealthCheckResult.Healthy("Version service is operational", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Version service failed", ex));
        }
    }
}
```

### 5. Security Considerations

The version endpoint is typically public, but consider:

- **Information disclosure:** Version numbers can reveal deployment timing and potentially security vulnerabilities
- **Rate limiting:** Implement rate limiting to prevent abuse
- **Authentication (optional):** For sensitive environments, require authentication:

```csharp
[Authorize]
[HttpGet]
[Route("api/version")]
public ActionResult<Version> Get()
{
    var response = _versionService.GetVersion();
    return StatusCode((int)HttpStatusCode.OK, response);
}
```

### 6. Documentation Standards

Always maintain up-to-date XML documentation comments for Swagger generation:

```csharp
/// <summary>
/// Retrieves the current application version
/// </summary>
/// <remarks>
/// This endpoint returns the build version number of the deployed application.
/// The version format follows semantic versioning: MAJOR.MINOR.PATCH.BUILD
/// 
/// Sample request:
/// 
///     GET /api/version
///     
/// Sample response:
/// 
///     {
///       "version": "1.2.3.4"
///     }
/// </remarks>
/// <response code="200">Successfully retrieved version information</response>
/// <response code="500">Internal server error occurred while retrieving version</response>
/// <returns>Version object containing the build version string</returns>
[ProducesResponseType(typeof(Version), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpGet]
[Route("api/version")]
public ActionResult<Version> Get()
{
    var response = _versionService.GetVersion();
    return StatusCode((int)HttpStatusCode.OK, response);
}
```

## Troubleshooting Guide

| Issue | Symptoms | Resolution |
|-------|----------|------------|
| Version returns `0.0.0.0` | Default version displayed | Check assembly version in `.csproj` or verify `version.json` exists |
| 500 Internal Server Error | API returns error status | Check application logs for exceptions; verify file permissions and paths |
| Swagger shows no version endpoint | Endpoint missing from Swagger UI | Ensure controllers are registered and XML documentation is enabled |
| Version doesn't update after deployment | Old version still displayed | Clear application cache; restart application; verify new files deployed |
| Docker container shows wrong version | Version mismatch in container | Rebuild Docker image; verify version file copied during build |
| Path not found in Linux container | File system error | Use forward slashes in paths; verify case sensitivity |

## Related Technologies

This API leverages the following components from the technology stack:

- **ASP.NET Core 6.0:** Web framework providing MVC and routing capabilities
- **System.Text.Json 6.0.10:** High-performance JSON serialization
- **Swashbuckle.AspNetCore.SwaggerUI 6.3.0:** API documentation generation
- **Microsoft.Extensions.DependencyInjection 6.0.0:** Dependency injection container
- **System.IO.Abstractions:** File system abstraction for testability

For more information on the overall versioning strategy and related features, see the [Versioning Features](/features/versioning.md) documentation.