# Controllers and Routing

## Overview

The BlackSlope API implements a layered controller architecture built on ASP.NET Core 6.0, utilizing attribute-based routing, comprehensive error handling, and standardized response patterns. Controllers inherit from a common base class that provides consistent HTTP response handling across all API endpoints.

## Controller Architecture

### Base Controller Implementation

All API controllers inherit from `BaseController`, which provides standardized methods for handling HTTP responses with appropriate status codes and error formatting.

```csharp
[EnableCors("AllowSpecificOrigin")]
public class BaseController : Controller
{
    protected ActionResult HandleSuccessResponse(object data, HttpStatusCode status = HttpStatusCode.OK)
    {
        return StatusCode((int)status, data);
    }

    protected ActionResult HandleCreatedResponse(object data, HttpStatusCode status = HttpStatusCode.Created)
    {
        return StatusCode((int)status, data);
    }

    protected ActionResult HandleErrorResponse(HttpStatusCode httpStatus, string message)
    {
        var response = new ApiResponse()
        {
            Errors = new List<ApiError>
            {
                new ApiError
                {
                    Code = (int)httpStatus,
                    Message = message,
                }
            }
        };

        return StatusCode((int)httpStatus, response);
    }

    protected ActionResult HandleDeletedResponse()
    {
        return StatusCode((int)HttpStatusCode.NoContent);
    }
}
```

**Key Features:**

- **CORS Support**: The `[EnableCors("AllowSpecificOrigin")]` attribute is applied at the base controller level, enabling cross-origin requests for all derived controllers
- **Consistent Response Handling**: Protected methods ensure uniform response structures across all endpoints
- **Structured Error Responses**: Errors are wrapped in an `ApiResponse` object containing a list of `ApiError` objects for consistent client-side error handling
- **HTTP Status Code Abstraction**: Methods accept `HttpStatusCode` enums rather than integer values, improving code readability and type safety

### Controller Responsibilities

Controllers in the BlackSlope API follow a clear separation of concerns:

1. **Request Validation**: Validate incoming requests using FluentValidation through the `IBlackSlopeValidator` interface
2. **Model Mapping**: Transform view models to domain models using AutoMapper
3. **Service Orchestration**: Delegate business logic to service layer components
4. **Response Formatting**: Map domain models back to view models and wrap in appropriate HTTP responses
5. **Exception Handling**: Rely on global filters for exception handling rather than try-catch blocks

**Example Controller Structure:**

```csharp
public class MoviesController : BaseController
{
    private readonly IMapper _mapper;
    private readonly IMovieService _movieService;
    private readonly IBlackSlopeValidator _blackSlopeValidator;

    public MoviesController(IMovieService movieService, IMapper mapper, IBlackSlopeValidator blackSlopeValidator)
    {
        _mapper = mapper;
        _blackSlopeValidator = blackSlopeValidator;
        _movieService = movieService;
    }
    
    // Action methods...
}
```

**Dependency Injection Pattern:**

- Controllers receive dependencies through constructor injection
- All dependencies are registered as interfaces, promoting testability and loose coupling
- The ASP.NET Core DI container manages the lifecycle of injected services

### Action Method Patterns

The API implements consistent patterns across all action methods:

#### GET Operations (Retrieve Resources)

```csharp
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpGet]
[Route("api/v1/movies")]
public async Task<ActionResult<List<MovieViewModel>>> Get()
{
    // 1. Retrieve data from service layer
    var movies = await _movieService.GetAllMoviesAsync();

    // 2. Map domain models to view models
    var response = _mapper.Map<List<MovieViewModel>>(movies);

    // 3. Return standardized success response
    return HandleSuccessResponse(response);
}
```

#### GET by ID Operations

```csharp
[HttpGet]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Get(int id)
{
    var movie = await _movieService.GetMovieAsync(id);
    var response = _mapper.Map<MovieViewModel>(movie);
    return HandleSuccessResponse(response);
}
```

#### POST Operations (Create Resources)

```csharp
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    // 1. Wrap view model in request object for validation
    var request = new CreateMovieRequest { Movie = viewModel };

    // 2. Validate request using FluentValidation
    await _blackSlopeValidator.AssertValidAsync(request);

    // 3. Map view model to domain model
    var movie = _mapper.Map<MovieDomainModel>(viewModel);

    // 4. Execute business logic
    var createdMovie = await _movieService.CreateMovieAsync(movie);

    // 5. Map result to view model
    var response = _mapper.Map<MovieViewModel>(createdMovie);

    // 6. Return 201 Created response
    return HandleCreatedResponse(response);
}
```

#### PUT Operations (Update Resources)

```csharp
[HttpPut]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
{
    Contract.Requires(viewModel != null);
    
    // Create request wrapper for validation
    var request = new UpdateMovieRequest { Movie = viewModel, Id = id };
    await _blackSlopeValidator.AssertValidAsync(request);

    // Support ID in URL, body, or both (URL takes precedence)
    viewModel.Id = id ?? viewModel.Id;

    var movie = _mapper.Map<MovieDomainModel>(viewModel);
    var updatedMovie = await _movieService.UpdateMovieAsync(movie);
    var response = _mapper.Map<MovieViewModel>(updatedMovie);

    return HandleSuccessResponse(response);
}
```

**Important Note**: The PUT operation accepts the ID from both the URL route parameter and the request body. The URL parameter takes precedence if both are provided.

#### DELETE Operations

```csharp
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpDelete]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<DeletedMovieResponse>> Delete(int id)
{
    await _movieService.DeleteMovieAsync(id);
    
    // Return 204 No Content (standard for successful DELETE)
    return HandleDeletedResponse();
}
```

## Routing Configuration

### Attribute Routing

The BlackSlope API exclusively uses attribute-based routing rather than convention-based routing. This approach provides explicit control over route definitions and makes the API structure immediately visible in the controller code.

**Route Pattern Structure:**

```
api/v{version}/{resource}/{id?}
```

**Examples:**

| HTTP Method | Route | Purpose |
|-------------|-------|---------|
| GET | `api/v1/movies` | Retrieve all movies |
| GET | `api/v1/movies/{id}` | Retrieve specific movie |
| POST | `api/v1/movies` | Create new movie |
| PUT | `api/v1/movies/{id}` | Update existing movie |
| DELETE | `api/v1/movies/{id}` | Delete movie |

### API Versioning in Routes

The API implements URL-based versioning with the version number embedded in the route path:

```csharp
[Route("api/v1/movies")]
```

**Versioning Strategy:**

- **Current Version**: v1 is the active API version
- **Version Format**: Major version only (v1, v2, etc.)
- **Breaking Changes**: New major versions should be created for breaking changes
- **Deprecation**: Old versions can coexist with new versions during transition periods

For more information on versioning strategy and implementation, see [API Versioning](/features/versioning.md).

### Route Constraints and Parameters

**Route Parameters:**

```csharp
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Get(int id)
```

- Route parameters are strongly typed (e.g., `int id`)
- ASP.NET Core automatically performs type conversion and validation
- Invalid types result in 404 Not Found responses

**Optional Parameters:**

```csharp
public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
```

- Nullable types (`int?`) indicate optional route parameters
- The application logic handles the presence or absence of the parameter

## Request/Response Handling

### Model Binding

The API uses multiple model binding sources:

#### FromBody Binding

```csharp
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
```

- Used for complex objects in POST/PUT requests
- Expects JSON payload in request body
- Content-Type header should be `application/json`
- Automatically deserializes JSON to C# objects

#### FromRoute Binding

```csharp
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Get(int id)
```

- Binds route parameters to action method parameters
- Parameter names must match route template placeholders
- Supports type conversion and validation

#### Hybrid Binding

```csharp
public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
```

- Combines route parameters with body content
- Useful for update operations where ID can be specified in URL or body

### Content Negotiation

The API is configured to handle JSON content negotiation:

**Request Headers:**
```
Content-Type: application/json
Accept: application/json
```

**Response Content Type:**

All responses are returned as `application/json` with UTF-8 encoding, as configured in the `HandledResultFilter`:

```csharp
context.HttpContext.Response.ContentType = new MediaTypeHeaderValue("application/json").ToString();
context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(error), Encoding.UTF8);
```

### Response Formatting

#### Success Responses

**200 OK (Retrieve/Update):**
```json
{
  "id": 1,
  "title": "The Matrix",
  "releaseYear": 1999
}
```

**201 Created (Create):**
```json
{
  "id": 42,
  "title": "New Movie",
  "releaseYear": 2024
}
```

**204 No Content (Delete):**
- Empty response body
- Status code indicates successful deletion

#### Error Responses

Errors follow a standardized structure defined by the `ApiResponse` and `ApiError` classes:

```json
{
  "errors": [
    {
      "code": 400,
      "message": "Validation failed for Movie.Title: Title is required"
    }
  ]
}
```

**Multiple Errors:**
```json
{
  "errors": [
    {
      "code": 400,
      "message": "Title is required"
    },
    {
      "code": 400,
      "message": "Release year must be between 1888 and 2100"
    }
  ]
}
```

## Filters and Attributes

The API implements a comprehensive filter pipeline for cross-cutting concerns. Filters are registered globally in the MVC configuration:

```csharp
public static IServiceCollection AddMvcApi(this IServiceCollection services)
{
    services
        .AddMvc(mvcOptions =>
        {
            mvcOptions.Filters.Add(new ModelStateValidationFilter());
            mvcOptions.Filters.Add(new HandledResultFilter());
        });

    return services;
}
```

### Action Filters

#### ModelStateValidationFilter

Executes before action methods to validate model state and request models:

```csharp
public class ModelStateValidationFilter : ActionFilterAttribute
{
    private const string ErrorName = "RequestModel";
    private const string ErrorCode = "MSTATE001";
    private const string ErrorText = "ModelState did not pass validation.";
    private const string ErrorDescription = "Unable to create request model...";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var modelState = context.ModelState;

        // Check for null request models
        if (context.ActionArguments.Any(kv => kv.Value == null))
        {
            context.ModelState.AddModelError(ErrorName, ErrorDescription);
        }

        // Validate model state
        if (!modelState.IsValid)
        {
            var exceptions = new List<HandledException>();
            foreach (var state in modelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    exceptions.Add(new HandledException(
                        ExceptionType.Validation, 
                        error.ErrorMessage, 
                        System.Net.HttpStatusCode.BadRequest, 
                        ErrorCode));
                }
            }

            throw new HandledException(ExceptionType.Validation, ErrorText, exceptions);
        }

        base.OnActionExecuting(context);
    }
}
```

**Key Behaviors:**

1. **Null Model Detection**: Automatically detects when request models fail to bind (null values)
2. **Model State Validation**: Checks ASP.NET Core's built-in model state validation
3. **Error Aggregation**: Collects all validation errors into a single exception
4. **Consistent Error Format**: Converts validation errors to `HandledException` objects for uniform error handling

**Common Validation Scenarios:**

- Missing required fields
- Invalid data types
- Malformed JSON
- Missing `[FromBody]` or `[FromRoute]` attributes
- Data annotation validation failures

For detailed validation rules and custom validators, see [Validation Documentation](/features/validation.md).

### Exception Filters

#### HandledResultFilter

Global exception filter that catches all unhandled exceptions and formats them consistently:

```csharp
public class HandledResultFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        context.ExceptionHandled = true;
        var error = new HandledResult<Exception>(context.Exception).HandleException();

        context.HttpContext.Response.Clear();
        context.HttpContext.Response.ContentType = new MediaTypeHeaderValue("application/json").ToString();
        context.HttpContext.Response.StatusCode = error.StatusCode;
        context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(error), Encoding.UTF8);
    }
}
```

**Exception Handling Flow:**

1. **Exception Capture**: Intercepts any exception thrown during request processing
2. **Exception Processing**: Delegates to `HandledResult<Exception>` for exception analysis
3. **Response Formatting**: Converts exception to JSON error response
4. **Status Code Mapping**: Sets appropriate HTTP status code based on exception type
5. **Response Writing**: Writes formatted error to response stream

**Supported Exception Types:**

The `HandledException` class supports multiple exception types:

```csharp
public enum ExceptionType
{
    Validation,
    Security,
    NotFound,
    BusinessLogic,
    Infrastructure
}
```

**Example Exception Usage:**

```csharp
[HttpGet]
[Route("SampleError")]
public object SampleError()
{
    throw new HandledException(
        ExceptionType.Security, 
        "This is an example security issue.", 
        System.Net.HttpStatusCode.RequestEntityTooLarge);
}
```

### Result Filters

While not explicitly implemented in the provided source files, the architecture supports result filters for post-processing action results before they're sent to the client. The base controller's response handling methods serve a similar purpose by standardizing result formatting.

### Authorization Attributes

The codebase includes commented-out authorization attributes, indicating planned security implementation:

```csharp
// TODO: enable this once authentication middleware has been configured
// [Authorize]
public class MoviesController : BaseController
```

**Planned Authorization:**

- JWT-based authentication using `System.IdentityModel.Tokens.Jwt` (v7.7.1)
- Azure AD integration via `Azure.Identity` (v1.14.2)
- Role-based access control (RBAC) for endpoint protection

**When Enabled:**

- Unauthenticated requests will receive 401 Unauthorized responses
- Authorization policies can be applied at controller or action level
- Claims-based authorization for fine-grained access control

### Response Type Attributes

All action methods include `[ProducesResponseType]` attributes for API documentation and OpenAPI/Swagger generation:

```csharp
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
```

**Benefits:**

- **Swagger Documentation**: Automatically generates accurate API documentation
- **Client Code Generation**: Enables strongly-typed client generation tools
- **Developer Guidance**: Documents expected response codes for each endpoint
- **Testing Support**: Provides clear expectations for integration tests

## Advanced Patterns

### Resilience Testing Endpoint

The Movies controller includes a specialized endpoint for testing HTTP resilience patterns with Polly:

```csharp
[HttpGet]
[Route("api/v1/movies/httpExponentialBackoffTest")]
public async Task<ActionResult> GetExponentialBackoff()
{
    await _movieService.GetExponentialBackoff();
    return HandleSuccessResponse(null);
}
```

This endpoint demonstrates the integration of Polly retry policies with exponential backoff for handling transient failures in downstream HTTP services. For more information on resilience patterns, see the service layer documentation.

### Contract Validation

Some action methods use Code Contracts for runtime validation:

```csharp
Contract.Requires(viewModel != null);
```

**Note**: While Code Contracts provide additional runtime safety, the `ModelStateValidationFilter` already handles null model validation. Consider removing redundant contract checks to avoid duplicate validation logic.

## Best Practices and Gotchas

### Controller Design Guidelines

1. **Keep Controllers Thin**: Controllers should only handle HTTP concerns; delegate business logic to services
2. **Use Async/Await**: All action methods should be asynchronous for better scalability
3. **Consistent Naming**: Follow RESTful conventions (Get, Post, Put, Delete)
4. **Explicit Routes**: Always use `[Route]` attributes; avoid relying on conventions
5. **Response Type Documentation**: Include `[ProducesResponseType]` for all possible responses

### Common Pitfalls

**1. Mixing Route Parameter Sources**

```csharp
// ❌ Avoid: Ambiguous ID source
public async Task<ActionResult> Update(int id, [FromBody] MovieViewModel model)
{
    // Which ID should be used if model.Id differs from route id?
}

// ✅ Prefer: Explicit precedence
public async Task<ActionResult> Update(int? id, [FromBody] MovieViewModel model)
{
    model.Id = id ?? model.Id; // URL takes precedence
}
```

**2. Forgetting Validation Wrappers**

```csharp
// ❌ Avoid: Direct view model validation
public async Task<ActionResult> Post([FromBody] CreateMovieViewModel viewModel)
{
    await _blackSlopeValidator.AssertValidAsync(viewModel); // Won't work
}

// ✅ Prefer: Request wrapper for validation
public async Task<ActionResult> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };
    await _blackSlopeValidator.AssertValidAsync(request);
}
```

**3. Inconsistent Error Handling**

```csharp
// ❌ Avoid: Manual try-catch in controllers
public async Task<ActionResult> Get(int id)
{
    try
    {
        var movie = await _movieService.GetMovieAsync(id);
        return Ok(movie);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message);
    }
}

// ✅ Prefer: Let global filters handle exceptions
public async Task<ActionResult> Get(int id)
{
    var movie = await _movieService.GetMovieAsync(id);
    var response = _mapper.Map<MovieViewModel>(movie);
    return HandleSuccessResponse(response);
}
```

### Performance Considerations

1. **Async All the Way**: Ensure the entire call stack is asynchronous to avoid thread pool starvation
2. **AutoMapper Performance**: Consider using projection queries for large datasets
3. **Response Caching**: Implement response caching for frequently accessed, rarely changing data
4. **Pagination**: Always implement pagination for collection endpoints to prevent large payload transfers

### Testing Recommendations

1. **Unit Test Controllers**: Mock service dependencies and test routing, validation, and response formatting
2. **Integration Tests**: Test the full request pipeline including filters and middleware
3. **Contract Tests**: Verify API contracts match documentation and client expectations
4. **Load Testing**: Validate performance under concurrent request loads

## Related Documentation

- [Architecture Layers](/architecture/layers.md) - Understanding the layered architecture and controller responsibilities
- [Validation](/features/validation.md) - Detailed validation rules and FluentValidation implementation
- [API Versioning](/features/versioning.md) - Versioning strategy and migration guidelines
- [Movies API Reference](/api_reference/movies_api.md) - Complete API endpoint documentation

## Summary

The BlackSlope API controller architecture provides a robust, maintainable foundation for RESTful API development. Key takeaways:

- **Standardized Responses**: Base controller ensures consistent response formatting
- **Global Error Handling**: Filters provide centralized exception handling and validation
- **Attribute Routing**: Explicit route definitions improve clarity and maintainability
- **Separation of Concerns**: Controllers focus on HTTP concerns while services handle business logic
- **Comprehensive Documentation**: Response type attributes enable automatic API documentation generation

This architecture supports the application's requirements for scalability, maintainability, and developer productivity while adhering to ASP.NET Core best practices.