# Versioning Strategy

## Overview

The BlackSlope.NET application implements a comprehensive versioning strategy that encompasses assembly versioning, API endpoint versioning, and deployment artifact versioning. This strategy ensures clear communication of changes to API consumers, maintains backward compatibility where possible, and provides a structured approach to managing breaking changes across the application lifecycle.

The versioning implementation leverages both .NET assembly versioning capabilities and a custom JSON-based versioning system, with API endpoints explicitly versioned in their route definitions to support multiple API versions simultaneously.

---

## Semantic Versioning

### Version Format

BlackSlope.NET follows **Semantic Versioning 2.0.0** (SemVer) principles with a four-part version number format:

```
MAJOR.MINOR.PATCH.BUILD
```

- **MAJOR**: Incremented for incompatible API changes that break backward compatibility
- **MINOR**: Incremented for new functionality added in a backward-compatible manner
- **PATCH**: Incremented for backward-compatible bug fixes
- **BUILD**: Automatically incremented build number (typically managed by CI/CD pipeline)

### Current Version Implementation

The application maintains its version in a centralized JSON file:

```json
{
  "version": "1.0.0.0"
}
```
*Source: `src/BlackSlope.Api.Common/Versioning/version.json`*

This version file serves as the single source of truth for the application version and is consumed by the versioning service at runtime.

### Breaking Changes Policy

**Major Version Increments** are required when:

- Removing or renaming API endpoints
- Changing request/response payload structures in non-backward-compatible ways
- Modifying authentication or authorization requirements
- Changing HTTP status codes for existing operations
- Altering the behavior of existing operations in ways that could break client expectations

**Example Breaking Changes:**
```csharp
// Breaking: Changing response type
// v1: Returns List<MovieViewModel>
// v2: Returns PagedResult<MovieViewModel>

// Breaking: Removing endpoint
[HttpGet]
[Route("api/v1/movies/{id}")] // Removed in v2

// Breaking: Changing required fields
public class CreateMovieViewModel 
{
    public string Title { get; set; }
    public string Director { get; set; } // New required field in v2
}
```

**Minor Version Increments** are appropriate for:

- Adding new optional fields to request/response models
- Adding new API endpoints
- Adding new optional query parameters
- Enhancing existing functionality without changing contracts

**Patch Version Increments** are used for:

- Bug fixes that don't alter API contracts
- Performance improvements
- Internal refactoring
- Security patches that don't change API behavior

### API Deprecation Strategy

BlackSlope.NET implements a structured deprecation process:

1. **Announcement Phase** (Minimum 6 months before removal)
   - Document deprecated endpoints in API documentation
   - Add `[Obsolete]` attributes to deprecated controllers/actions
   - Include deprecation warnings in Swagger/OpenAPI documentation
   - Communicate deprecation timeline to API consumers

2. **Warning Phase** (3 months before removal)
   - Add custom response headers to deprecated endpoints:
     ```
     X-API-Deprecated: true
     X-API-Deprecation-Date: 2024-12-31
     X-API-Sunset-Date: 2025-03-31
     ```
   - Log usage of deprecated endpoints for monitoring

3. **Removal Phase**
   - Remove deprecated endpoints in next major version
   - Maintain previous major version for minimum 12 months post-release

**Implementation Example:**

```csharp
/// <summary>
/// [DEPRECATED] Use api/v2/movies instead
/// This endpoint will be removed in v3.0.0 (Sunset: 2025-03-31)
/// </summary>
[Obsolete("Use api/v2/movies instead. This endpoint will be removed in v3.0.0")]
[HttpGet]
[Route("api/v1/movies/legacy")]
public async Task<ActionResult<List<MovieViewModel>>> GetLegacy()
{
    Response.Headers.Add("X-API-Deprecated", "true");
    Response.Headers.Add("X-API-Sunset-Date", "2025-03-31");
    
    // Implementation
}
```

---

## Version Implementation

### Assembly Versioning

The application uses .NET assembly versioning to track the version of compiled assemblies. This is configured in the project file and can be accessed at runtime.

**Version Service Implementation:**

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
*Source: `src/BlackSlope.Api.Common/Versioning/Services/AssemblyVersionService.cs`*

This service retrieves the assembly version from the compiled assembly metadata, providing a reliable way to determine the running application version.

### JSON File Versioning

The centralized `version.json` file provides a human-readable and easily updatable version source:

**Version Model:**

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
*Source: `src/BlackSlope.Api.Common/Versioning/Version.cs`*

**Key Features:**
- Custom JSON converter for flexible serialization
- Immutable version object (read-only property)
- Single constructor ensures version is always set
- JSON property naming follows lowercase convention for API consistency

**Usage in CI/CD:**

```bash
# Update version in CI/CD pipeline
$version = "2.1.0.${env:BUILD_NUMBER}"
$json = @{ version = $version } | ConvertTo-Json
$json | Set-Content -Path "src/BlackSlope.Api.Common/Versioning/version.json"
```

### API Endpoint Versioning

BlackSlope.NET implements **URL-based versioning** for API endpoints, embedding the version directly in the route path. This approach provides clear, explicit versioning that is easily discoverable and cacheable.

**Current Implementation:**

```csharp
[HttpGet]
[Route("api/v1/movies")]
public async Task<ActionResult<List<MovieViewModel>>> Get()
{
    var movies = await _movieService.GetAllMoviesAsync();
    var response = _mapper.Map<List<MovieViewModel>>(movies);
    return HandleSuccessResponse(response);
}

[HttpGet]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Get(int id)
{
    var movie = await _movieService.GetMovieAsync(id);
    var response = _mapper.Map<MovieViewModel>(movie);
    return HandleSuccessResponse(response);
}

[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    // Implementation
}

[HttpPut]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
{
    // Implementation
}

[HttpDelete]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<DeletedMovieResponse>> Delete(int id)
{
    // Implementation
}
```
*Source: `src/BlackSlope.Api/Operations/Movies/MoviesController.cs`*

**Versioning Pattern:**
- All routes follow the pattern: `api/v{major}/resource`
- Only major version is included in URL (e.g., `v1`, `v2`)
- Minor and patch versions maintain backward compatibility within the same major version
- Each major version can have its own controller implementation

**Supporting Multiple Versions:**

```csharp
// v1 Controller
[ApiController]
[Route("api/v1/[controller]")]
public class MoviesV1Controller : BaseController
{
    // v1 implementation
}

// v2 Controller with breaking changes
[ApiController]
[Route("api/v2/[controller]")]
public class MoviesV2Controller : BaseController
{
    // v2 implementation with new features/breaking changes
}
```

**Advantages of URL-Based Versioning:**
- **Explicit and Discoverable**: Version is immediately visible in the URL
- **Cacheable**: Different versions can be cached independently
- **Simple Routing**: No custom header parsing or content negotiation required
- **Browser-Friendly**: Easy to test in browsers and tools like Postman
- **Documentation-Friendly**: Swagger UI naturally separates versions

**Alternative Versioning Approaches** (not currently implemented but available):

1. **Header-Based Versioning:**
   ```csharp
   [ApiVersion("1.0")]
   [ApiVersion("2.0")]
   [Route("api/movies")]
   public class MoviesController : BaseController
   {
       [HttpGet]
       [MapToApiVersion("1.0")]
       public async Task<ActionResult> GetV1() { }
       
       [HttpGet]
       [MapToApiVersion("2.0")]
       public async Task<ActionResult> GetV2() { }
   }
   ```

2. **Query String Versioning:**
   ```
   GET /api/movies?api-version=1.0
   ```

3. **Media Type Versioning:**
   ```
   Accept: application/vnd.blackslope.v1+json
   ```

---

## Deployment Versioning

### Image Tagging Strategy

For Docker containerization, BlackSlope.NET follows a comprehensive tagging strategy to support various deployment scenarios:

**Tag Formats:**

1. **Semantic Version Tag**: `blackslope.api:1.0.0.123`
   - Full four-part version number
   - Immutable reference to specific build
   - Used for production deployments

2. **Major.Minor Tag**: `blackslope.api:1.0`
   - Automatically updated to latest patch version
   - Useful for environments that want latest stable minor version

3. **Major Tag**: `blackslope.api:1`
   - Automatically updated to latest minor/patch version
   - Used for development environments

4. **Latest Tag**: `blackslope.api:latest`
   - Points to most recent build
   - Used for development/testing only, never production

5. **Branch Tags**: `blackslope.api:develop-123`, `blackslope.api:feature-auth-456`
   - Build-specific tags for non-main branches
   - Used for feature testing and CI/CD validation

**Docker Build Example:**

```dockerfile
# Multi-stage build from src/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["BlackSlope.Api/BlackSlope.Api.csproj", "BlackSlope.Api/"]
RUN dotnet restore "BlackSlope.Api/BlackSlope.Api.csproj"
COPY . .
WORKDIR "/src/BlackSlope.Api"
RUN dotnet build "BlackSlope.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlackSlope.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

**CI/CD Tagging Script:**

```bash
#!/bin/bash
VERSION=$(cat src/BlackSlope.Api.Common/Versioning/version.json | jq -r '.version')
MAJOR=$(echo $VERSION | cut -d. -f1)
MINOR=$(echo $VERSION | cut -d. -f1-2)

# Build image
docker build -t blackslope.api:${VERSION} -f src/Dockerfile ./src

# Apply multiple tags
docker tag blackslope.api:${VERSION} blackslope.api:${MINOR}
docker tag blackslope.api:${VERSION} blackslope.api:${MAJOR}
docker tag blackslope.api:${VERSION} blackslope.api:latest

# Push to registry
docker push blackslope.api:${VERSION}
docker push blackslope.api:${MINOR}
docker push blackslope.api:${MAJOR}
docker push blackslope.api:latest
```

### Release Management

**Release Process:**

1. **Version Bump**
   - Update `version.json` with new version number
   - Update assembly version in project files if needed
   - Commit version change to version control

2. **Build & Test**
   - Run full test suite: `dotnet test ./src/`
   - Execute integration tests (when SpecFlow supports .NET 6)
   - Perform security scanning and code analysis

3. **Create Release Branch**
   ```bash
   git checkout -b release/v1.0.0
   git push origin release/v1.0.0
   ```

4. **Build Release Artifacts**
   - Build Docker image with version tags
   - Generate release notes from commit history
   - Create deployment manifests

5. **Deploy to Staging**
   - Deploy to staging environment
   - Run smoke tests and integration tests
   - Perform manual QA validation

6. **Production Deployment**
   - Create Git tag: `git tag -a v1.0.0 -m "Release v1.0.0"`
   - Deploy to production using blue-green or canary strategy
   - Monitor health checks and application metrics

7. **Post-Deployment**
   - Verify health check endpoints
   - Monitor error rates and performance metrics
   - Update API documentation

**Release Checklist:**

- [ ] Version number updated in `version.json`
- [ ] CHANGELOG.md updated with release notes
- [ ] All tests passing
- [ ] API documentation updated in Swagger
- [ ] Database migrations tested and documented
- [ ] Breaking changes documented (if major version)
- [ ] Rollback plan prepared
- [ ] Monitoring and alerting configured
- [ ] Stakeholders notified of deployment window

### Rollback Procedures

**Immediate Rollback (Critical Issues):**

1. **Identify Issue**
   - Monitor health check endpoints: `/health`, `/health/ready`
   - Check application logs and error rates
   - Validate database connectivity via health checks

2. **Execute Rollback**
   ```bash
   # Kubernetes rollback example
   kubectl rollout undo deployment/blackslope-api
   
   # Docker Swarm rollback example
   docker service update --rollback blackslope-api
   
   # Manual rollback
   docker stop blackslope-container
   docker rm blackslope-container
   docker run -d --name blackslope-container blackslope.api:1.0.0.122
   ```

3. **Verify Rollback**
   - Confirm previous version is running
   - Validate health checks return 200 OK
   - Monitor error rates return to baseline

**Database Rollback Considerations:**

```csharp
// Entity Framework Core migration rollback
dotnet ef database update PreviousMigrationName --project=./src/BlackSlope.Api/BlackSlope.Api.csproj

// Example: Rolling back to specific migration
dotnet ef database update 20190814225754_initialized --project=./src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Rollback Decision Matrix:**

| Issue Severity | Response Time | Action |
|---------------|---------------|---------|
| Critical (P0) | Immediate | Automatic rollback, incident response |
| High (P1) | < 15 minutes | Manual rollback after validation |
| Medium (P2) | < 1 hour | Hotfix deployment or scheduled rollback |
| Low (P3) | Next release | Include fix in next patch version |

**Post-Rollback Actions:**

1. Document root cause in incident report
2. Create hotfix branch from previous stable version
3. Develop and test fix
4. Deploy hotfix as patch version (e.g., 1.0.1)
5. Conduct post-mortem to prevent recurrence

---

## Client Compatibility

### Version Negotiation

While BlackSlope.NET currently uses URL-based versioning (explicit version in path), the architecture supports implementing version negotiation for future requirements.

**Current Approach:**
Clients explicitly specify the API version in the URL:
```
GET https://api.blackslope.com/api/v1/movies
```

**Recommended Client Implementation:**

```csharp
public class BlackSlopeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiVersion;
    
    public BlackSlopeApiClient(HttpClient httpClient, string apiVersion = "v1")
    {
        _httpClient = httpClient;
        _apiVersion = apiVersion;
        _httpClient.BaseAddress = new Uri($"https://api.blackslope.com/api/{_apiVersion}/");
    }
    
    public async Task<List<Movie>> GetMoviesAsync()
    {
        var response = await _httpClient.GetAsync("movies");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Movie>>();
    }
}
```

**Version Discovery Endpoint:**

Implement a version discovery endpoint to help clients determine available API versions:

```csharp
[HttpGet]
[Route("api/versions")]
public ActionResult<ApiVersionInfo> GetVersions()
{
    return new ApiVersionInfo
    {
        CurrentVersion = "1.0.0.0",
        SupportedVersions = new[] { "v1" },
        DeprecatedVersions = new Dictionary<string, string>(),
        LatestVersion = "v1"
    };
}
```

### Backward Compatibility

**Compatibility Guarantees:**

Within a major version (e.g., all v1.x.x releases):
- Existing endpoints remain functional
- Request/response schemas maintain backward compatibility
- New optional fields may be added
- Existing field types and names remain unchanged
- HTTP status codes for existing scenarios remain consistent

**Maintaining Compatibility:**

1. **Additive Changes Only**
   ```csharp
   // Compatible: Adding optional property
   public class MovieViewModel
   {
       public int Id { get; set; }
       public string Title { get; set; }
       public string Director { get; set; }
       public DateTime? ReleaseDate { get; set; } // New optional field - OK
   }
   ```

2. **Avoid Removing Fields**
   ```csharp
   // Incompatible: Removing property (requires major version bump)
   public class MovieViewModel
   {
       public int Id { get; set; }
       public string Title { get; set; }
       // public string Director { get; set; } // REMOVED - BREAKING CHANGE
   }
   ```

3. **Use Nullable Types for New Fields**
   ```csharp
   // New fields should be nullable to maintain compatibility
   public class MovieViewModel
   {
       public int Id { get; set; }
       public string Title { get; set; }
       public string? Genre { get; set; } // Nullable - clients can ignore
   }
   ```

4. **Maintain Validation Rules**
   ```csharp
   // Don't make existing optional fields required
   public class CreateMovieViewModel
   {
       [Required] // Existing requirement - maintain
       public string Title { get; set; }
       
       public string? Director { get; set; } // Optional - keep optional
   }
   ```

**Testing Backward Compatibility:**

```csharp
[Fact]
public async Task GetMovies_V1_ReturnsCompatibleSchema()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/v1/movies");
    var content = await response.Content.ReadAsStringAsync();
    var movies = JsonSerializer.Deserialize<List<MovieViewModel>>(content);
    
    // Assert - Verify all expected v1 fields are present
    Assert.NotNull(movies);
    Assert.All(movies, movie =>
    {
        Assert.NotEqual(0, movie.Id);
        Assert.NotNull(movie.Title);
        // New fields should not break deserialization
    });
}
```

### Client Upgrade Paths

**Recommended Upgrade Strategy:**

1. **Monitor Deprecation Notices**
   - Check response headers for deprecation warnings
   - Subscribe to API changelog notifications
   - Review release notes for each version

2. **Test Against New Version**
   - Use staging/sandbox environment with new API version
   - Run integration test suite against new version
   - Validate all critical workflows

3. **Gradual Migration**
   ```csharp
   // Feature flag approach for gradual migration
   public class MovieService
   {
       private readonly IFeatureManager _featureManager;
       private readonly BlackSlopeApiClient _v1Client;
       private readonly BlackSlopeApiClient _v2Client;
       
       public async Task<List<Movie>> GetMoviesAsync()
       {
           if (await _featureManager.IsEnabledAsync("UseApiV2"))
           {
               return await _v2Client.GetMoviesAsync();
           }
           return await _v1Client.GetMoviesAsync();
       }
   }
   ```

4. **Parallel Running**
   - Run both old and new versions simultaneously
   - Compare results for consistency
   - Monitor error rates and performance

5. **Complete Migration**
   - Switch all traffic to new version
   - Monitor for issues
   - Remove old version client code

**Migration Timeline Example:**

| Phase | Duration | Activities |
|-------|----------|-----------|
| Preparation | 2 weeks | Review changes, update dependencies, plan migration |
| Testing | 2 weeks | Integration testing, performance testing, UAT |
| Canary Deployment | 1 week | 10% traffic to new version, monitor metrics |
| Gradual Rollout | 2 weeks | Increase to 50%, then 100% over time |
| Cleanup | 1 week | Remove old version code, update documentation |

**Client SDK Versioning:**

If providing client SDKs, version them independently:

```
BlackSlope.Client.SDK v1.0.0 -> Supports API v1
BlackSlope.Client.SDK v2.0.0 -> Supports API v2
BlackSlope.Client.SDK v2.1.0 -> Supports API v2 with new features
```

---

## Best Practices

### Clear Versioning Policy

**Documentation Requirements:**

1. **Version in Every Response**
   ```csharp
   public class BaseController : ControllerBase
   {
       protected ActionResult<T> HandleSuccessResponse<T>(T data)
       {
           Response.Headers.Add("X-API-Version", "1.0.0.0");
           Response.Headers.Add("X-API-Supported-Versions", "v1");
           return Ok(data);
       }
   }
   ```

2. **Swagger Documentation**
   ```csharp
   services.AddSwaggerGen(c =>
   {
       c.SwaggerDoc("v1", new OpenApiInfo
       {
           Title = "BlackSlope API",
           Version = "v1",
           Description = "BlackSlope.NET Reference Architecture API",
           Contact = new OpenApiContact
           {
               Name = "BlackSlope Team",
               Email = "support@blackslope.com"
           }
       });
       
       // Include XML comments
       var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
       var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
       c.IncludeXmlComments(xmlPath);
   });
   ```

3. **API Changelog**
   Maintain a detailed CHANGELOG.md:
   ```markdown
   # Changelog
   
   ## [1.0.0] - 2024-01-15
   ### Added
   - Initial release of Movies API
   - GET /api/v1/movies - List all movies
   - GET /api/v1/movies/{id} - Get single movie
   - POST /api/v1/movies - Create movie
   - PUT /api/v1/movies/{id} - Update movie
   - DELETE /api/v1/movies/{id} - Delete movie
   
   ### Security
   - JWT authentication support
   - Azure AD integration
   ```

### Deprecation Warnings

**Implementation Strategy:**

```csharp
public class DeprecationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeprecationMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        
        // Check if endpoint is deprecated
        if (IsDeprecated(path, out var deprecationInfo))
        {
            context.Response.Headers.Add("X-API-Deprecated", "true");
            context.Response.Headers.Add("X-API-Deprecation-Date", 
                deprecationInfo.DeprecationDate.ToString("yyyy-MM-dd"));
            context.Response.Headers.Add("X-API-Sunset-Date", 
                deprecationInfo.SunsetDate.ToString("yyyy-MM-dd"));
            context.Response.Headers.Add("X-API-Deprecation-Info", 
                deprecationInfo.Message);
            
            _logger.LogWarning(
                "Deprecated endpoint accessed: {Path} by {Client}", 
                path, 
                context.Connection.RemoteIpAddress);
        }
        
        await _next(context);
    }
    
    private bool IsDeprecated(string path, out DeprecationInfo info)
    {
        // Check against deprecation registry
        // Return deprecation details if found
    }
}
```

**Deprecation Notice in Swagger:**

```csharp
/// <summary>
/// [DEPRECATED - Will be removed in v3.0.0]
/// Return a list of all movies
/// </summary>
/// <remarks>
/// **DEPRECATION NOTICE**
/// 
/// This endpoint is deprecated and will be removed in version 3.0.0 (March 31, 2025).
/// 
/// **Migration Path:**
/// Please migrate to the new paginated endpoint: GET /api/v2/movies
/// 
/// **Reason for Deprecation:**
/// This endpoint returns all movies without pagination, which can cause performance 
/// issues with large datasets. The v2 endpoint provides pagination support.
/// 
/// **Support Timeline:**
/// - Deprecated: January 1, 2024
/// - Sunset: March 31, 2025
/// </remarks>
[Obsolete("Use GET /api/v2/movies instead. This endpoint will be removed in v3.0.0")]
[HttpGet]
[Route("api/v1/movies/all")]
public async Task<ActionResult<List<MovieViewModel>>> GetAll()
{
    // Implementation
}
```

### Version Documentation

**Comprehensive Documentation Structure:**

```
/docs
  /api
    /v1
      - overview.md
      - authentication.md
      - movies.md
      - errors.md
      - changelog.md
    /v2
      - overview.md
      - migration-guide.md
      - movies.md
      - errors.md
      - changelog.md
  /versioning
    - strategy.md (this document)
    - deprecation-policy.md
    - upgrade-guide.md
```

**Version-Specific Documentation:**

Each API version should have:
- Complete endpoint reference
- Request/response examples
- Authentication requirements
- Error codes and handling
- Rate limiting information
- Migration guide from previous version

**Automated Documentation Generation:**

```csharp
// Configure Swagger for multiple versions
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "BlackSlope API v1", 
        Version = "v1",
        Description = "Version 1 of the BlackSlope API"
    });
    
    c.SwaggerDoc("v2", new OpenApiInfo 
    { 
        Title = "BlackSlope API v2", 
        Version = "v2",
        Description = "Version 2 of the BlackSlope API with enhanced features"
    });
    
    c.DocInclusionPredicate((version, apiDesc) =>
    {
        var actionApiVersionModel = apiDesc.ActionDescriptor
            .GetApiVersionModel(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);
        
        if (actionApiVersionModel == null)
            return true;
        
        return actionApiVersionModel.DeclaredApiVersions.Any(v => 
            $"v{v.ToString()}" == version);
    });
});

// Configure Swagger UI
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API v1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "BlackSlope API v2");
});
```

### Monitoring and Metrics

**Version Usage Tracking:**

```csharp
public class VersionMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsCollector _metrics;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var version = ExtractVersionFromPath(context.Request.Path);
        
        _metrics.IncrementCounter($"api.version.{version}.requests");
        
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();
        
        _metrics.RecordHistogram(
            $"api.version.{version}.duration", 
            sw.ElapsedMilliseconds);
        
        _metrics.IncrementCounter(
            $"api.version.{version}.status.{context.Response.StatusCode}");
    }
}
```

**Key Metrics to Track:**

- Request count per API version
- Error rate per API version
- Response time per API version
- Deprecated endpoint usage
- Client version distribution
- Migration progress (% of traffic on new version)

### Testing Strategy

**Version-Specific Tests:**

```csharp
public class MoviesV1ControllerTests
{
    [Fact]
    public async Task GetMovies_V1_ReturnsExpectedSchema()
    {
        // Arrange
        var controller = CreateController();
        
        // Act
        var result = await controller.Get();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var movies = Assert.IsType<List<MovieViewModel>>(okResult.Value);
        Assert.NotEmpty(movies);
    }
    
    [Fact]
    public async Task GetMovies_V1_MaintainsBackwardCompatibility()
    {
        // Test that v1 response schema hasn't changed
        // Verify all required fields are present
        // Ensure no breaking changes in field types
    }
}
```

**Contract Testing:**

```csharp
[Fact]
public async Task ApiContract_V1_RemainsStable()
{
    // Load expected contract from file
    var expectedContract = LoadContract("v1-movies-contract.json");
    
    // Get actual response
    var response = await _client.GetAsync("/api/v1/movies");
    var actualContract = await response.Content.ReadAsStringAsync();
    
    // Compare contracts
    var differences = JsonContractComparer.Compare(expectedContract, actualContract);
    
    Assert.Empty(differences);
}
```

---

## Related Documentation

For more information on related topics, please refer to:

- [Features: Versioning](/features/versioning.md) - Detailed feature documentation for versioning capabilities
- [API Reference: Version API](/api_reference/version_api.md) - Complete API reference for version endpoints
- [Deployment: Production Best Practices](/deployment/production_best_practices.md) - Production deployment guidelines including versioning considerations

---

## Summary

BlackSlope.NET implements a robust versioning strategy that balances flexibility with stability:

- **Semantic Versioning** provides clear communication of change impact
- **URL-based API versioning** offers explicit, discoverable version management
- **Multiple tagging strategies** support various deployment scenarios
- **Structured deprecation process** gives clients time to migrate
- **Comprehensive documentation** ensures developers understand version implications

By following these versioning practices, the BlackSlope.NET application maintains a stable API contract while allowing for evolution and improvement over time. The strategy supports both rapid development and long-term maintainability, ensuring that API consumers can confidently integrate with and depend on the platform.