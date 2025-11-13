# Exception Handling

## Exception Handling Architecture

The application implements a comprehensive exception handling architecture that provides centralized error management, standardized error responses, and seamless integration with the middleware pipeline. This architecture consists of three primary components:

### Centralized Exception Middleware

The `ExceptionHandlingMiddleware` serves as the global exception handler for the entire application. It intercepts all unhandled exceptions thrown during request processing and transforms them into standardized API responses. This middleware is registered in the ASP.NET Core pipeline and executes as one of the earliest middleware components to ensure comprehensive exception coverage.

**Key characteristics:**
- **Global scope**: Catches all exceptions that bubble up through the middleware pipeline
- **Logging integration**: Automatically logs all exceptions with full context using `ILogger<ExceptionHandlingMiddleware>`
- **Response standardization**: Converts exceptions into consistent JSON responses following the `ApiResponse` pattern
- **HTTP status code mapping**: Translates exception types into appropriate HTTP status codes

### Custom Exception Types

The framework provides specialized exception types that enable developers to throw exceptions with rich metadata that can be properly handled and presented to API consumers:

| Exception Type | Purpose | HTTP Status Code |
|---------------|---------|------------------|
| `HandledException` | Single validation or business logic error | Configurable (default: 400 Bad Request) |
| `HandledExceptionCollection` | Multiple related errors that should be returned together | Configurable (default: 400 Bad Request) |
| `ApiException` | API-specific errors with structured error details | Configurable via `ApiHttpStatusCode` |

### Error Response Standardization

All exceptions are transformed into standardized response models that provide consistent structure for client applications:

- **HandledResponseModel**: Used for `HandledException` and `HandledExceptionCollection` types
- **ApiResponse**: Used for `ApiException` and general exceptions
- Both models include error codes, messages, and HTTP status codes for comprehensive error reporting

## HandledException Framework

The `HandledException` framework provides a structured approach to handling expected errors in business logic and validation scenarios. This framework enables developers to throw exceptions that carry rich metadata and can be gracefully handled by the middleware.

### Creating Handled Exceptions

The `HandledException` class offers multiple constructors to accommodate different error scenarios:

```csharp
// Basic handled exception with type and message
throw new HandledException(
    ExceptionType.Validation, 
    "The email address is already registered",
    HttpStatusCode.BadRequest,
    "ERR001"
);

// Handled exception with inner exception
throw new HandledException(
    ExceptionType.BusinessLogic,
    "Unable to process payment",
    innerHandledException,
    HttpStatusCode.BadRequest
);

// Handled exception with multiple inner exceptions
throw new HandledException(
    ExceptionType.Validation,
    "Multiple validation errors occurred",
    listOfHandledExceptions,
    HttpStatusCode.BadRequest
);
```

**Constructor Parameters:**

- **type** (`ExceptionType`): Categorizes the exception for client-side handling (e.g., Validation, BusinessLogic, General)
- **message** (`string`): Human-readable error message displayed to end users
- **status** (`HttpStatusCode`): HTTP status code returned in the response (default: 400 Bad Request)
- **errorCode** (`string`): Application-specific error code for programmatic error handling (default: "ERR001", only available in first constructor)

**Note:** The second constructor (with single inner exception) sets ErrorCode to DefaultErrorCode ("ERR001") but does not allow overriding it. The third constructor (with multiple inner exceptions) does not set an ErrorCode property at all.

### Exception Collections

When multiple related errors occur simultaneously (such as form validation with multiple field errors), use `HandledExceptionCollection` to group them:

```csharp
var exceptions = new HandledExceptionCollection(HttpStatusCode.BadRequest)
{
    InnerExceptions = new List<HandledException>
    {
        new HandledException(ExceptionType.Validation, "Email is required"),
        new HandledException(ExceptionType.Validation, "Password must be at least 8 characters"),
        new HandledException(ExceptionType.Validation, "Phone number format is invalid")
    }
};

throw exceptions;
```

The collection automatically aggregates all inner exception messages into a single composite message while preserving individual error details in the response.

**Note:** The `InnerExceptions` property must be initialized before use. Failing to initialize this property will result in a `NullReferenceException` when the collection's `Message` property is accessed.

### Error Codes and Types

#### Exception Types

The `ExceptionType` enumeration categorizes errors for client-side handling:

```csharp
public enum ExceptionType
{
    General,        // Generic errors without specific categorization
    Validation,     // Input validation failures
    BusinessLogic,  // Business rule violations
    // Additional types as defined in your application
}
```

Client applications can use the exception type to determine appropriate UI behavior (e.g., displaying validation errors inline vs. showing a general error dialog).

#### Error Codes

Error codes provide programmatic identifiers for specific error conditions:

- **Default code**: `"ERR001"` (defined in `HandledException.DefaultErrorCode`)
- **Custom codes**: Override the default by providing the `errorCode` parameter
- **Purpose**: Enable client applications to implement error-specific handling logic
- **Frontend detection**: The constant `ReturnedExceptionType = "HandledException"` is used by the frontend to detect if an exception is handled by the notification service or should be redirected to an error page

**Best practices for error codes:**
- Use a consistent naming convention (e.g., "ERR" prefix followed by numeric identifier)
- Document error codes in your API reference documentation
- Reserve code ranges for different modules or error categories
- Avoid reusing codes for different error conditions

### Exception List Retrieval

The `GetExceptionList()` method provides a flattened view of all exceptions for API consumption:

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

This method handles both single exceptions and exceptions with inner exception collections, ensuring consistent output structure.

## Exception Middleware

The `ExceptionHandlingMiddleware` implements the global exception handling strategy for the application. It integrates with the ASP.NET Core middleware pipeline to provide comprehensive error handling.

### Global Exception Catching

The middleware wraps the entire request pipeline in a try-catch block:

```csharp
public async Task Invoke(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (Exception ex)
    {
        await HandleExceptionAsync(context, ex);
    }
}
```

**Execution flow:**
1. Middleware invokes the next component in the pipeline
2. If any exception occurs downstream, it's caught by this middleware
3. The exception is processed and transformed into a standardized response
4. The response is written directly to the HTTP context

### Logging Integration

All exceptions are automatically logged with full context before being transformed into responses:

```csharp
_logger.LogError(exception, response);
```

**Logged information includes:**
- Exception type and message
- Stack trace (automatically included by `LogError`)
- Serialized response body
- HTTP context information (via the logger's scope)

**Integration with correlation IDs:**
The logging system integrates with the correlation ID middleware (see [Correlation ID documentation](/features/correlation_id.md)) to ensure all log entries for a request can be traced together.

### Response Formatting

The middleware implements type-specific exception handling using a pattern-matching approach:

```csharp
private Task HandleExceptionAsync(HttpContext context, Exception exception)
{
    var statusCode = ApiHttpStatusCode.InternalServerError;
    string response;
    
    if (exception is ApiException)
    {
        var apiException = exception as ApiException;
        statusCode = apiException.ApiHttpStatusCode;

        var apiErrors = new List<ApiError>();
        foreach (var error in apiException.ApiErrors)
        {
            apiErrors.Add(PrepareApiError(error.Code, error.Message));
        }

        var apiResponse = PrepareResponse(apiException.Data, apiErrors);
        response = Serialize(apiResponse);
    }
    else
    {
        var apiErrors = new List<ApiError>
        {
            PrepareApiError((int)statusCode, statusCode.GetDescription()),
        };
        var apiResponse = PrepareResponse(null, apiErrors);
        response = Serialize(apiResponse);
    }

    _logger.LogError(exception, response);

    context.Response.ContentType = "application/json";
    context.Response.StatusCode = (int)statusCode;

    return context.Response.WriteAsync(response);
}
```

**Response formatting features:**
- **Content-Type**: Always set to `application/json`
- **Status Code**: Derived from exception type or defaults to 500
- **Serialization**: Uses `System.Text.Json` with camelCase naming and indented formatting
- **Error structure**: Consistent across all exception types

**JSON serialization options:**
```csharp
var result = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
});
```

## Error Response Models

The application uses two primary response models for error handling, each serving different exception handling patterns.

### HandledResponseModel Structure

The `HandledResponseModel` is used specifically for `HandledException` and `HandledExceptionCollection` types:

```csharp
public class HandledResponseModel
{
    public HandledResponseModel(HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        StatusCode = (int)status;
        Exceptions = new List<HandledResponseError>();
    }

    public int StatusCode { get; set; }
    public string Descriptor { get; set; }
    public List<HandledResponseError> Exceptions { get; set; }
}
```

**Properties:**
- **StatusCode** (`int`): Numeric HTTP status code (e.g., 400, 404, 500)
- **Descriptor** (`string`): Human-readable status description (e.g., "Bad Request", "Internal Server Error")
- **Exceptions** (`List<HandledResponseError>`): Collection of detailed error information

**Example JSON response:**
```json
{
  "statusCode": 400,
  "descriptor": "Bad Request",
  "exceptions": [
    {
      "code": "ERR001",
      "type": "Validation",
      "message": "Email address is required"
    },
    {
      "code": "ERR001",
      "type": "Validation",
      "message": "Password must be at least 8 characters"
    }
  ]
}
```

### HandledResult Pattern

The `HandledResult<T>` class implements a generic pattern for processing exceptions and generating appropriate responses:

```csharp
public class HandledResult<T> where T : Exception
{
    private const string BadRequestDescriptor = "Bad Request";
    private const string ServerErrorDescriptor = "Internal Server Error";

    public HandledResult(T value)
    {
        Exception = value;
        StatusCode = HttpStatusCode.BadRequest;
    }

    public HandledResult(HttpStatusCode status, T value)
    {
        Exception = value;
        StatusCode = status;
    }

    public T Exception { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}
```

**Usage pattern:**
```csharp
// Create a handled result from an exception
var result = new HandledResult<HandledException>(exception);

// Process the exception and generate response
var response = result.HandleException();
```

### Exception Processing with TypeSwitch

The `HandleException()` method uses a type-switching pattern to handle different exception types appropriately:

```csharp
public HandledResponseModel HandleException()
{
    var response = new HandledResponseModel();

    TypeSwitch.Eval(
        Exception,

        TypeSwitch.Case<HandledException>(ex =>
        {
            response = new HandledResponseModel()
            {
                StatusCode = (int)ex.StatusCode,
                Descriptor = BadRequestDescriptor,
                Exceptions = ToExResponse(ex),
            };
        }),

        TypeSwitch.Case<HandledExceptionCollection>((ex) =>
        {
            response = new HandledResponseModel()
            {
                StatusCode = (int)ex.StatusCode,
                Descriptor = BadRequestDescriptor,
                Exceptions = ToExResponse(ex),
            };
        }),

        TypeSwitch.Case<Exception>((ex) =>
        {
            response = new HandledResponseModel()
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Descriptor = BadRequestDescriptor,
                Exceptions = ToExResponse(ex),
            };
        }),

        TypeSwitch.Default(() => { }));

    return response;
}
```

**Type-specific handling:**
1. **HandledException**: Uses the exception's configured status code and extracts error details
2. **HandledExceptionCollection**: Processes all inner exceptions and aggregates them
3. **Generic Exception**: Defaults to 500 Internal Server Error and wraps in a generic handled exception
4. **Default case**: No-op (should never occur given the type constraints)

### Error Detail Transformation

The private `ToExResponse` methods transform exceptions into `HandledResponseError` objects:

```csharp
private List<HandledResponseError> ToExResponse(HandledException ex)
{
    return ToExResponse(new List<HandledException>() { ex });
}

private List<HandledResponseError> ToExResponse(Exception ex)
{
    return ToExResponse(new List<HandledException>() 
    { 
        new HandledException(ExceptionType.General, ex.Message) 
    });
}

private List<HandledResponseError> ToExResponse(List<HandledException> exs)
{
    var collection = new List<HandledResponseError>();
    exs.ForEach(ex =>
    {
        collection.Add(new HandledResponseError()
        {
            Code = ex.ErrorCode,
            Type = ex.ExceptionType.ToString(),
            Message = ex.Message,
        });
    });
    return collection;
}
```

**Transformation logic:**
- Single exceptions are wrapped in a list for consistent processing
- Generic exceptions are converted to `HandledException` with type `General`
- All exceptions are mapped to `HandledResponseError` objects with code, type, and message

### ApiException Details

The `ApiException` type provides an alternative exception handling pattern with its own response structure:

```csharp
// Example ApiException usage (structure inferred from middleware)
throw new ApiException
{
    ApiHttpStatusCode = ApiHttpStatusCode.BadRequest,
    Data = customDataObject,
    ApiErrors = new List<ApiError>
    {
        new ApiError { Code = 1001, Message = "Invalid request parameter" }
    }
};
```

**ApiResponse structure:**
```csharp
public class ApiResponse
{
    public object Data { get; set; }
    public IEnumerable<ApiError> Errors { get; set; }
}

public class ApiError
{
    public int Code { get; set; }
    public string Message { get; set; }
}
```

**Example JSON response:**
```json
{
  "data": {
    "requestId": "abc123",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "errors": [
    {
      "code": 1001,
      "message": "Invalid request parameter"
    }
  ]
}
```

## Integration with Middleware Pipeline

The exception handling middleware should be registered early in the middleware pipeline to ensure comprehensive coverage. See the [Middleware Pipeline documentation](/architecture/middleware_pipeline.md) for detailed configuration instructions.

**Recommended registration order:**
1. Correlation ID middleware (for request tracing)
2. Exception handling middleware (this component)
3. Authentication/Authorization middleware
4. Application-specific middleware
5. MVC/API controllers

**Registration example:**
```csharp
public void Configure(IApplicationBuilder app)
{
    // Register exception handling early in the pipeline
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    // Other middleware registrations...
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
}
```

## Best Practices and Gotchas

### Best Practices

1. **Use HandledException for expected errors**: Business logic violations, validation failures, and other expected error conditions should use `HandledException` rather than generic exceptions.

2. **Provide meaningful error messages**: Error messages should be clear, actionable, and safe to display to end users (avoid exposing sensitive system information).

3. **Use appropriate HTTP status codes**: 
   - 400 Bad Request: Client input validation errors
   - 404 Not Found: Resource not found
   - 409 Conflict: Business rule violations
   - 500 Internal Server Error: Unexpected system errors

4. **Group related validation errors**: Use `HandledExceptionCollection` when multiple validation errors occur on the same request.

5. **Implement custom error codes**: Define application-specific error codes for programmatic error handling by client applications.

### Common Gotchas

1. **Exception swallowing**: Ensure exceptions are not caught and ignored in lower layers without proper handling or re-throwing.

2. **Sensitive information exposure**: Never include stack traces, connection strings, or other sensitive information in error messages returned to clients.

3. **Status code consistency**: Ensure the HTTP status code in the exception matches the actual error condition (e.g., don't use 400 for server-side errors).

4. **Logging before throwing**: Avoid logging exceptions before throwing them, as the middleware will log them automatically. This prevents duplicate log entries.

5. **Inner exception handling**: When wrapping exceptions, ensure inner exception details are preserved for debugging while sanitizing messages for client responses.

### Error Response Reference

For complete details on error response formats and status codes, see the [Error Responses API Reference](/api_reference/error_responses.md).