# Validation Framework

The BlackSlope API implements a comprehensive validation framework built on FluentValidation, providing a robust, composable approach to request validation with standardized error handling and reporting.

## Validation Architecture

The validation framework is structured around three core components that work together to provide flexible, maintainable validation logic:

### Core Components

**IBlackSlopeValidator Interface**

The `IBlackSlopeValidator` interface defines the contract for validation execution throughout the application:

```csharp
public interface IBlackSlopeValidator
{
    void AssertValid<T>(T instance);
    Task AssertValidAsync<T>(T instance);
    void AssertValid<T>(T instance, string[] ruleSetsToExecute);
}
```

This interface provides three validation methods:
- **AssertValid\<T\>**: Synchronous validation of an instance against all registered rules
- **AssertValidAsync\<T\>**: Asynchronous validation for rules that require async operations (e.g., database checks)
- **AssertValid\<T\> with rulesets**: Selective validation using specific named rulesets

**BlackSlopeValidator Implementation**

The `BlackSlopeValidator` class implements the validation interface and orchestrates the validation process:

```csharp
public class BlackSlopeValidator : IBlackSlopeValidator
{
    private readonly IValidatorAbstractFactory _validatorAbstractFactory;

    public BlackSlopeValidator(IValidatorAbstractFactory validatorAbstractFactory)
    {
        _validatorAbstractFactory = validatorAbstractFactory;
    }

    public async Task AssertValidAsync<T>(T instance)
    {
        var validator = _validatorAbstractFactory.Resolve<T>();
        var validationResult = await validator.ValidateAsync(instance);

        HandleValidationFailure(validationResult, instance);
    }

    private static void HandleValidationFailure(ValidationResult result, object instance)
    {
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(CreateApiError).ToList();
            throw new ApiException(ApiHttpStatusCode.BadRequest, instance, errors);
        }
    }

    private static ApiError CreateApiError(ValidationFailure validationFailure)
    {
        int errorCode;
        string message = null;
        if (validationFailure.CustomState is Enum validationFailureEnum)
        {
            errorCode = (int)validationFailure.CustomState;
            message = validationFailureEnum.GetDescription();
        }
        else
        {
            errorCode = (int)ApiHttpStatusCode.BadRequest;
        }

        return new ApiError
        {
            Code = errorCode,
            Message = string.IsNullOrEmpty(message)
                ? validationFailure.ErrorMessage
                : message,
        };
    }
}
```

**Key Design Decisions:**

- **Factory Pattern**: Uses `IValidatorAbstractFactory` to resolve validators dynamically, enabling dependency injection and loose coupling
- **Exception-Based Validation**: Throws `ApiException` on validation failure, allowing centralized error handling (see [Exception Handling](/features/exception_handling.md))
- **Enum-Based Error Codes**: Leverages enums with descriptions for strongly-typed, maintainable error codes
- **Standardized Error Format**: Converts FluentValidation failures to `ApiError` objects for consistent API responses

**Validator Abstract Factory**

The factory pattern enables runtime resolution of validators based on the type being validated. This allows:
- Registration of multiple validators for the same type
- Composite validator patterns
- Testability through mock validators
- Separation of validation logic from business logic

### FluentValidation Integration

The framework leverages FluentValidation 10.3.6 with dependency injection extensions, providing:

- **Declarative Validation Rules**: Express validation logic as fluent chains
- **Built-in Validators**: Leverage pre-built validators (NotEmpty, InclusiveBetween, etc.)
- **Async Validation Support**: Execute async operations during validation (database checks, external API calls)
- **Dependent Rules**: Chain validation rules that only execute if parent rules pass
- **Custom State Management**: Attach custom error codes and metadata to validation failures

## Composite Validation Pattern

The `CompositeValidator\<T\>` enables combining multiple validators for the same type, supporting separation of concerns and reusable validation logic:

```csharp
public class CompositeValidator<T> : AbstractValidator<T>
{
    public CompositeValidator(IEnumerable<IValidator<T>> validators)
    {
        if (validators is null)
        {
            throw new ArgumentNullException(nameof(validators));
        }

        foreach (var validator in validators)
        {
            Include(validator);
        }
    }
}
```

**Usage Scenarios:**

1. **Shared Validation Logic**: Create base validators for common properties (e.g., `BaseEntityValidator`) and compose them with specific validators
2. **Context-Specific Validation**: Apply different validation rules based on operation context (create vs. update)
3. **Cross-Cutting Concerns**: Separate business rule validation from technical validation (format, length, etc.)

**Example Registration:**

```csharp
services.AddTransient<IValidator<CreateMovieViewModel>, CreateMovieViewModelValidatorCollection>();
services.AddTransient<IValidator<CreateMovieViewModel>, CreateMovieBusinessRulesValidator>();
services.AddTransient<IValidator<CreateMovieViewModel>>(sp => 
    new CompositeValidator<CreateMovieViewModel>(
        sp.GetServices<IValidator<CreateMovieViewModel>>()));
```

## Movie Validators

The movie validation implementation demonstrates best practices for building domain-specific validators with complex business rules.

### Create Movie Validation Rules

The `CreateMovieViewModelValidatorCollection` implements comprehensive validation for movie creation requests:

```csharp
public class CreateMovieViewModelValidatorCollection : AbstractValidator<CreateMovieViewModel>
{
    public CreateMovieViewModelValidatorCollection(IMovieService movieService)
    {
        // Async business rule validation - checks for duplicate movies
        RuleFor(x => x).MustAsync(async (x, cancellationtoken) =>
                !await movieService.CheckIfMovieExistsAsync(x.Title, x.ReleaseDate))
            .WithState(x => MovieErrorCode.MovieAlreadyExists);

        // Title validation with dependent rules
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithState(x => MovieErrorCode.EmptyOrNullMovieTitle)
            .DependentRules(() =>
                RuleFor(x => x.Title.Length)
                    .InclusiveBetween(2, 50)
                    .WithState(x => MovieErrorCode.TitleNotBetween2and50Characters));

        // Description validation with dependent rules
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithState(x => MovieErrorCode.EmptyOrNullMovieDescription)
            .DependentRules(() =>
                RuleFor(x => x.Description.Length)
                    .InclusiveBetween(2, 50)
                    .WithState(x => MovieErrorCode.DescriptionNotBetween2and50Characters));
    }
}
```

**Validation Rule Breakdown:**

| Rule | Type | Purpose | Error Code |
|------|------|---------|------------|
| Movie Uniqueness | Async Business Rule | Prevents duplicate movies with same title and release date | `MovieAlreadyExists` |
| Title Not Empty | Required Field | Ensures title is provided | `EmptyOrNullMovieTitle` |
| Title Length | Format Validation | Enforces 2-50 character constraint | `TitleNotBetween2and50Characters` |
| Description Not Empty | Required Field | Ensures description is provided | `EmptyOrNullMovieDescription` |
| Description Length | Format Validation | Enforces 2-50 character constraint | `DescriptionNotBetween2and50Characters` |

**Key Implementation Details:**

- **Dependent Rules**: Length validation only executes if the field is not empty, preventing redundant error messages
- **Custom Error Codes**: Uses `MovieErrorCode` enum with `.WithState()` for strongly-typed error identification
- **Async Validation**: Leverages `MustAsync` for database-dependent business rules
- **Service Injection**: Accepts `IMovieService` via constructor for business rule validation

### Update Movie Validation Rules

Update validators typically extend or modify create validators with additional constraints:

```csharp
public class UpdateMovieViewModelValidatorCollection : AbstractValidator<UpdateMovieViewModel>
{
    public UpdateMovieViewModelValidatorCollection(IMovieService movieService)
    {
        // ID validation - required for updates
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithState(x => MovieErrorCode.InvalidMovieId);

        // Movie existence check
        RuleFor(x => x.Id).MustAsync(async (id, cancellationtoken) =>
                await movieService.MovieExistsAsync(id))
            .WithState(x => MovieErrorCode.MovieNotFound);

        // Include create validation rules
        Include(new CreateMovieViewModelValidatorCollection(movieService));
    }
}
```

**Update-Specific Considerations:**

- **Entity Existence**: Verify the entity exists before validating other properties
- **Optimistic Concurrency**: Consider adding version/timestamp validation for concurrent updates
- **Partial Updates**: For PATCH operations, create separate validators that only validate provided fields

### Request Validation Collections

Organizing validators into collections provides clear separation and maintainability:

```
src/BlackSlope.Api/Operations/Movies/Validators/
├── CreateMovieViewModelValidatorCollection.cs
├── UpdateMovieViewModelValidatorCollection.cs
├── DeleteMovieRequestValidatorCollection.cs
└── SearchMovieRequestValidatorCollection.cs
```

**Naming Conventions:**

- **{Operation}{Entity}ViewModelValidatorCollection**: For request body validation
- **{Operation}{Entity}RequestValidatorCollection**: For query parameter validation
- **{Entity}BusinessRulesValidator**: For cross-cutting business rules

## Validation Execution

The framework provides multiple execution points for validation, ensuring comprehensive request validation before business logic execution.

### Automatic Model State Validation

The `ModelStateValidationFilter` provides automatic validation of ASP.NET Core model binding:

```csharp
public class ModelStateValidationFilter : ActionFilterAttribute
{
    private const string ErrorName = "RequestModel";
    private const string ErrorCode = "MSTATE001";
    private const string ErrorText = "ModelState did not pass validation.";
    private const string ErrorDescription = "Unable to create request model. Most likely its not being constructed from [FromUri] / [FromBody] or not enough data supplied to create the object.";

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
            var execptions = new List<HandledException>();
            foreach (var state in modelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    execptions.Add(new HandledException(
                        ExceptionType.Validation, 
                        error.ErrorMessage, 
                        System.Net.HttpStatusCode.BadRequest, 
                        ErrorCode));
                }
            }

            // Throw the exception and lets the service exception filter capture it.
            throw new HandledException(ExceptionType.Validation, ErrorText, execptions);
        }

        base.OnActionExecuting(context);
    }
}
```

**Filter Responsibilities:**

1. **Null Request Detection**: Identifies when request models fail to bind (missing [FromBody]/[FromUri] attributes)
2. **Model State Aggregation**: Collects all model binding errors
3. **Error Standardization**: Converts ASP.NET Core errors to `HandledException` format
4. **Early Termination**: Prevents controller action execution on validation failure

**Registration:**

```csharp
// Global registration
services.AddControllers(options =>
{
    options.Filters.Add<ModelStateValidationFilter>();
});

// Or per-controller
[ServiceFilter(typeof(ModelStateValidationFilter))]
public class MoviesController : ControllerBase
{
    // ...
}
```

### Custom Validation Filters

Beyond model state validation, implement custom filters for cross-cutting validation concerns:

**Authorization Validation Filter:**

```csharp
public class ResourceAuthorizationFilter : IAsyncActionFilter
{
    private readonly IBlackSlopeValidator _validator;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, 
        ActionExecutionDelegate next)
    {
        var resourceId = context.ActionArguments["id"];
        var authRequest = new ResourceAuthorizationRequest 
        { 
            ResourceId = resourceId,
            UserId = context.HttpContext.User.GetUserId()
        };

        await _validator.AssertValidAsync(authRequest);
        await next();
    }
}
```

**Rate Limiting Validation Filter:**

```csharp
public class RateLimitValidationFilter : IAsyncActionFilter
{
    private readonly IBlackSlopeValidator _validator;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, 
        ActionExecutionDelegate next)
    {
        var rateLimitRequest = new RateLimitRequest
        {
            UserId = context.HttpContext.User.GetUserId(),
            Endpoint = context.HttpContext.Request.Path
        };

        await _validator.AssertValidAsync(rateLimitRequest);
        await next();
    }
}
```

### Error Response Formatting

Validation errors are automatically formatted into standardized API responses through the exception handling middleware (see [Error Responses](/api_reference/error_responses.md)):

**Validation Error Response Structure:**

```json
{
  "statusCode": 400,
  "message": "Validation failed",
  "errors": [
    {
      "code": 1001,
      "message": "Movie title must be between 2 and 50 characters"
    },
    {
      "code": 1002,
      "message": "Movie description cannot be empty"
    }
  ],
  "timestamp": "2024-01-15T10:30:00Z",
  "path": "/api/movies"
}
```

**Error Code Ranges:**

| Range | Category | Example |
|-------|----------|---------|
| 1000-1999 | Movie Validation | `MovieAlreadyExists = 1001` |
| 2000-2999 | User Validation | `InvalidEmailFormat = 2001` |
| 3000-3999 | Authorization | `InsufficientPermissions = 3001` |
| 9000-9999 | System Validation | `ModelStateError = 9001` |

## Creating Custom Validators

Building custom validators follows established patterns to ensure consistency and maintainability across the application.

### Implementing IBlackSlopeValidator

For complex validation scenarios requiring custom logic, implement the `IBlackSlopeValidator` interface:

```csharp
public class CustomBusinessRuleValidator : IBlackSlopeValidator
{
    private readonly IMovieService _movieService;
    private readonly IUserService _userService;

    public CustomBusinessRuleValidator(
        IMovieService movieService,
        IUserService userService)
    {
        _movieService = movieService;
        _userService = userService;
    }

    public async Task AssertValidAsync<T>(T instance)
    {
        if (instance is CreateMovieViewModel createRequest)
        {
            await ValidateMovieCreationRules(createRequest);
        }
        else
        {
            throw new NotSupportedException($"Validation not supported for type {typeof(T).Name}");
        }
    }

    private async Task ValidateMovieCreationRules(CreateMovieViewModel request)
    {
        var errors = new List<ApiError>();

        // Complex business rule: User can only create 5 movies per day
        var userMovieCount = await _movieService.GetUserMovieCountTodayAsync(request.UserId);
        if (userMovieCount >= 5)
        {
            errors.Add(new ApiError
            {
                Code = (int)MovieErrorCode.DailyMovieLimitExceeded,
                Message = "You have reached the daily limit of 5 movie creations"
            });
        }

        // Complex business rule: Premium users can create movies with longer descriptions
        var user = await _userService.GetUserAsync(request.UserId);
        if (!user.IsPremium && request.Description.Length > 200)
        {
            errors.Add(new ApiError
            {
                Code = (int)MovieErrorCode.DescriptionTooLongForFreeUser,
                Message = "Free users are limited to 200 character descriptions"
            });
        }

        if (errors.Any())
        {
            throw new ApiException(ApiHttpStatusCode.BadRequest, request, errors);
        }
    }

    public void AssertValid<T>(T instance)
    {
        throw new NotImplementedException("Use async validation for business rules");
    }

    public void AssertValid<T>(T instance, string[] ruleSetsToExecute)
    {
        throw new NotImplementedException("Use async validation for business rules");
    }
}
```

### Validation Composition

Compose multiple validators to build complex validation logic from reusable components:

**Base Entity Validator:**

```csharp
public class BaseEntityValidator<T> : AbstractValidator<T> where T : BaseEntity
{
    public BaseEntityValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThanOrEqualTo(0)
            .WithState(x => CommonErrorCode.InvalidEntityId);

        RuleFor(x => x.CreatedDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithState(x => CommonErrorCode.InvalidCreatedDate);
    }
}
```

**Composed Movie Validator:**

```csharp
public class MovieValidator : CompositeValidator<MovieViewModel>
{
    public MovieValidator(
        IEnumerable<IValidator<MovieViewModel>> validators,
        BaseEntityValidator<MovieViewModel> baseValidator) 
        : base(validators)
    {
        Include(baseValidator);
    }
}
```

**Registration with Composition:**

```csharp
services.AddTransient<BaseEntityValidator<MovieViewModel>>();
services.AddTransient<IValidator<MovieViewModel>, MovieFormatValidator>();
services.AddTransient<IValidator<MovieViewModel>, MovieBusinessRulesValidator>();
services.AddTransient<IValidator<MovieViewModel>, MovieValidator>();
```

### Reusable Validation Rules

Create extension methods for commonly used validation patterns:

```csharp
public static class CustomValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> IsValidEmail<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .EmailAddress()
            .WithState(x => CommonErrorCode.InvalidEmailFormat);
    }

    public static IRuleBuilderOptions<T, string> IsValidPhoneNumber<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithState(x => CommonErrorCode.InvalidPhoneFormat);
    }

    public static IRuleBuilderOptions<T, DateTime> IsNotInFuture<T>(
        this IRuleBuilder<T, DateTime> ruleBuilder)
    {
        return ruleBuilder
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithState(x => CommonErrorCode.DateCannotBeInFuture);
    }

    public static IRuleBuilderOptions<T, TProperty> WithErrorCode<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> ruleBuilder,
        Enum errorCode)
    {
        return ruleBuilder.WithState(x => errorCode);
    }
}
```

**Usage:**

```csharp
public class UserValidator : AbstractValidator<UserViewModel>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .IsValidEmail();

        RuleFor(x => x.PhoneNumber)
            .IsValidPhoneNumber();

        RuleFor(x => x.BirthDate)
            .IsNotInFuture();
    }
}
```

### Best Practices and Gotchas

**Performance Considerations:**

- **Async Validation Overhead**: Async validators incur additional overhead; use synchronous validation for simple rules
- **Database Queries**: Cache frequently accessed validation data (e.g., lookup tables) using `IMemoryCache`
- **Validation Order**: Place fast, synchronous validations before slow, async validations using `DependentRules()`

**Common Pitfalls:**

1. **Circular Dependencies**: Avoid validators that depend on services that depend on validators
2. **Null Reference Exceptions**: Always check for null before accessing nested properties
3. **Async Void**: Never use `async void` in validators; always return `Task`
4. **State Mutation**: Validators should never modify the instance being validated

**Testing Validators:**

```csharp
[Fact]
public async Task CreateMovieValidator_WithDuplicateTitle_ShouldFail()
{
    // Arrange
    var movieService = new Mock<IMovieService>();
    movieService
        .Setup(x => x.CheckIfMovieExistsAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
        .ReturnsAsync(true);

    var validator = new CreateMovieViewModelValidatorCollection(movieService.Object);
    var viewModel = new CreateMovieViewModel
    {
        Title = "Existing Movie",
        ReleaseDate = DateTime.UtcNow
    };

    // Act
    var result = await validator.ValidateAsync(viewModel);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => 
        (MovieErrorCode)e.CustomState == MovieErrorCode.MovieAlreadyExists);
}
```

**Integration with Controllers:**

Controllers should leverage the validation framework through the `IBlackSlopeValidator` interface (see [Controllers](/features/controllers.md)):

```csharp
[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IBlackSlopeValidator _validator;
    private readonly IMovieService _movieService;

    public MoviesController(
        IBlackSlopeValidator validator,
        IMovieService movieService)
    {
        _validator = validator;
        _movieService = movieService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMovie([FromBody] CreateMovieViewModel request)
    {
        // Validation is automatically handled by ModelStateValidationFilter
        // Additional custom validation can be performed here
        await _validator.AssertValidAsync(request);

        var movie = await _movieService.CreateMovieAsync(request);
        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
    }
}
```

This comprehensive validation framework ensures data integrity, provides clear error messages, and maintains separation of concerns between validation logic and business logic throughout the BlackSlope API.