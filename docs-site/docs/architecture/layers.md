# Application Layers

This document provides a comprehensive overview of the application's layered architecture, detailing the responsibilities, implementation patterns, and interactions between each layer. The application follows a clean architecture approach with clear separation of concerns across presentation, business logic, data access, and cross-cutting concerns.

## Presentation Layer (Controllers)

The presentation layer is responsible for handling HTTP requests, orchestrating service calls, and returning appropriate responses to clients. Controllers in this application follow RESTful conventions and leverage ASP.NET Core's MVC framework.

### Controller Responsibilities

Controllers serve as the entry point for all API requests and have the following core responsibilities:

1. **Request Validation**: Validate incoming requests using FluentValidation through the `IBlackSlopeValidator` interface
2. **Service Orchestration**: Coordinate calls to business logic services
3. **Data Transformation**: Map between view models (API contracts) and domain models using AutoMapper
4. **Response Formatting**: Return standardized HTTP responses with appropriate status codes
5. **API Documentation**: Provide Swagger/OpenAPI documentation through XML comments

### Base Controller Implementation

All controllers inherit from `BaseController`, which provides standardized response handling methods:

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
                },
            },
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
- **CORS Support**: Enabled through the `[EnableCors("AllowSpecificOrigin")]` attribute
- **Consistent Response Structure**: All responses follow a standardized format
- **HTTP Status Code Abstraction**: Simplifies status code management across controllers

### Controller Example: MoviesController

The `MoviesController` demonstrates the standard controller pattern used throughout the application:

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
        // get all movies from service
        var movies = await _movieService.GetAllMoviesAsync();

        // prepare response
        var response = _mapper.Map<List<MovieViewModel>>(movies);

        // 200 response
        return HandleSuccessResponse(response);
    }
}
```

### Routing and Versioning

The application uses **explicit route-based versioning** with the following conventions:

- **Version Format**: `api/v{version}/{resource}`
- **Current Version**: v1
- **Example Routes**:
  - `GET api/v1/movies` - Retrieve all movies
  - `GET api/v1/movies/{id}` - Retrieve a specific movie
  - `POST api/v1/movies` - Create a new movie
  - `PUT api/v1/movies/{id}` - Update an existing movie
  - `DELETE api/v1/movies/{id}` - Delete a movie

**Versioning Strategy Benefits:**
- Clear API evolution path
- Backward compatibility support
- Easy to deprecate old versions
- Client-friendly version discovery

### Request/Response Handling

#### Request Processing Flow

1. **Request Reception**: Controller action receives HTTP request
2. **Model Binding**: ASP.NET Core binds request data to view models
3. **Validation**: FluentValidation validates the request through `IBlackSlopeValidator`
4. **Mapping**: AutoMapper transforms view models to domain models
5. **Service Invocation**: Business logic service processes the request
6. **Response Mapping**: Domain models are mapped back to view models
7. **Response Return**: Standardized HTTP response is returned

#### Create Operation Example

```csharp
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };

    // validate request model
    await _blackSlopeValidator.AssertValidAsync(request);

    // map view model to domain model
    var movie = _mapper.Map<MovieDomainModel>(viewModel);

    // create new movie
    var createdMovie = await _movieService.CreateMovieAsync(movie);

    // prepare response
    var response = _mapper.Map<MovieViewModel>(createdMovie);

    // 201 response
    return HandleCreatedResponse(response);
}
```

#### Update Operation Example

```csharp
[HttpPut]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
{
    Contract.Requires(viewModel != null);
    var request = new UpdateMovieRequest { Movie = viewModel, Id = id };

    await _blackSlopeValidator.AssertValidAsync(request);

    // id can be in URL, body, or both
    viewModel.Id = id ?? viewModel.Id;

    // map view model to domain model
    var movie = _mapper.Map<MovieDomainModel>(viewModel);

    // update existing movie
    var updatedMovie = await _movieService.UpdateMovieAsync(movie);

    // prepare response
    var response = _mapper.Map<MovieViewModel>(updatedMovie);

    // 200 response
    return HandleSuccessResponse(response);
}
```

**Important Implementation Details:**
- The `id` parameter is nullable to support flexible client implementations
- ID can be provided in the URL, request body, or both (URL takes precedence)
- Code contracts (`Contract.Requires`) provide runtime validation

### API Documentation

Controllers use XML documentation comments to generate Swagger/OpenAPI documentation:

```csharp
/// <summary>
/// Delete an existing movie
/// </summary>
/// <remarks>
/// Use this operation to delete an existing movie
/// </remarks>
/// <response code="204">Movie successfully delete, no content</response>
/// <response code="400">Bad Request</response>
/// <response code="401">Unauthorized</response>
/// <response code="500">Internal Server Error</response>
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpDelete]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<DeletedMovieResponse>> Delete(int id)
{
    await _movieService.DeleteMovieAsync(id);
    return HandleDeletedResponse();
}
```

**Documentation Best Practices:**
- Use `<summary>` for brief operation descriptions
- Use `<remarks>` for detailed usage instructions
- Document all possible response codes with `<response>` tags
- Apply `[ProducesResponseType]` attributes for Swagger generation

### Authentication and Authorization

Controllers are prepared for authentication but currently have it disabled:

```csharp
// TODO: enable this once authentication middleware has been configured
// [Authorize]
public class MoviesController : BaseController
```

**Planned Authentication:**
- Azure Active Directory integration via `Azure.Identity` (v1.14.2)
- JWT token validation using `System.IdentityModel.Tokens.Jwt` (v7.7.1)
- Role-based authorization support

For more information on controller implementation patterns, see [/features/controllers.md](/features/controllers.md).

## Business Logic Layer (Services)

The business logic layer encapsulates all domain logic, business rules, and orchestration between repositories. Services operate on domain models and are isolated from infrastructure concerns.

### Service Interfaces and Implementations

Services follow the **interface-implementation pattern** to support dependency injection and testability:

```csharp
public interface IMovieService
{
    Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie);
    Task<int> DeleteMovieAsync(int id);
    Task<IEnumerable<MovieDomainModel>> GetAllMoviesAsync();
    Task<MovieDomainModel> GetMovieAsync(int id);
    Task<MovieDomainModel> UpdateMovieAsync(MovieDomainModel movie);
    Task<bool> CheckIfMovieExistsAsync(string title, DateTime? releaseDate);
    Task<dynamic> GetExponentialBackoff();
}
```

### Service Implementation Pattern

The `MovieService` demonstrates the standard service implementation:

```csharp
public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IFakeApiRepository _fakeApiRepository;
    private readonly IMapper _mapper;

    public MovieService(IMovieRepository movieRepository, IFakeApiRepository fakeApiRepository, IMapper mapper)
    {
        _movieRepository = movieRepository;
        _fakeApiRepository = fakeApiRepository;
        _mapper = mapper;
    }

    public async Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie)
    {
        var dto = await _movieRepository.Create(_mapper.Map<MovieDtoModel>(movie));
        return _mapper.Map<MovieDomainModel>(dto);
    }

    public async Task<int> DeleteMovieAsync(int id) => 
        await _movieRepository.DeleteAsync(id);

    public async Task<IEnumerable<MovieDomainModel>> GetAllMoviesAsync()
    {
        var movies = await _movieRepository.GetAllAsync();
        return _mapper.Map<List<MovieDomainModel>>(movies);
    }

    public async Task<MovieDomainModel> GetMovieAsync(int id)
    {
        var movie = await _movieRepository.GetSingleAsync(id);
        return _mapper.Map<MovieDomainModel>(movie);
    }

    public async Task<MovieDomainModel> UpdateMovieAsync(MovieDomainModel movie)
    {
        var dto = await _movieRepository.UpdateAsync(_mapper.Map<MovieDtoModel>(movie));
        return _mapper.Map<MovieDomainModel>(dto);
    }

    public async Task<bool> CheckIfMovieExistsAsync(string title, DateTime? releaseDate) =>
        await _movieRepository.MovieExistsAsync(title, releaseDate);

    public async Task<dynamic> GetExponentialBackoff()
    {
        return await _fakeApiRepository.GetExponentialBackoff();
    }
}
```

### Service Layer Responsibilities

| Responsibility | Description | Example |
|----------------|-------------|---------|
| **Business Logic** | Implement domain-specific rules and workflows | Validate movie uniqueness before creation |
| **Data Transformation** | Map between domain models and DTOs | Convert `MovieDomainModel` to `MovieDtoModel` |
| **Repository Orchestration** | Coordinate multiple repository calls | Aggregate data from multiple sources |
| **Transaction Management** | Manage data consistency across operations | Ensure atomic updates across related entities |
| **Caching Strategy** | Implement caching for performance optimization | Cache frequently accessed movie data |
| **External Service Integration** | Coordinate calls to external APIs | Integrate with third-party movie databases |

### Domain Models

Domain models represent the core business entities and are used exclusively within the service layer:

**Characteristics:**
- **Technology Agnostic**: No database or framework-specific attributes
- **Rich Behavior**: Can contain business logic methods
- **Validation**: Business rule validation (not just data validation)
- **Immutability**: Prefer immutable properties where appropriate

**Example Domain Model Structure:**
```csharp
public class MovieDomainModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
    
    // Business logic methods can be added here
    public bool IsClassic() => ReleaseDate.HasValue && 
                               ReleaseDate.Value.Year < 1980;
}
```

### Business Validation Rules

Business validation occurs at the service layer and differs from input validation:

**Input Validation (Controller Layer):**
- Data type validation
- Required field validation
- Format validation (email, phone, etc.)
- Length constraints

**Business Validation (Service Layer):**
- Business rule enforcement
- Cross-entity validation
- State transition validation
- Domain-specific constraints

**Example Business Validation:**
```csharp
public async Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie)
{
    // Business rule: Check for duplicate movies
    var exists = await CheckIfMovieExistsAsync(movie.Title, movie.ReleaseDate);
    if (exists)
    {
        throw new BusinessRuleException("A movie with this title and release date already exists");
    }
    
    // Business rule: Validate release date is not in the future
    if (movie.ReleaseDate.HasValue && movie.ReleaseDate.Value > DateTime.UtcNow)
    {
        throw new BusinessRuleException("Release date cannot be in the future");
    }
    
    var dto = await _movieRepository.Create(_mapper.Map<MovieDtoModel>(movie));
    return _mapper.Map<MovieDomainModel>(dto);
}
```

### Service Layer Patterns

#### Repository Aggregation Pattern

Services can aggregate data from multiple repositories:

```csharp
public async Task<MovieDetailsDomainModel> GetMovieDetailsAsync(int id)
{
    // Aggregate data from multiple repositories
    var movie = await _movieRepository.GetSingleAsync(id);
    var reviews = await _reviewRepository.GetByMovieIdAsync(id);
    var ratings = await _ratingRepository.GetByMovieIdAsync(id);
    
    return new MovieDetailsDomainModel
    {
        Movie = _mapper.Map<MovieDomainModel>(movie),
        Reviews = _mapper.Map<List<ReviewDomainModel>>(reviews),
        AverageRating = ratings.Average(r => r.Score)
    };
}
```

#### External Service Integration

The service layer handles integration with external APIs using Polly for resilience:

```csharp
public async Task<dynamic> GetExponentialBackoff()
{
    // Delegates to repository that implements Polly retry policies
    return await _fakeApiRepository.GetExponentialBackoff();
}
```

**Resilience Features:**
- Exponential backoff retry policies
- Circuit breaker patterns
- Timeout policies
- Fallback strategies

For more information on service implementation, see [/features/services.md](/features/services.md).

## Data Access Layer (Repositories)

The data access layer abstracts all database operations and provides a clean interface for the service layer. This layer uses Entity Framework Core with the repository pattern to manage data persistence.

### Repository Pattern Implementation

Repositories provide a collection-like interface for accessing domain objects:

```csharp
public interface IMovieRepository
{
    Task<MovieDtoModel> Create(MovieDtoModel movie);
    Task<int> DeleteAsync(int id);
    Task<IEnumerable<MovieDtoModel>> GetAllAsync();
    Task<MovieDtoModel> GetSingleAsync(int id);
    Task<bool> MovieExistsAsync(string title, DateTime? releaseDate);
    Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie);
}
```

### Repository Implementation

The `MovieRepository` demonstrates the standard repository implementation pattern:

```csharp
public class MovieRepository : IMovieRepository
{
    private readonly MovieContext _context;

    public MovieRepository(MovieContext movieContext)
    {
        _context = movieContext;
    }

    public async Task<MovieDtoModel> Create(MovieDtoModel movie)
    {
        await _context.Movies.AddAsync(movie);
        await _context.SaveChangesAsync();

        return movie;
    }

    public async Task<int> DeleteAsync(int id)
    {
        var dto = _context.Movies.FirstOrDefault(x => x.Id == id);
        _context.Movies.Remove(dto);
        await _context.SaveChangesAsync();

        return id;
    }

    public async Task<IEnumerable<MovieDtoModel>> GetAllAsync() =>
        await _context.Movies.ToListAsync();

    public async Task<MovieDtoModel> GetSingleAsync(int id) =>
        await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<bool> MovieExistsAsync(string title, DateTime? releaseDate) =>
        await _context.Movies.AnyAsync(m => m.Title == title && m.ReleaseDate == releaseDate);

    public async Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie)
    {
        Contract.Requires(movie != null);
        var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
        dto.Title = movie.Title;
        dto.Description = movie.Description;
        await _context.SaveChangesAsync();

        return movie;
    }
}
```

### Entity Framework Context

The `MovieContext` manages the database connection and entity configurations:

**Key Features:**
- **DbContext Lifecycle**: Scoped lifetime per HTTP request
- **Connection String Management**: Configured via `appsettings.json` or Azure Key Vault
- **Migration Support**: Database schema versioning through EF Core migrations
- **Change Tracking**: Automatic entity state management

**Context Configuration:**
```csharp
public class MovieContext : DbContext
{
    public MovieContext(DbContextOptions<MovieContext> options) : base(options)
    {
    }

    public DbSet<MovieDtoModel> Movies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entity configurations
        modelBuilder.Entity<MovieDtoModel>(entity =>
        {
            entity.ToTable("Movies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });
    }
}
```

### DTO Models

Data Transfer Objects (DTOs) represent the database schema and are used exclusively in the repository layer:

**DTO Characteristics:**
- **Database Mapping**: Direct mapping to database tables
- **EF Core Attributes**: Contains Entity Framework annotations
- **No Business Logic**: Pure data containers
- **Serialization Support**: Can be serialized for caching or messaging

**Example DTO Model:**
```csharp
[Table("Movies")]
public class MovieDtoModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
```

### Repository Layer Responsibilities

| Responsibility | Description | Implementation |
|----------------|-------------|----------------|
| **CRUD Operations** | Create, Read, Update, Delete operations | Standard EF Core methods |
| **Query Optimization** | Efficient data retrieval | Use `AsNoTracking()` for read-only queries |
| **Data Mapping** | Map between DTOs and database | EF Core handles automatically |
| **Connection Management** | Database connection lifecycle | Managed by DbContext |
| **Transaction Handling** | Ensure data consistency | Use `SaveChangesAsync()` for atomic operations |
| **Error Handling** | Database-specific error handling | Catch and translate SQL exceptions |

### Query Patterns

#### Simple Queries

```csharp
public async Task<MovieDtoModel> GetSingleAsync(int id) =>
    await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);
```

#### Existence Checks

```csharp
public async Task<bool> MovieExistsAsync(string title, DateTime? releaseDate) =>
    await _context.Movies.AnyAsync(m => m.Title == title && m.ReleaseDate == releaseDate);
```

#### Optimized Read Queries

For read-only operations, use `AsNoTracking()` to improve performance:

```csharp
public async Task<IEnumerable<MovieDtoModel>> GetAllAsync() =>
    await _context.Movies.AsNoTracking().ToListAsync();
```

### Update Pattern Considerations

The current update implementation retrieves the entity before updating:

```csharp
public async Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie)
{
    Contract.Requires(movie != null);
    var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
    dto.Title = movie.Title;
    dto.Description = movie.Description;
    await _context.SaveChangesAsync();

    return movie;
}
```

**Important Notes:**
- This pattern ensures the entity is tracked by EF Core
- Only specified properties are updated (partial updates)
- Null reference exception possible if entity doesn't exist
- Consider adding null checks and throwing appropriate exceptions

**Improved Update Pattern:**
```csharp
public async Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie)
{
    Contract.Requires(movie != null);
    
    var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
    if (dto == null)
    {
        throw new EntityNotFoundException($"Movie with ID {movie.Id} not found");
    }
    
    dto.Title = movie.Title;
    dto.Description = movie.Description;
    dto.ModifiedDate = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();

    return dto;
}
```

### Database Configuration

**Connection String Management:**
```json
{
  "ConnectionStrings": {
    "MovieDatabase": "Server=localhost;Database=BlackSlope;Trusted_Connection=True;"
  }
}
```

**Startup Configuration:**
```csharp
services.AddDbContext<MovieContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("MovieDatabase"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    )
);
```

### Health Checks

The application includes database health checks using `AspNetCore.HealthChecks.SqlServer` (v5.0.3):

```csharp
services.AddHealthChecks()
    .AddSqlServer(
        connectionString: configuration.GetConnectionString("MovieDatabase"),
        healthQuery: "SELECT 1;",
        name: "sql",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db", "sql", "sqlserver" }
    )
    .AddDbContextCheck<MovieContext>(
        name: "efcore",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db", "efcore" }
    );
```

For more information on repository implementation, see [/features/repositories.md](/features/repositories.md).

## Cross-Cutting Concerns

Cross-cutting concerns are aspects of the application that affect multiple layers and are implemented using middleware, filters, and shared services.

### Middleware Components

Middleware components process HTTP requests and responses in a pipeline pattern. The application uses custom middleware for exception handling and can be extended for other concerns.

#### Exception Handling Middleware

The `ExceptionHandlingMiddleware` provides centralized exception handling across the entire application:

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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

    private static ApiResponse PrepareResponse(object data, IEnumerable<ApiError> apiErrors)
    {
        var response = new ApiResponse
        {
            Data = data,
            Errors = apiErrors,
        };

        return response;
    }

    private static ApiError PrepareApiError(int code, string message)
    {
        return new ApiError
        {
            Code = code,
            Message = message,
        };
    }

    private static string Serialize(ApiResponse apiResponse)
    {
        var result = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        });

        return result;
    }
}
```

**Exception Handling Features:**

| Feature | Description | Benefit |
|---------|-------------|---------|
| **Centralized Handling** | All exceptions caught in one place | Consistent error responses |
| **Custom Exception Support** | Handles `ApiException` with custom status codes | Fine-grained error control |
| **Structured Logging** | Logs exceptions with full context | Improved debugging and monitoring |
| **Standardized Response** | Returns consistent `ApiResponse` format | Client-friendly error handling |
| **JSON Serialization** | Uses `System.Text.Json` for performance | Fast response generation |

**Exception Response Format:**
```json
{
  "data": null,
  "errors": [
    {
      "code": 500,
      "message": "Internal Server Error"
    }
  ]
}
```

**Custom Exception Example:**
```csharp
[HttpGet]
[Route("SampleError")]
public object SampleError()
{
    throw new HandledException(
        ExceptionType.Security, 
        "This is an example security issue.", 
        System.Net.HttpStatusCode.RequestEntityTooLarge
    );
}
```

#### Middleware Registration

Middleware must be registered in the correct order in `Startup.cs`:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Exception handling should be first to catch all errors
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    // Other middleware
    app.UseRouting();
    app.UseCors("AllowSpecificOrigin");
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapHealthChecks("/health");
    });
}
```

### Validation Framework

The application uses **FluentValidation** (v10.3.6) for comprehensive input validation with dependency injection support.

#### Validation Architecture

```csharp
public interface IBlackSlopeValidator
{
    Task AssertValidAsync<T>(T request);
}
```

**Validation Flow:**
1. Controller receives request
2. Request wrapped in validation request object
3. `IBlackSlopeValidator.AssertValidAsync()` invoked
4. FluentValidation rules executed
5. Validation failures throw exceptions
6. Exception middleware catches and formats errors

#### Validation Example

**Request Validator:**
```csharp
public class CreateMovieRequestValidator : AbstractValidator<CreateMovieRequest>
{
    public CreateMovieRequestValidator()
    {
        RuleFor(x => x.Movie.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Movie.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.Movie.ReleaseDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.Movie.ReleaseDate.HasValue)
            .WithMessage("Release date cannot be in the future");
    }
}
```

**Controller Usage:**
```csharp
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };

    // Validation occurs here - throws exception on failure
    await _blackSlopeValidator.AssertValidAsync(request);

    // Continue with business logic
    var movie = _mapper.Map<MovieDomainModel>(viewModel);
    var createdMovie = await _movieService.CreateMovieAsync(movie);
    var response = _mapper.Map<MovieViewModel>(createdMovie);

    return HandleCreatedResponse(response);
}
```

#### Validation Features

**Built-in Validators:**
- `NotEmpty()` - Ensures value is not null or empty
- `MaximumLength()` - Validates string length
- `MinimumLength()` - Validates minimum string length
- `EmailAddress()` - Validates email format
- `GreaterThan()` / `LessThan()` - Numeric comparisons
- `Must()` - Custom validation logic

**Conditional Validation:**
```csharp
RuleFor(x => x.Movie.ReleaseDate)
    .LessThanOrEqualTo(DateTime.UtcNow)
    .When(x => x.Movie.ReleaseDate.HasValue)
    .WithMessage("Release date cannot be in the future");
```

**Async Validation:**
```csharp
RuleFor(x => x.Movie.Title)
    .MustAsync(async (title, cancellation) => 
    {
        return !await _movieService.CheckIfMovieExistsAsync(title, null);
    })
    .WithMessage("A movie with this title already exists");
```

### AutoMapper Configuration

**AutoMapper** (v10.1.1) handles object-to-object mapping between layers:

**Mapping Profile Example:**
```csharp
public class MovieMappingProfile : Profile
{
    public MovieMappingProfile()
    {
        // ViewModel to Domain Model
        CreateMap<CreateMovieViewModel, MovieDomainModel>();
        CreateMap<MovieViewModel, MovieDomainModel>();

        // Domain Model to ViewModel
        CreateMap<MovieDomainModel, MovieViewModel>();

        // Domain Model to DTO
        CreateMap<MovieDomainModel, MovieDtoModel>();

        // DTO to Domain Model
        CreateMap<MovieDtoModel, MovieDomainModel>();
    }
}
```

**Registration:**
```csharp
services.AddAutoMapper(typeof(Startup));
```

### Caching Strategy

The application includes **Microsoft.Extensions.Caching.Memory** (v6.0.2) for in-memory caching:

**Cache Configuration:**
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size
    options.CompactionPercentage = 0.25; // Compact when 75% full
});
```

**Cache Usage Example:**
```csharp
public class CachedMovieService : IMovieService
{
    private readonly IMovieService _innerService;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    public async Task<MovieDomainModel> GetMovieAsync(int id)
    {
        var cacheKey = $"movie_{id}";
        
        if (_cache.TryGetValue(cacheKey, out MovieDomainModel cachedMovie))
        {
            return cachedMovie;
        }

        var movie = await _innerService.GetMovieAsync(id);
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_cacheDuration)
            .SetSize(1);
            
        _cache.Set(cacheKey, movie, cacheOptions);

        return movie;
    }
}
```

### Resilience and HTTP Client Management

The application uses **Polly** (v7.2.2) for resilience patterns:

**Polly Configuration:**
```csharp
services.AddHttpClient("FakeApi")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                // Log retry attempt
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}
```

**Resilience Patterns:**
- **Retry with Exponential Backoff**: Automatically retries failed requests with increasing delays
- **Circuit Breaker**: Prevents cascading failures by stopping requests to failing services
- **Timeout**: Ensures requests don't hang indefinitely
- **Fallback**: Provides alternative responses when services are unavailable

### Logging and Diagnostics

The application uses **Microsoft.Extensions.Logging** with multiple providers:

**Logging Configuration:**
```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.AddApplicationInsights(); // For Azure Application Insights
});
```

**Structured Logging Example:**
```csharp
_logger.LogInformation(
    "Movie created: {MovieId}, Title: {Title}", 
    movie.Id, 
    movie.Title
);

_logger.LogError(
    exception, 
    "Failed to create movie: {Title}", 
    movie.Title
);
```

### CORS Configuration

Cross-Origin Resource Sharing (CORS) is configured to allow specific origins:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("https://example.com", "https://app.example.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});
```

### Code Quality and Analysis

The application uses **StyleCop.Analyzers** (v1.1.118) and **Microsoft.CodeAnalysis.NetAnalyzers** (v6.0.0) for code quality:

**StyleCop Configuration (.editorconfig):**
```ini
[*.cs]
# SA1633: File must have header
dotnet_diagnostic.SA1633.severity = none

# SA1200: Using directives must be placed correctly
dotnet_diagnostic.SA1200.severity = warning
```

**Benefits:**
- Consistent code style across the team
- Early detection of potential issues
- Improved code maintainability
- Automated code review

For more information on the overall architecture, see [/architecture/overview.md](/architecture/overview.md).

---

## Summary

This application implements a clean, layered architecture with clear separation of concerns:

- **Presentation Layer**: Handles HTTP concerns, validation, and response formatting
- **Business Logic Layer**: Implements domain logic and orchestrates data access
- **Data Access Layer**: Abstracts database operations using Entity Framework Core
- **Cross-Cutting Concerns**: Provides shared functionality through middleware and services

The architecture supports maintainability, testability, and scalability while leveraging modern .NET 6.0 features and industry-standard libraries.