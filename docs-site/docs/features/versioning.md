# API Versioning

## Overview

The BlackSlope API implements a comprehensive versioning system that enables clients to retrieve the current build version of the API. This system supports multiple versioning strategies and provides a standardized endpoint for version information retrieval. The versioning infrastructure is built on a service-oriented architecture with dependency injection, allowing for flexible implementation swapping based on deployment requirements.

## Versioning Strategy

The API versioning implementation supports three distinct strategies:

### URL Path Versioning

The version endpoint follows a URL path-based approach:

```
GET /api/version
```

This endpoint is version-agnostic and returns the current build version regardless of the API version being used. The endpoint is defined in the `VersionController` and is accessible without version prefixes, making it a stable reference point for clients to query the current API build.

**Key Characteristics:**
- **Stability**: The `/api/version` endpoint remains constant across all API versions
- **Accessibility**: No authentication required (unless globally enforced)
- **Simplicity**: Single endpoint for all version information needs

### Assembly-Based Versioning

The `AssemblyVersionService` extracts version information directly from the compiled assembly metadata:

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

**Advantages:**
- Automatically synchronized with build process
- No external file dependencies
- Version is embedded in the compiled binary
- Follows standard .NET versioning conventions (Major.Minor.Build.Revision)

**Use Cases:**
- Production deployments where version is set during CI/CD pipeline
- Environments where file system access is restricted
- Scenarios requiring guaranteed version-binary coupling

### JSON File-Based Versioning

The `JsonVersionService` reads version information from an external JSON configuration file:

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

**Expected JSON Format:**

```json
{
  "version": "1.2.3.4"
}
```

**Advantages:**
- Version can be updated without recompilation
- Supports dynamic versioning in containerized environments
- Enables version overrides for testing scenarios
- Decouples version from build artifacts

**Use Cases:**
- Development and testing environments
- Docker containers where version is injected at runtime
- Scenarios requiring version hotfixes without redeployment

**Important Considerations:**
- The file path is relative to the content root and navigates up one directory level
- Uses `IFileSystem` abstraction for testability and flexibility
- Missing or malformed JSON files will cause runtime exceptions
- Ensure the `version.json` file is included in deployment packages

## Version Service

The versioning system is built around a service-oriented architecture with clear separation of concerns.

### IVersionService Interface

The `IVersionService` interface defines the contract for all version service implementations:

```csharp
namespace BlackSlope.Api.Common.Versioning.Interfaces
{
    public interface IVersionService
    {
        Version GetVersion();
    }
}
```

**Design Principles:**
- **Single Responsibility**: The interface has one method with one purpose
- **Dependency Inversion**: Controllers depend on abstractions, not concrete implementations
- **Open/Closed**: New versioning strategies can be added without modifying existing code

### Service Registration

Register the desired version service implementation in your `Startup.cs` or `Program.cs`:

```csharp
// For assembly-based versioning
services.AddScoped<IVersionService, AssemblyVersionService>();

// OR for JSON file-based versioning
services.AddScoped<IVersionService, JsonVersionService>();
```

**Configuration Recommendations:**

| Environment | Recommended Strategy | Rationale |
|-------------|---------------------|-----------|
| Development | `JsonVersionService` | Allows quick version changes for testing |
| Staging | `AssemblyVersionService` | Matches production behavior |
| Production | `AssemblyVersionService` | Ensures version accuracy and reliability |
| Docker/Kubernetes | `JsonVersionService` | Enables version injection via ConfigMaps/Secrets |

### Assembly Version Implementation

The `AssemblyVersionService` leverages .NET reflection to extract version metadata:

```csharp
public class AssemblyVersionService : IVersionService
{
    public Version GetVersion()
    {
        // Uses the VersionController's assembly as the reference point
        var buildVersion = typeof(VersionController).Assembly.GetName().Version.ToString();
        return new Version(buildVersion);
    }
}
```

**Technical Details:**
- **Assembly Reference**: Uses `typeof(VersionController).Assembly` to ensure the correct assembly is queried
- **Version Format**: Returns a string in the format `Major.Minor.Build.Revision` (e.g., "1.0.0.0")
- **Performance**: Reflection-based but cached by the runtime; negligible performance impact
- **Thread Safety**: Fully thread-safe as it only reads immutable assembly metadata

**Setting Assembly Version:**

Configure the version in your `.csproj` file:

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <Version>1.2.3</Version>
  <AssemblyVersion>1.2.3.0</AssemblyVersion>
  <FileVersion>1.2.3.0</FileVersion>
</PropertyGroup>
```

Or use CI/CD pipeline variables:

```xml
<PropertyGroup>
  <Version>$(BUILD_VERSION)</Version>
  <AssemblyVersion>$(BUILD_VERSION).0</AssemblyVersion>
</PropertyGroup>
```

### JSON Version Implementation

The `JsonVersionService` provides file-based version management with dependency injection for testability:

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

**Dependencies:**
- **IFileSystem**: Abstraction from `System.IO.Abstractions` package for file system operations
- **IWebHostEnvironment**: ASP.NET Core service providing hosting environment information

**File Path Resolution:**
1. Starts from `ContentRootPath` (typically the application's root directory)
2. Navigates up one directory level (`..`)
3. Enters the `Blackslope.Api.Common` directory
4. Accesses the `Versioning` subdirectory
5. Reads `version.json`

**Error Scenarios:**

| Scenario | Exception Type | Mitigation |
|----------|---------------|------------|
| File not found | `FileNotFoundException` | Ensure file is included in build output |
| Invalid JSON | `JsonException` | Validate JSON format during deployment |
| Permission denied | `UnauthorizedAccessException` | Verify application has read permissions |
| Null version property | `NullReferenceException` | Add validation in service or use nullable types |

**Testing Considerations:**

The use of `IFileSystem` enables comprehensive unit testing:

```csharp
[Fact]
public void GetVersion_ReturnsCorrectVersion()
{
    // Arrange
    var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { @"c:\app\Blackslope.Api.Common\Versioning\version.json", 
          new MockFileData("{\"version\":\"2.1.0\"}") }
    });
    var mockEnvironment = Mock.Of<IWebHostEnvironment>(
        e => e.ContentRootPath == @"c:\app\api");
    var service = new JsonVersionService(mockFileSystem, mockEnvironment);

    // Act
    var result = service.GetVersion();

    // Assert
    Assert.Equal("2.1.0", result.BuildVersion);
}
```

## Version Controller

The `VersionController` exposes the version information through a RESTful endpoint.

### Controller Implementation

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

**Key Features:**
- **Dependency Injection**: Receives `IVersionService` through constructor injection
- **XML Documentation**: Includes comprehensive XML comments for Swagger/OpenAPI generation
- **Response Type Attributes**: Explicitly declares possible HTTP status codes
- **Type Safety**: Returns strongly-typed `ActionResult<Version>`
- **Base Controller**: Inherits from `BaseController` for shared functionality

### Endpoint for Version Retrieval

**Endpoint Details:**

| Property | Value |
|----------|-------|
| HTTP Method | GET |
| Route | `/api/version` |
| Authentication | Not specified (inherits from base controller or global policy) |
| Content Type | `application/json` |
| Success Status | 200 OK |
| Error Status | 500 Internal Server Error |

**Request Example:**

```http
GET /api/version HTTP/1.1
Host: api.blackslope.com
Accept: application/json
```

**Success Response Example:**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "version": "1.2.3.4"
}
```

**Error Response Example:**

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "error": "Unable to retrieve version information"
}
```

### Version Response Format

The `Version` class defines the response structure with JSON serialization attributes:

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
- **Immutability**: Read-only property with constructor initialization
- **Custom Converter**: Uses `VersionJsonConverter` for specialized serialization logic
- **Property Naming**: JSON property name differs from C# property name for API consistency
- **Type Safety**: Strongly-typed string property prevents type confusion

**JSON Serialization:**

The `[JsonPropertyName("version")]` attribute ensures the JSON output uses lowercase "version" regardless of the C# property name:

```json
{
  "version": "1.2.3.4"
}
```

Without this attribute, the default serialization would produce:

```json
{
  "buildVersion": "1.2.3.4"
}
```

### JSON Serialization

The custom `VersionJsonConverter` (referenced but not provided in source files) likely handles specialized serialization scenarios such as:

- **Null Handling**: Graceful handling of null version values
- **Format Validation**: Ensuring version strings follow expected patterns
- **Backward Compatibility**: Supporting legacy version formats
- **Deserialization**: Converting JSON back to `Version` objects

**Expected Converter Implementation Pattern:**

```csharp
public class VersionJsonConverter : JsonConverter<Version>
{
    public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var versionString = reader.GetString();
        return new Version(versionString);
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("version", value.BuildVersion);
        writer.WriteEndObject();
    }
}
```

**Integration with System.Text.Json:**

The application uses `System.Text.Json` (version 6.0.10) as specified in the tech stack, ensuring:
- High-performance serialization
- Native .NET 6 integration
- Reduced memory allocations
- Source generator support (if configured)

## Best Practices

### When to Increment Versions

Follow semantic versioning principles adapted for API development:

**Version Format: `MAJOR.MINOR.PATCH.BUILD`**

| Component | Increment When | Example Scenarios |
|-----------|----------------|-------------------|
| **MAJOR** | Breaking changes to API contracts | - Removing endpoints<br>- Changing response structures<br>- Modifying authentication schemes<br>- Renaming properties |
| **MINOR** | Backward-compatible new features | - Adding new endpoints<br>- Adding optional parameters<br>- Adding new response properties<br>- New functionality |
| **PATCH** | Backward-compatible bug fixes | - Fixing incorrect behavior<br>- Performance improvements<br>- Security patches<br>- Documentation updates |
| **BUILD** | Every build/deployment | - Automated CI/CD builds<br>- Nightly builds<br>- Development iterations |

**Version Increment Examples:**

```
1.0.0.0 → 2.0.0.0  // Breaking change: Removed deprecated endpoints
1.0.0.0 → 1.1.0.0  // New feature: Added user preferences endpoint
1.0.0.0 → 1.0.1.0  // Bug fix: Corrected date formatting issue
1.0.0.0 → 1.0.0.1  // Build: Automated deployment
```

**CI/CD Integration:**

Configure your build pipeline to automatically increment versions:

```yaml
# Azure DevOps example
variables:
  major: 1
  minor: 2
  patch: $[counter(format('{0}.{1}', variables['major'], variables['minor']), 0)]
  build: $(Build.BuildId)
  version: $(major).$(minor).$(patch).$(build)

steps:
- task: DotNetCoreCLI@2
  inputs:
    command: 'build'
    arguments: '/p:Version=$(version)'
```

### Backward Compatibility

Maintaining backward compatibility is critical for API stability and client trust.

**Compatibility Guidelines:**

1. **Additive Changes Only (for MINOR versions)**
   - Add new endpoints without removing old ones
   - Add optional parameters with default values
   - Add new properties to responses (clients should ignore unknown properties)

2. **Deprecation Before Removal**
   - Mark endpoints as deprecated in documentation
   - Add deprecation warnings in response headers
   - Provide migration guides
   - Maintain deprecated endpoints for at least one MAJOR version

3. **Response Structure Stability**
   ```csharp
   // ✅ GOOD: Adding optional property
   public class UserResponse
   {
       public int Id { get; set; }
       public string Name { get; set; }
       public string Email { get; set; }  // New in v1.1
   }

   // ❌ BAD: Changing property type
   public class UserResponse
   {
       public string Id { get; set; }  // Was int, now string - BREAKING!
       public string Name { get; set; }
   }
   ```

4. **Request Validation**
   - Accept additional properties in requests (ignore unknown fields)
   - Make new parameters optional
   - Provide sensible defaults

**Testing Backward Compatibility:**

```csharp
[Fact]
public void VersionEndpoint_MaintainsResponseStructure()
{
    // Arrange
    var controller = new VersionController(new AssemblyVersionService());
    
    // Act
    var result = controller.Get();
    var okResult = Assert.IsType<ObjectResult>(result.Result);
    var version = Assert.IsType<Version>(okResult.Value);
    
    // Assert - Ensure response structure hasn't changed
    Assert.NotNull(version.BuildVersion);
    Assert.IsType<string>(version.BuildVersion);
}
```

### Deprecation Strategy

Implement a structured approach to deprecating API features:

**Phase 1: Announcement (MINOR version)**
- Document the deprecation in release notes
- Add deprecation notices to API documentation
- Update Swagger/OpenAPI specifications with deprecation flags
- Notify clients through communication channels

```csharp
/// <summary>
/// Legacy version endpoint (DEPRECATED)
/// </summary>
/// <remarks>
/// This endpoint is deprecated and will be removed in v3.0.0.
/// Please use /api/v2/version instead.
/// </remarks>
[Obsolete("Use /api/v2/version instead. This endpoint will be removed in v3.0.0")]
[HttpGet]
[Route("api/v1/version")]
public ActionResult<Version> GetLegacy()
{
    Response.Headers.Add("X-API-Deprecated", "true");
    Response.Headers.Add("X-API-Deprecation-Info", "Use /api/v2/version");
    return _versionService.GetVersion();
}
```

**Phase 2: Warning Period (1-2 MINOR versions)**
- Add response headers indicating deprecation
- Log usage of deprecated endpoints for monitoring
- Provide migration documentation and code examples
- Offer support for clients during transition

```csharp
public class DeprecationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeprecationMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            _logger.LogWarning(
                "Deprecated endpoint accessed: {Path} by {Client}", 
                context.Request.Path, 
                context.Connection.RemoteIpAddress);
            
            context.Response.Headers.Add("X-API-Deprecated", "true");
            context.Response.Headers.Add("Sunset", "Sat, 31 Dec 2024 23:59:59 GMT");
        }
        
        await _next(context);
    }
}
```

**Phase 3: Removal (MAJOR version)**
- Remove deprecated endpoints in next MAJOR version
- Return 410 Gone for removed endpoints (optional transition period)
- Update all documentation
- Communicate removal in release notes

**Deprecation Timeline Example:**

| Version | Action | Timeline |
|---------|--------|----------|
| v1.5.0 | Announce deprecation of `/api/v1/version` | Month 0 |
| v1.6.0 | Add deprecation warnings | Month 1 |
| v1.7.0 | Continue warnings, provide migration support | Month 2 |
| v1.8.0 | Final warning before removal | Month 3 |
| v2.0.0 | Remove deprecated endpoint | Month 4 |

**Client Communication:**

```json
// Deprecation notice in API response
{
  "version": "1.5.0",
  "deprecation": {
    "deprecated": true,
    "sunset": "2024-12-31T23:59:59Z",
    "replacement": "/api/v2/version",
    "documentation": "https://docs.blackslope.com/migration/v2"
  }
}
```

## Related Documentation

For additional information on API versioning and related topics, refer to:

- [Version API Reference](/api_reference/version_api.md) - Detailed API endpoint specifications and examples
- [Controllers](/features/controllers.md) - Overview of controller architecture and base controller functionality
- [Versioning Strategy](/deployment/versioning_strategy.md) - Deployment-specific versioning considerations and CI/CD integration

## Troubleshooting

### Common Issues

**Issue: Version endpoint returns 500 Internal Server Error**

*Cause*: `JsonVersionService` cannot find or read `version.json` file

*Solution*:
```csharp
// Add error handling to JsonVersionService
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
            throw new FileNotFoundException($"Version file not found at {filepath}");
        }
        
        var fileContents = _fileSystem.File.ReadAllText(filepath);
        return JsonSerializer.Deserialize<Version>(fileContents);
    }
    catch (Exception ex)
    {
        // Log error and return fallback version
        _logger.LogError(ex, "Failed to read version from JSON file");
        return new Version("0.0.0.0");
    }
}
```

**Issue: Assembly version shows as 1.0.0.0 in all builds**

*Cause*: Assembly version not configured in project file or CI/CD pipeline

*Solution*: Update `.csproj` file:
```xml
<PropertyGroup>
  <Version>$(VersionPrefix)</Version>
  <AssemblyVersion>$(VersionPrefix).0</AssemblyVersion>
  <FileVersion>$(VersionPrefix).$(BuildNumber)</FileVersion>
</PropertyGroup>
```

**Issue: Version endpoint not appearing in Swagger UI**

*Cause*: Controller not discovered or Swagger configuration excludes it

*Solution*: Verify controller registration and Swagger configuration:
```csharp
services.AddControllers()
    .AddApplicationPart(typeof(VersionController).Assembly);

services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BlackSlope API", Version = "v1" });
    // Ensure XML comments are included
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
```