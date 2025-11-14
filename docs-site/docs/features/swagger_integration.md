# Swagger/OpenAPI Integration

The BlackSlope API implements comprehensive Swagger/OpenAPI documentation using Swashbuckle.AspNetCore (version 6.3.0), providing interactive API documentation and testing capabilities for all endpoints. This integration enables automatic API discovery, schema generation, and a user-friendly interface for developers and consumers of the API.

## Swagger Configuration

### SwaggerUI Setup

The Swagger integration is configured through the `BlackSlopeServiceCollectionExtensions` class, which provides extension methods for streamlined service registration. The configuration is driven by the `SwaggerConfig` class, which encapsulates all necessary settings:

```csharp
public class SwaggerConfig
{
    public string Version { get; set; }
    public string ApplicationName { get; set; }
    public string XmlFile { get; set; }
}
```

The `AddSwagger` extension method registers and configures Swagger services:

```csharp
public static IServiceCollection AddSwagger(this IServiceCollection services, SwaggerConfig swaggerConfig) =>
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(swaggerConfig.Version, new OpenApiInfo 
        { 
            Title = swaggerConfig.ApplicationName, 
            Version = swaggerConfig.Version 
        });
        options.DocumentFilter<DocumentFilterAddHealth>();
        AddSecurityDefinition(options);
        AddSecurityRequirement(options);
        SetDocumentPath(swaggerConfig, options);
    });
```

**Key Configuration Elements:**

- **Version Management**: The API version is dynamically configured through the `SwaggerConfig.Version` property, enabling version-specific documentation
- **Application Naming**: The `ApplicationName` property sets the title displayed in the Swagger UI
- **XML Documentation**: The `XmlFile` property specifies the XML documentation file for enhanced API descriptions

### Service Registration in Startup

The Swagger service is registered in the `Startup.ConfigureServices` method:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvcService();
    ApplicationConfiguration(services);
    CorsConfiguration(services);

    services.AddSwagger(HostConfig.Swagger);
    services.AddAzureAd(HostConfig.AzureAd);
    // ... additional service registrations
}
```

The `HostConfig.Swagger` property provides the configuration values loaded from application settings (see [Application Settings](/configuration/application_settings.md) for configuration details).

### Middleware Configuration

Swagger UI is enabled in the request pipeline through the `Configure` method:

```csharp
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

    HealthCheckStartup.Configure(app, env, HostConfig);
    app.UseHttpsRedirection();

    app.UseSwagger(HostConfig.Swagger);  // Enables Swagger middleware

    app.UseRouting();
    app.UseCors("AllowSpecificOrigin");
    // ... additional middleware
}
```

## API Documentation Generation

### Automatic Endpoint Discovery

Swagger automatically discovers all controller endpoints decorated with routing attributes. The system scans for:

- HTTP method attributes (`[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`)
- Route templates (`[Route("api/v1/movies")]`)
- Response type declarations (`[ProducesResponseType]`)

### XML Documentation Comments

The integration leverages XML documentation comments to generate rich API descriptions. The XML file path is configured dynamically:

```csharp
private static void SetDocumentPath(SwaggerConfig swaggerConfig, SwaggerGenOptions options)
{
    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = swaggerConfig.XmlFile;
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
}
```

**Important**: Ensure that XML documentation generation is enabled in your project file:

```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### Request/Response Schemas

Controllers use comprehensive XML documentation and response type attributes to generate accurate schemas. Example from `MoviesController`:

```csharp
/// <summary>
/// Return a list of all movies
/// </summary>
/// <remarks>
/// Use this operation to return a list of all movies
/// </remarks>
/// <response code="200">Returns a list of all movies</response>
/// <response code="401">Unauthorized</response>
/// <response code="500">Internal Server Error</response>
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpGet]
[Route("api/v1/movies")]
public async Task<ActionResult<List<MovieViewModel>>> Get()
{
    var movies = await _movieService.GetAllMoviesAsync();
    var response = _mapper.Map<List<MovieViewModel>>(movies);
    return HandleSuccessResponse(response);
}
```

**Documentation Best Practices:**

1. **Summary**: Provide a concise one-line description of the endpoint's purpose
2. **Remarks**: Include detailed usage instructions and business logic context
3. **Response Codes**: Document all possible HTTP status codes with descriptions
4. **ProducesResponseType**: Explicitly declare response types for accurate schema generation

### Example Values and Schema Generation

Swagger automatically generates example values based on:

- Property types and nullability
- Data annotations (e.g., `[Required]`, `[Range]`, `[StringLength]`)
- Default values specified in model classes
- Custom example providers (if implemented)

For complex request bodies, consider the `CreateMovieViewModel` example:

```csharp
/// <summary>
/// Create a new movie
/// </summary>
/// <remarks>
/// Use this operation to create a new movie
/// </remarks>
/// <response code="201">Movie successfully created, will return the new movie</response>
/// <response code="400">Bad Request</response>
/// <response code="401">Unauthorized</response>
/// <response code="500">Internal Server Error</response>
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };
    await _blackSlopeValidator.AssertValidAsync(request);
    
    var movie = _mapper.Map<MovieDomainModel>(viewModel);
    var createdMovie = await _movieService.CreateMovieAsync(movie);
    var response = _mapper.Map<MovieViewModel>(createdMovie);
    
    return HandleCreatedResponse(response);
}
```

## Configuration Options

### Swagger Configuration Class

The `SwaggerConfig` class serves as the central configuration model for all Swagger-related settings. This class should be populated from application configuration files:

```json
{
  "BlackSlope.Api": {
    "Swagger": {
      "Version": "v1",
      "ApplicationName": "BlackSlope API",
      "XmlFile": "BlackSlope.Api.xml"
    }
  }
}
```

**Configuration Properties:**

| Property | Type | Description | Example |
|----------|------|-------------|---------|
| `Version` | string | API version identifier used in Swagger document generation | "v1", "v2" |
| `ApplicationName` | string | Display name shown in Swagger UI header | "BlackSlope API" |
| `XmlFile` | string | Name of the XML documentation file | "BlackSlope.Api.xml" |

### Endpoint Customization

Endpoints can be customized through various attributes and conventions:

**Route Versioning:**
```csharp
[Route("api/v1/movies")]  // Version included in route
[Route("api/v1/movies/{id}")]  // Route parameters
```

**Operation IDs:**
Swagger generates operation IDs automatically, but these can be customized:
```csharp
[HttpGet]
[Route("api/v1/movies")]
// Generated OperationId: "Movies_Get"
```

**Tags:**
Group related endpoints using controller names or explicit tags:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class MoviesController : BaseController
{
    // All endpoints automatically tagged with "Movies"
}
```

### Health Check Documentation

The application includes a custom document filter to add health check endpoints to the Swagger documentation:

```csharp
public class DocumentFilterAddHealth : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        Contract.Requires(swaggerDoc != null);
        swaggerDoc.Paths.Add("/health", HealthPathItem());
    }

    private static OpenApiPathItem HealthPathItem()
    {
        var pathItem = new OpenApiPathItem();
        pathItem.AddOperation(OperationType.Get, new OpenApiOperation
        {
            Tags = new List<OpenApiTag>() { new OpenApiTag() { Name = "Health" } },
            OperationId = "Health_Get",
            Responses = new OpenApiResponses()
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "OK",
                },
            },
        });

        return pathItem;
    }
}
```

**Why This Is Necessary:**

Health check endpoints are typically registered through ASP.NET Core's health check middleware rather than MVC controllers. Since Swagger only discovers MVC endpoints by default, this document filter manually adds the `/health` endpoint to the generated OpenAPI specification.

**Registration:**
```csharp
options.DocumentFilter<DocumentFilterAddHealth>();
```

## Customization

### Document Filters for Health Checks

The `DocumentFilterAddHealth` class demonstrates the document filter pattern for adding non-controller endpoints to Swagger documentation. This pattern can be extended for other middleware-based endpoints:

**Implementation Pattern:**

1. Implement `IDocumentFilter` interface
2. Override the `Apply` method to modify the `OpenApiDocument`
3. Create `OpenApiPathItem` and `OpenApiOperation` objects
4. Add to the document's `Paths` collection
5. Register the filter in Swagger configuration

**Extension Example:**

To add additional middleware endpoints (e.g., metrics, diagnostics):

```csharp
public class DocumentFilterAddDiagnostics : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Paths.Add("/diagnostics", CreateDiagnosticsPathItem());
    }

    private static OpenApiPathItem CreateDiagnosticsPathItem()
    {
        var pathItem = new OpenApiPathItem();
        pathItem.AddOperation(OperationType.Get, new OpenApiOperation
        {
            Tags = new List<OpenApiTag>() { new OpenApiTag() { Name = "Diagnostics" } },
            OperationId = "Diagnostics_Get",
            Responses = new OpenApiResponses()
            {
                ["200"] = new OpenApiResponse { Description = "Diagnostics information" },
            },
        });
        return pathItem;
    }
}
```

### Custom Operation Filters

While not currently implemented in the codebase, operation filters provide fine-grained control over individual endpoint documentation. Common use cases include:

**Adding Custom Headers:**
```csharp
public class AddCorrelationIdHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-ID",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Correlation ID for request tracking",
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
```

**Registration:**
```csharp
options.OperationFilter<AddCorrelationIdHeaderFilter>();
```

### Security Configuration

The application implements JWT Bearer authentication in Swagger, allowing users to test authenticated endpoints directly from the UI:

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
                Reference = new OpenApiReference 
                { 
                    Type = ReferenceType.SecurityScheme, 
                    Id = "oauth2" 
                },
            },
            new[] { "readAccess", "writeAccess" }
        },
    });
```

**Using Authentication in Swagger UI:**

1. Click the "Authorize" button in the Swagger UI
2. Enter your JWT token in the format: `Bearer {your-token}`
3. Click "Authorize" to apply the token to all requests
4. The token will be included in the `Authorization` header for all subsequent API calls

**Note**: The `MoviesController` currently has authentication disabled:
```csharp
// TODO: enable this once authentication middleware has been configured
// [Authorize]
public class MoviesController : BaseController
```

Once authentication is enabled, the `[Authorize]` attribute should be uncommented to enforce authentication requirements.

### UI Theming and Customization

While the current implementation uses default Swagger UI styling, the UI can be customized through additional configuration:

**Custom CSS:**
```csharp
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API v1");
    c.InjectStylesheet("/swagger-ui/custom.css");
});
```

**Custom JavaScript:**
```csharp
c.InjectJavascript("/swagger-ui/custom.js");
```

**Index Page Customization:**
```csharp
c.IndexStream = () => GetType().Assembly
    .GetManifestResourceStream("BlackSlope.Api.SwaggerIndex.html");
```

## Integration with Other Components

### Relationship with Movies API

The Swagger documentation automatically reflects all endpoints defined in the `MoviesController`. For detailed information about the Movies API functionality, see [Movies API Reference](/api_reference/movies_api.md).

**Documented Endpoints:**

- `GET /api/v1/movies` - Retrieve all movies
- `GET /api/v1/movies/{id}` - Retrieve a specific movie
- `POST /api/v1/movies` - Create a new movie
- `PUT /api/v1/movies/{id}` - Update an existing movie
- `DELETE /api/v1/movies/{id}` - Delete a movie
- `GET /api/v1/movies/httpExponentialBackoffTest` - Test Polly retry policies

### Configuration Dependencies

Swagger configuration is loaded from application settings during startup. The configuration structure must match the `SwaggerConfig` class properties. For complete configuration details, see [Application Settings](/configuration/application_settings.md).

### Quick Start Integration

For developers getting started with the API, Swagger UI provides an interactive interface for exploring and testing endpoints without writing code. See [Quick Start Guide](/getting_started/quick_start.md) for initial setup instructions.

## Troubleshooting and Common Issues

### XML Documentation Not Appearing

**Symptom**: Endpoint descriptions are missing or incomplete in Swagger UI.

**Solutions:**
1. Verify XML documentation generation is enabled in the project file
2. Ensure the XML file name in `SwaggerConfig.XmlFile` matches the generated file
3. Check that the XML file is being copied to the output directory
4. Verify the file path resolution in `SetDocumentPath` method

### Security Scheme Not Working

**Symptom**: Authorization button appears but tokens aren't being sent with requests.

**Solutions:**
1. Verify the security scheme ID ("oauth2") matches between definition and requirement
2. Ensure the token format includes "Bearer " prefix
3. Check that the `Authorization` header is not being overridden by other middleware
4. Confirm Azure AD configuration is correct (see `AddAzureAd` method)

### Health Check Endpoint Not Appearing

**Symptom**: The `/health` endpoint is not visible in Swagger documentation.

**Solutions:**
1. Verify `DocumentFilterAddHealth` is registered in Swagger configuration
2. Check that the health check middleware is properly configured
3. Ensure the document filter's `Apply` method is being called
4. Validate that `Contract.Requires` is not throwing exceptions

### Version Conflicts

**Symptom**: Swagger UI shows incorrect or multiple versions of the API.

**Solutions:**
1. Ensure only one `SwaggerDoc` is registered per version
2. Verify the version string in `SwaggerConfig.Version` is consistent
3. Check for duplicate Swagger middleware registrations
4. Validate that the version routing matches the Swagger document version

## Best Practices

1. **Always Document Response Types**: Use `[ProducesResponseType]` attributes for all possible responses
2. **Provide Meaningful Descriptions**: Write clear, concise XML documentation comments
3. **Use Consistent Versioning**: Maintain version consistency across routes, Swagger docs, and configuration
4. **Secure Sensitive Endpoints**: Apply `[Authorize]` attributes to protected endpoints
5. **Test Through Swagger UI**: Regularly verify that Swagger documentation accurately reflects API behavior
6. **Keep XML Documentation Updated**: Update XML comments whenever endpoint behavior changes
7. **Use Document Filters Sparingly**: Only add non-controller endpoints when absolutely necessary
8. **Validate Configuration**: Ensure `SwaggerConfig` values are properly loaded from application settings

## Performance Considerations

- **XML Documentation Loading**: XML files are loaded once at startup; large documentation files may impact startup time
- **Swagger Generation**: OpenAPI document generation occurs at startup and is cached; minimal runtime overhead
- **UI Assets**: Swagger UI assets are served as static files; consider CDN hosting for production environments
- **Security Overhead**: JWT validation adds minimal overhead per request when authentication is enabled

## Future Enhancements

Potential improvements to the Swagger integration:

1. **Multiple API Versions**: Support for side-by-side documentation of multiple API versions
2. **Custom Example Providers**: Implement `IExamplesProvider` for more realistic example data
3. **Schema Filters**: Add schema filters for custom type documentation
4. **Response Examples**: Include actual response examples in documentation
5. **OAuth2 Flow Configuration**: Implement full OAuth2 authorization code flow in Swagger UI
6. **API Deprecation Notices**: Add custom attributes and filters to mark deprecated endpoints