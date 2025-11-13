# Error Responses

## Overview

The BlackSlope API implements a comprehensive error handling system that provides consistent, structured error responses across all endpoints. The system uses custom exception types, standardized error models, and HTTP status codes to communicate failures to API consumers in a predictable and actionable manner.

The error handling architecture is built around three core components:
- **ApiResponse**: A wrapper model that encapsulates both successful data and error information
- **ApiException**: A custom exception type that carries structured error details and HTTP status codes
- **HandledException**: A specialized exception for business logic errors with support for nested exceptions

## Standard Error Format

All API error responses follow a consistent structure using the `ApiResponse` model, which provides a unified contract for both success and failure scenarios.

### ApiResponse Structure

```csharp
public class ApiResponse
{
    public object Data { get; set; }
    public IEnumerable<ApiError> Errors { get; set; }
}
```

**Properties:**
- `Data`: Contains the response payload for successful requests; typically `null` for error responses
- `Errors`: A collection of `ApiError` objects describing one or more errors that occurred

### ApiError Model

Each individual error is represented by the `ApiError` structure:

```csharp
public class ApiError
{
    public int Code { get; set; }
    public string Message { get; set; }
}
```

**Properties:**
- `Code`: A numeric error code that uniquely identifies the error type (see [Error Code Conventions](#error-code-conventions))
- `Message`: A human-readable description of the error

### Example Error Response

```json
{
  "data": null,
  "errors": [
    {
      "code": 40003,
      "message": "Movie Title cannot be null or empty"
    },
    {
      "code": 40004,
      "message": "Movie Description cannot be null or empty"
    }
  ]
}
```

## HTTP Status Codes

The API uses standard HTTP status codes to indicate the general category of error. The `ApiException` class maps these to the `ApiHttpStatusCode` enumeration for type-safe status code handling.

### Status Code Categories

| Status Code | Category | Usage |
|-------------|----------|-------|
| **400 Bad Request** | Client Error | Validation failures, malformed requests, business rule violations |
| **401 Unauthorized** | Authentication Error | Missing or invalid authentication credentials (JWT tokens) |
| **403 Forbidden** | Authorization Error | Authenticated but lacking required permissions |
| **404 Not Found** | Resource Error | Requested resource does not exist |
| **409 Conflict** | State Conflict | Resource state conflict (e.g., duplicate entries, version mismatches) |
| **500 Internal Server Error** | Server Error | Unexpected errors, unhandled exceptions, infrastructure failures |

### Status Code Selection Guidelines

- Use **400** for validation errors and invalid input data
- Use **401** when authentication fails or tokens are missing/expired (see [Authentication documentation](/features/exception_handling.md))
- Use **404** when a specific resource identifier doesn't match any existing entity
- Use **409** for business logic conflicts (e.g., attempting to create a duplicate resource)
- Use **500** only for truly unexpected errors; prefer specific 4xx codes for predictable failures

## Error Models

### ApiException Class

The `ApiException` class is the primary exception type for API operations. It extends the standard .NET `Exception` class with API-specific properties and multiple constructor overloads for different error scenarios.

```csharp
public class ApiException : Exception
{
    public ApiHttpStatusCode ApiHttpStatusCode { get; set; }
    public IEnumerable<ApiError> ApiErrors { get; set; }
    public new object Data { get; set; }
    
    // Multiple constructor overloads available
}
```

**Key Properties:**
- `ApiHttpStatusCode`: The HTTP status code to return with the response
- `ApiErrors`: Collection of structured error details
- `Data`: Optional additional data to include in the error response

#### Constructor Patterns

**Single Error with Status Code:**
```csharp
throw new ApiException(
    ApiHttpStatusCode.BadRequest,
    null,
    new ApiError 
    { 
        Code = (int)MovieErrorCode.EmptyOrNullMovieTitle,
        Message = "Movie Title cannot be null or empty"
    }
);
```

**Multiple Errors:**
```csharp
var errors = new List<ApiError>
{
    new ApiError { Code = 40003, Message = "Movie Title cannot be null or empty" },
    new ApiError { Code = 40004, Message = "Movie Description cannot be null or empty" }
};

throw new ApiException(
    ApiHttpStatusCode.BadRequest,
    null,
    errors,
    "Validation failed for multiple fields"
);
```

**With Inner Exception:**
```csharp
try
{
    // Database operation
}
catch (SqlException ex)
{
    throw new ApiException(
        ApiHttpStatusCode.InternalServerError,
        null,
        new ApiError { Code = 50001, Message = "Database operation failed" },
        "Failed to retrieve movie data",
        ex
    );
}
```

### HandledException Class

The `HandledException` class provides a specialized exception type for business logic errors with enhanced features for error tracking and nested exception handling.

```csharp
public class HandledException : Exception
{
    public const string DefaultErrorCode = "ERR001";
    public const string ReturnedExceptionType = "HandledException";
    
    public string ErrorCode { get; set; }
    public ExceptionType ExceptionType { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public List<HandledException> InnerExceptions { get; set; }
}
```

**Key Features:**
- **Error Code Tracking**: Each exception carries a unique error code (default: "ERR001")
- **Exception Type Classification**: Uses `ExceptionType` enum for categorization
- **Nested Exception Support**: Can contain multiple inner `HandledException` instances
- **Frontend Integration**: The `ReturnedExceptionType` constant allows frontend code to distinguish handled exceptions from unexpected errors

#### Usage Example

```csharp
throw new HandledException(
    ExceptionType.ValidationError,
    "Movie validation failed",
    HttpStatusCode.BadRequest,
    "MOV001"
);
```

#### Nested Exceptions

```csharp
var innerExceptions = new List<HandledException>
{
    new HandledException(ExceptionType.ValidationError, "Invalid title", HttpStatusCode.BadRequest),
    new HandledException(ExceptionType.ValidationError, "Invalid description", HttpStatusCode.BadRequest)
};

throw new HandledException(
    ExceptionType.ValidationError,
    "Multiple validation errors occurred",
    innerExceptions,
    HttpStatusCode.BadRequest
);
```

#### Exception List Serialization

The `GetExceptionList()` method provides a flattened, serializable representation of the exception hierarchy:

```csharp
public List<ExceptionListItem> GetExceptionList()
{
    var collection = new List<ExceptionListItem>();
    if (InnerExceptions != null && InnerExceptions.Any())
    {
        InnerExceptions.ForEach(ex => 
            collection.Add(new ExceptionListItem() 
            { 
                Name = ex.Message, 
                Type = ex.ExceptionType.ToString() 
            }));
    }
    else
    {
        collection.Add(new ExceptionListItem() 
        { 
            Name = Message, 
            Type = ExceptionType.ToString() 
        });
    }
    return collection;
}
```

## Error Code Conventions

The API uses a structured error code system where codes are organized by domain and error type. Error codes are defined as enumerations with descriptive attributes.

### Error Code Structure

Error codes follow a five-digit pattern: `XYYYZ`
- **X**: HTTP status code category (4 = client error, 5 = server error)
- **YYY**: Domain-specific identifier (e.g., 000 = movies)
- **Z**: Sequential error number within the domain

### Movie Domain Error Codes

```csharp
public enum MovieErrorCode
{
    [Description("Request model cannot be null")]
    NullRequestViewModel = 40001,

    [Description("Movie Id cannot be null or empty")]
    EmptyOrNullMovieId = 40002,

    [Description("Movie Title cannot be null or empty")]
    EmptyOrNullMovieTitle = 40003,

    [Description("Movie Description cannot be null or empty")]
    EmptyOrNullMovieDescription = 40004,

    [Description("Movie Title should be between 2 and 50 characters")]
    TitleNotBetween2and50Characters = 40005,

    [Description("Movie Description should be between 2 and 50 characters")]
    DescriptionNotBetween2and50Characters = 40006,

    [Description("Movie already exists")]
    MovieAlreadyExists = 40007,

    [Description("Id in URL does not match with id in body")]
    IdConflict = 40901,
}
```

### Using Error Codes

```csharp
// Extract description from enum attribute
var errorCode = MovieErrorCode.EmptyOrNullMovieTitle;
var description = errorCode.GetAttributeOfType<DescriptionAttribute>()?.Description;

throw new ApiException(
    ApiHttpStatusCode.BadRequest,
    null,
    new ApiError 
    { 
        Code = (int)errorCode,
        Message = description
    }
);
```

### Best Practices for Error Codes

1. **Consistency**: Always use enum-defined codes rather than magic numbers
2. **Documentation**: Include descriptive `[Description]` attributes on all enum values
3. **Uniqueness**: Ensure error codes are unique across the entire API
4. **Grouping**: Group related errors in the same numeric range (e.g., 40001-40099 for movie validation)
5. **Extensibility**: Leave gaps in numbering to accommodate future error types

## Common Error Scenarios

### Validation Errors (400 Bad Request)

Validation errors occur when client input fails to meet business rules or data constraints. These are typically caught by FluentValidation validators (see [Validation documentation](/features/validation.md)) or manual validation logic.

**Single Field Validation:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40005,
      "message": "Movie Title should be between 2 and 50 characters"
    }
  ]
}
```

**Multiple Field Validation:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40003,
      "message": "Movie Title cannot be null or empty"
    },
    {
      "code": 40004,
      "message": "Movie Description cannot be null or empty"
    }
  ]
}
```

**Implementation Example:**
```csharp
public async Task<MovieResponse> CreateMovie(CreateMovieRequest request)
{
    var errors = new List<ApiError>();
    
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        errors.Add(new ApiError 
        { 
            Code = (int)MovieErrorCode.EmptyOrNullMovieTitle,
            Message = "Movie Title cannot be null or empty"
        });
    }
    
    if (string.IsNullOrWhiteSpace(request.Description))
    {
        errors.Add(new ApiError 
        { 
            Code = (int)MovieErrorCode.EmptyOrNullMovieDescription,
            Message = "Movie Description cannot be null or empty"
        });
    }
    
    if (errors.Any())
    {
        throw new ApiException(ApiHttpStatusCode.BadRequest, null, errors);
    }
    
    // Proceed with creation
}
```

### Authentication Failures (401 Unauthorized)

Authentication errors occur when JWT tokens are missing, expired, or invalid. The API uses Azure AD authentication via the `Azure.Identity` and `Microsoft.IdentityModel` libraries.

**Missing Token:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40101,
      "message": "Authorization token is required"
    }
  ]
}
```

**Expired Token:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40102,
      "message": "Authorization token has expired"
    }
  ]
}
```

**Invalid Token:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40103,
      "message": "Authorization token is invalid"
    }
  ]
}
```

### Resource Not Found (404 Not Found)

404 errors indicate that a requested resource identifier doesn't match any existing entity in the database.

**Example Response:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40401,
      "message": "Movie with ID '12345' was not found"
    }
  ]
}
```

**Implementation Example:**
```csharp
public async Task<MovieResponse> GetMovieById(string movieId)
{
    var movie = await _context.Movies.FindAsync(movieId);
    
    if (movie == null)
    {
        throw new ApiException(
            ApiHttpStatusCode.NotFound,
            null,
            new ApiError 
            { 
                Code = 40401,
                Message = $"Movie with ID '{movieId}' was not found"
            }
        );
    }
    
    return _mapper.Map<MovieResponse>(movie);
}
```

### Conflict Errors (409 Conflict)

Conflict errors occur when the requested operation would violate business rules or data integrity constraints.

**Duplicate Resource:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40007,
      "message": "Movie already exists"
    }
  ]
}
```

**ID Mismatch:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 40901,
      "message": "Id in URL does not match with id in body"
    }
  ]
}
```

**Implementation Example:**
```csharp
public async Task<MovieResponse> UpdateMovie(string urlId, UpdateMovieRequest request)
{
    if (urlId != request.Id)
    {
        throw new ApiException(
            ApiHttpStatusCode.Conflict,
            null,
            new ApiError 
            { 
                Code = (int)MovieErrorCode.IdConflict,
                Message = "Id in URL does not match with id in body"
            }
        );
    }
    
    var existingMovie = await _context.Movies
        .FirstOrDefaultAsync(m => m.Title == request.Title && m.Id != request.Id);
    
    if (existingMovie != null)
    {
        throw new ApiException(
            ApiHttpStatusCode.Conflict,
            null,
            new ApiError 
            { 
                Code = (int)MovieErrorCode.MovieAlreadyExists,
                Message = "Movie already exists"
            }
        );
    }
    
    // Proceed with update
}
```

### Internal Server Errors (500 Internal Server Error)

500 errors represent unexpected failures in the application or infrastructure layer. These should be logged with full stack traces for debugging.

**Database Connection Failure:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 50001,
      "message": "Database connection failed"
    }
  ]
}
```

**Unhandled Exception:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 50000,
      "message": "An unexpected error occurred"
    }
  ]
}
```

**Implementation Example:**
```csharp
public async Task<MovieResponse> GetMovieById(string movieId)
{
    try
    {
        return await _movieRepository.GetByIdAsync(movieId);
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Database error while retrieving movie {MovieId}", movieId);
        
        throw new ApiException(
            ApiHttpStatusCode.InternalServerError,
            null,
            new ApiError 
            { 
                Code = 50001,
                Message = "Database connection failed"
            },
            "Failed to retrieve movie data",
            ex
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error while retrieving movie {MovieId}", movieId);
        
        throw new ApiException(
            ApiHttpStatusCode.InternalServerError,
            null,
            new ApiError 
            { 
                Code = 50000,
                Message = "An unexpected error occurred"
            },
            "Unexpected error in GetMovieById",
            ex
        );
    }
}
```

## Integration with Exception Handling Middleware

The error response models integrate with the global exception handling middleware (see [Exception Handling documentation](/features/exception_handling.md)) to automatically convert exceptions into properly formatted API responses.

### Middleware Flow

1. **Exception Occurs**: An `ApiException` or `HandledException` is thrown in application code
2. **Middleware Intercepts**: The exception handling middleware catches the exception
3. **Response Construction**: Middleware creates an `ApiResponse` object with appropriate errors
4. **Status Code Setting**: HTTP status code is set based on the exception's `StatusCode` property
5. **JSON Serialization**: Response is serialized using `System.Text.Json` and returned to client

### Example Middleware Implementation

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (ApiException apiEx)
    {
        context.Response.StatusCode = (int)apiEx.ApiHttpStatusCode;
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse
        {
            Data = apiEx.Data,
            Errors = apiEx.ApiErrors
        };
        
        await context.Response.WriteAsJsonAsync(response);
    }
    catch (HandledException handledEx)
    {
        context.Response.StatusCode = (int)handledEx.StatusCode;
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse
        {
            Data = null,
            Errors = handledEx.GetExceptionList()
                .Select(e => new ApiError 
                { 
                    Code = int.Parse(handledEx.ErrorCode.Replace("ERR", "")),
                    Message = e.Name 
                })
        };
        
        await context.Response.WriteAsJsonAsync(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception occurred");
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse
        {
            Data = null,
            Errors = new List<ApiError>
            {
                new ApiError 
                { 
                    Code = 50000,
                    Message = "An unexpected error occurred"
                }
            }
        };
        
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

## Best Practices and Recommendations

### For API Developers

1. **Always Use Structured Exceptions**: Prefer `ApiException` over generic exceptions for predictable error handling
2. **Provide Specific Error Codes**: Use domain-specific error codes from enumerations rather than generic codes
3. **Include Actionable Messages**: Error messages should guide the client on how to fix the issue
4. **Log Before Throwing**: Always log exceptions with context before throwing, especially for 500 errors
5. **Validate Early**: Perform validation as early as possible in the request pipeline
6. **Aggregate Validation Errors**: Return all validation errors in a single response rather than failing on the first error

### For API Consumers

1. **Check Status Code First**: Use HTTP status code to determine error category before parsing error details
2. **Handle Multiple Errors**: Always expect the `errors` array to contain multiple items
3. **Use Error Codes for Logic**: Base error handling logic on error codes, not messages (messages may change)
4. **Display User-Friendly Messages**: Transform technical error messages into user-friendly text in the UI
5. **Implement Retry Logic**: Use Polly (already configured in the stack) for transient failures (see [Resilience Patterns](/features/exception_handling.md))

### Error Response Checklist

Before deploying error handling code, verify:

- [ ] Error codes are unique and documented
- [ ] HTTP status codes accurately reflect the error type
- [ ] Error messages are clear and actionable
- [ ] Sensitive information (stack traces, connection strings) is not exposed
- [ ] Errors are logged with appropriate severity levels
- [ ] Multiple validation errors are aggregated into a single response
- [ ] Inner exceptions are properly wrapped and logged
- [ ] API documentation reflects all possible error codes for each endpoint

## Related Documentation

- [Exception Handling](/features/exception_handling.md) - Global exception handling middleware and patterns
- [Movies API Reference](/api_reference/movies_api.md) - Specific error codes and scenarios for movie endpoints
- [Validation](/features/validation.md) - FluentValidation integration and validation error handling