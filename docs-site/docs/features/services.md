# Services Layer

The Services layer implements the business logic tier of the application, acting as an intermediary between the API controllers and the data access layer (repositories). This layer encapsulates domain-specific business rules, orchestrates data transformations, and provides a clean abstraction for the application's core functionality.

## Service Pattern

The Services layer follows the **Service Pattern** architectural approach, which separates business logic from presentation and data access concerns. This implementation adheres to several key principles:

### Interface-Based Design

All services are defined through interfaces, promoting loose coupling and enabling dependency injection. This approach provides several benefits:

- **Testability**: Services can be easily mocked or stubbed in unit tests
- **Flexibility**: Implementations can be swapped without affecting consumers
- **Maintainability**: Clear contracts define service capabilities
- **Dependency Inversion**: High-level modules depend on abstractions, not concrete implementations

```csharp
public interface IMovieService
{
    Task<IEnumerable<MovieDomainModel>> GetAllMoviesAsync();
    Task<MovieDomainModel> GetMovieAsync(int id);
    Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie);
    Task<MovieDomainModel> UpdateMovieAsync(MovieDomainModel movie);
    Task<int> DeleteMovieAsync(int id);
    Task<bool> CheckIfMovieExistsAsync(string title, DateTime? releaseDate);
    Task<dynamic> GetExponentialBackoff();
}
```

### Asynchronous Operations

All service methods are implemented as asynchronous operations using the `Task<T>` pattern. This design choice:

- Improves application scalability by freeing up threads during I/O operations
- Aligns with modern ASP.NET Core best practices
- Enables efficient handling of database and external API calls
- Prevents thread pool starvation under high load

### Business Logic Encapsulation

Services encapsulate business rules and validation logic, ensuring that:

- Domain rules are enforced consistently across the application
- Controllers remain thin and focused on HTTP concerns
- Business logic can be reused across multiple endpoints or applications
- Complex operations are abstracted behind simple, intuitive interfaces

For more information on how the Services layer fits into the overall architecture, see [Architecture Layers](/architecture/layers.md).

## Domain Models

Domain models represent the business entities within the Services layer. Unlike Data Transfer Objects (DTOs) used in the repository layer or view models used in controllers, domain models are optimized for business logic operations.

### MovieDomainModel

The `MovieDomainModel` represents a movie entity at the service layer:

```csharp
public class MovieDomainModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
}
```

**Design Considerations:**

- **Nullable ReleaseDate**: Accommodates movies in pre-production or with unknown release dates
- **Simple Properties**: Focuses on core business attributes without infrastructure concerns
- **Framework Agnostic**: No dependencies on Entity Framework or other data access frameworks
- **Validation Ready**: Properties are designed to work with FluentValidation rules

**Separation of Concerns:**

The domain model differs from repository DTOs in several ways:

| Aspect | Domain Model | Repository DTO |
|--------|--------------|----------------|
| Purpose | Business logic operations | Data persistence |
| Location | Services layer | Repository layer |
| Dependencies | None (POCO) | May include EF Core attributes |
| Validation | Business rules | Data constraints |
| Mapping | To/from DTOs | To/from database entities |

This separation allows the business layer to evolve independently from the data access layer, supporting scenarios like:

- Changing database schemas without affecting business logic
- Adding computed properties or business-specific fields
- Implementing different persistence strategies (e.g., switching from SQL Server to NoSQL)

## Movie Service

The `MovieService` class implements the `IMovieService` interface, providing concrete implementations for all movie-related business operations.

### Constructor and Dependencies

```csharp
public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IFakeApiRepository _fakeApiRepository;
    private readonly IMapper _mapper;

    public MovieService(
        IMovieRepository movieRepository, 
        IFakeApiRepository fakeApiRepository, 
        IMapper mapper)
    {
        _movieRepository = movieRepository;
        _fakeApiRepository = fakeApiRepository;
        _mapper = mapper;
    }
}
```

**Dependency Injection:**

The service receives three dependencies through constructor injection:

1. **IMovieRepository**: Provides data access operations for movie entities (see [Repositories](/features/repositories.md))
2. **IFakeApiRepository**: Demonstrates integration with external APIs and resilience patterns
3. **IMapper**: AutoMapper instance for transforming between domain models and DTOs (see [AutoMapper](/features/automapper.md))

### CRUD Operations Implementation

#### Retrieving All Movies

```csharp
public async Task<IEnumerable<MovieDomainModel>> GetAllMoviesAsync()
{
    var movies = await _movieRepository.GetAllAsync();
    return _mapper.Map<List<MovieDomainModel>>(movies);
}
```

**Implementation Details:**

- Delegates data retrieval to the repository layer
- Uses AutoMapper to transform `MovieDtoModel` collection to `MovieDomainModel` collection
- Returns `IEnumerable<T>` to provide flexibility in how consumers iterate results
- Asynchronous execution prevents blocking during database operations

**Performance Considerations:**

- For large datasets, consider implementing pagination at the repository level
- The mapping operation occurs in-memory after data retrieval
- Consider using projection queries for read-only scenarios to reduce memory overhead

#### Retrieving a Single Movie

```csharp
public async Task<MovieDomainModel> GetMovieAsync(int id)
{
    var movie = await _movieRepository.GetSingleAsync(id);
    return _mapper.Map<MovieDomainModel>(movie);
}
```

**Error Handling:**

- If the repository returns `null` for a non-existent ID, the mapper will return `null`
- Controllers should handle null returns appropriately (typically returning 404 Not Found)
- Consider implementing custom exceptions for business rule violations

#### Creating a Movie

```csharp
public async Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie)
{
    var dto = await _movieRepository.Create(_mapper.Map<MovieDtoModel>(movie));
    return _mapper.Map<MovieDomainModel>(dto);
}
```

**Workflow:**

1. Maps incoming domain model to DTO for repository layer
2. Delegates creation to repository (which handles database insertion)
3. Maps returned DTO back to domain model
4. Returns the created entity with database-generated values (e.g., ID)

**Business Validation:**

While not shown in this implementation, this method is the ideal location for:

- Validating business rules (e.g., duplicate title checks)
- Enforcing data integrity constraints
- Triggering domain events or side effects
- Logging business-level operations

#### Updating a Movie

```csharp
public async Task<MovieDomainModel> UpdateMovieAsync(MovieDomainModel movie)
{
    var dto = await _movieRepository.UpdateAsync(_mapper.Map<MovieDtoModel>(movie));
    return _mapper.Map<MovieDomainModel>(dto);
}
```

**Update Semantics:**

- Assumes the incoming model contains the complete entity state
- The repository layer handles concurrency checks and database updates
- Returns the updated entity to reflect any database-side changes (e.g., timestamps)

**Potential Enhancements:**

```csharp
// Example: Add existence check before update
public async Task<MovieDomainModel> UpdateMovieAsync(MovieDomainModel movie)
{
    var exists = await _movieRepository.GetSingleAsync(movie.Id);
    if (exists == null)
    {
        throw new EntityNotFoundException($"Movie with ID {movie.Id} not found");
    }
    
    var dto = await _movieRepository.UpdateAsync(_mapper.Map<MovieDtoModel>(movie));
    return _mapper.Map<MovieDomainModel>(dto);
}
```

#### Deleting a Movie

```csharp
public async Task<int> DeleteMovieAsync(int id) => 
    await _movieRepository.DeleteAsync(id);
```

**Return Value:**

- Returns the number of affected rows (typically 0 or 1)
- A return value of 0 indicates the entity was not found
- Controllers can use this to determine appropriate HTTP status codes

**Cascade Considerations:**

- Ensure database foreign key constraints are properly configured
- Consider soft deletes for audit trail requirements
- Implement business rules for preventing deletion of referenced entities

### Business Validation

#### Duplicate Movie Check

```csharp
public async Task<bool> CheckIfMovieExistsAsync(string title, DateTime? releaseDate) =>
    await _movieRepository.MovieExistsAsync(title, releaseDate);
```

**Use Cases:**

- Pre-validation before creating new movies
- Preventing duplicate entries in the database
- Supporting unique constraint enforcement at the business layer

**Implementation Notes:**

- Delegates to repository for efficient database-level checking
- Combines title and release date for uniqueness determination
- Nullable release date allows checking movies without confirmed release dates

**Integration with Validation:**

This method can be integrated with FluentValidation for declarative validation rules:

```csharp
// Example FluentValidation rule
RuleFor(x => x.Title)
    .MustAsync(async (model, title, cancellation) => 
    {
        return !await _movieService.CheckIfMovieExistsAsync(title, model.ReleaseDate);
    })
    .WithMessage("A movie with this title and release date already exists");
```

### External API Integration

#### Exponential Backoff Demonstration

```csharp
public async Task<dynamic> GetExponentialBackoff()
{
    return await _fakeApiRepository.GetExponentialBackoff();
}
```

**Purpose:**

This method demonstrates integration with external APIs and resilience patterns using Polly. The implementation showcases:

- **Retry Logic**: Automatic retries with exponential backoff for transient failures
- **Circuit Breaker**: Prevents cascading failures when external services are unavailable
- **Timeout Policies**: Ensures requests don't hang indefinitely

**Polly Integration:**

The application uses Polly (version 7.2.2) with HTTP client extensions for resilience:

```csharp
// Example Polly configuration (typically in Startup.cs)
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
```

**Production Considerations:**

- Replace `dynamic` return type with strongly-typed models
- Implement proper error handling and logging
- Consider caching strategies for frequently accessed external data
- Monitor external API health using health checks

## Service Registration

The Services layer uses extension methods to encapsulate dependency injection configuration, following ASP.NET Core conventions.

### Extension Method Pattern

```csharp
public static class MovieServiceServiceCollectionExtensions
{
    public static IServiceCollection AddMovieService(this IServiceCollection services)
    {
        services.TryAddTransient<IMovieService, MovieService>();
        return services;
    }
}
```

**Design Benefits:**

- **Encapsulation**: Service registration logic is contained within the feature area
- **Discoverability**: Extension methods appear in IntelliSense when working with `IServiceCollection`
- **Composability**: Multiple service registrations can be chained together
- **Namespace Convention**: Placed in `Microsoft.Extensions.DependencyInjection` namespace for consistency

### Service Lifetime Management

The implementation uses `TryAddTransient` for service registration:

```csharp
services.TryAddTransient<IMovieService, MovieService>();
```

**Lifetime Choice: Transient**

| Lifetime | Behavior | Use Case |
|----------|----------|----------|
| **Transient** | New instance per request | Lightweight services with no state |
| Scoped | One instance per HTTP request | Services that maintain request-specific state |
| Singleton | Single instance for application lifetime | Expensive-to-create, stateless services |

**Rationale for Transient:**

- `MovieService` is stateless and lightweight
- Dependencies (repositories, mapper) are also transient or scoped
- No shared state between requests
- Minimal overhead for instance creation

**TryAdd vs. Add:**

The `TryAdd` prefix prevents duplicate registrations:

```csharp
// TryAdd only registers if IMovieService is not already registered
services.TryAddTransient<IMovieService, MovieService>();
services.TryAddTransient<IMovieService, AlternativeMovieService>(); // Ignored

// Add always registers, potentially causing issues
services.AddTransient<IMovieService, MovieService>();
services.AddTransient<IMovieService, AlternativeMovieService>(); // Both registered
```

### Registration in Startup

The extension method is typically called in `Startup.cs` or `Program.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Other service registrations...
    
    services.AddMovieService();
    
    // Additional configuration...
}
```

**Dependency Chain:**

The service registration assumes that dependencies are already registered:

```csharp
// Typical registration order
services.AddDbContext<ApplicationDbContext>(); // EF Core context
services.AddRepositories(); // Repository layer
services.AddAutoMapper(typeof(MovieProfile)); // AutoMapper profiles
services.AddMovieService(); // Service layer
```

For more details on dependency injection configuration, see [Dependency Injection](/architecture/dependency_injection.md).

## AutoMapper Integration

The Services layer uses AutoMapper to transform between domain models and repository DTOs, eliminating boilerplate mapping code.

### Mapping Profile Configuration

```csharp
public class MovieProfile : Profile
{
    public MovieProfile()
    {
        CreateMap<MovieDomainModel, MovieDtoModel>().ReverseMap();
    }
}
```

**Profile Registration:**

AutoMapper profiles are automatically discovered and registered:

```csharp
// In Startup.cs
services.AddAutoMapper(typeof(MovieProfile).Assembly);
```

**Bidirectional Mapping:**

The `ReverseMap()` method creates mappings in both directions:

- `MovieDomainModel` → `MovieDtoModel` (for repository operations)
- `MovieDtoModel` → `MovieDomainModel` (for returning results)

**Convention-Based Mapping:**

AutoMapper uses convention-based mapping when property names match:

```csharp
// These properties map automatically
MovieDomainModel.Id → MovieDtoModel.Id
MovieDomainModel.Title → MovieDtoModel.Title
MovieDomainModel.Description → MovieDtoModel.Description
MovieDomainModel.ReleaseDate → MovieDtoModel.ReleaseDate
```

### Custom Mapping Scenarios

For complex transformations, AutoMapper supports custom mapping logic:

```csharp
public class MovieProfile : Profile
{
    public MovieProfile()
    {
        CreateMap<MovieDomainModel, MovieDtoModel>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Trim()))
            .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => 
                src.ReleaseDate.HasValue ? src.ReleaseDate.Value.Date : (DateTime?)null))
            .ReverseMap();
    }
}
```

**Advanced Mapping Features:**

- **Conditional Mapping**: Map properties based on runtime conditions
- **Value Resolvers**: Implement complex transformation logic
- **Type Converters**: Handle conversions between incompatible types
- **Null Substitution**: Provide default values for null properties

For comprehensive AutoMapper documentation, see [AutoMapper Configuration](/features/automapper.md).

## Best Practices and Considerations

### Error Handling Strategy

Services should implement consistent error handling:

```csharp
public async Task<MovieDomainModel> GetMovieAsync(int id)
{
    if (id <= 0)
    {
        throw new ArgumentException("Movie ID must be greater than zero", nameof(id));
    }
    
    var movie = await _movieRepository.GetSingleAsync(id);
    
    if (movie == null)
    {
        throw new EntityNotFoundException($"Movie with ID {id} not found");
    }
    
    return _mapper.Map<MovieDomainModel>(movie);
}
```

**Exception Hierarchy:**

- Use custom exceptions for business rule violations
- Let infrastructure exceptions (e.g., `DbUpdateException`) bubble up
- Implement global exception handling middleware for consistent API responses

### Transaction Management

For operations spanning multiple repositories, implement transaction boundaries:

```csharp
public async Task<MovieDomainModel> CreateMovieWithReviewsAsync(
    MovieDomainModel movie, 
    IEnumerable<ReviewDomainModel> reviews)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();
    
    try
    {
        var createdMovie = await CreateMovieAsync(movie);
        
        foreach (var review in reviews)
        {
            review.MovieId = createdMovie.Id;
            await _reviewService.CreateReviewAsync(review);
        }
        
        await transaction.CommitAsync();
        return createdMovie;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Caching Strategies

Leverage `Microsoft.Extensions.Caching.Memory` for frequently accessed data:

```csharp
private readonly IMemoryCache _cache;

public async Task<IEnumerable<MovieDomainModel>> GetAllMoviesAsync()
{
    const string cacheKey = "all_movies";
    
    if (!_cache.TryGetValue(cacheKey, out IEnumerable<MovieDomainModel> movies))
    {
        var dtos = await _movieRepository.GetAllAsync();
        movies = _mapper.Map<List<MovieDomainModel>>(dtos);
        
        _cache.Set(cacheKey, movies, TimeSpan.FromMinutes(5));
    }
    
    return movies;
}
```

**Cache Invalidation:**

Ensure cache is invalidated on data modifications:

```csharp
public async Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie)
{
    var result = await CreateMovieAsync(movie);
    _cache.Remove("all_movies"); // Invalidate cache
    return result;
}
```

### Testing Considerations

The service layer is highly testable due to its dependency on interfaces:

```csharp
[Fact]
public async Task GetMovieAsync_ReturnsMovie_WhenMovieExists()
{
    // Arrange
    var mockRepository = new Mock<IMovieRepository>();
    var mockMapper = new Mock<IMapper>();
    
    var dto = new MovieDtoModel { Id = 1, Title = "Test Movie" };
    var domain = new MovieDomainModel { Id = 1, Title = "Test Movie" };
    
    mockRepository.Setup(r => r.GetSingleAsync(1)).ReturnsAsync(dto);
    mockMapper.Setup(m => m.Map<MovieDomainModel>(dto)).Returns(domain);
    
    var service = new MovieService(mockRepository.Object, null, mockMapper.Object);
    
    // Act
    var result = await service.GetMovieAsync(1);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Movie", result.Title);
}
```

### Performance Optimization

**Projection Queries:**

For read-only operations, consider using projection to avoid unnecessary mapping:

```csharp
// Instead of retrieving full entities and mapping
var movies = await _movieRepository.GetAllAsync();
return _mapper.Map<List<MovieDomainModel>>(movies);

// Use projection at the database level
var movies = await _dbContext.Movies
    .Select(m => new MovieDomainModel 
    { 
        Id = m.Id, 
        Title = m.Title,
        Description = m.Description,
        ReleaseDate = m.ReleaseDate
    })
    .ToListAsync();
```

**Batch Operations:**

Implement batch operations for bulk data modifications:

```csharp
public async Task<int> DeleteMoviesAsync(IEnumerable<int> ids)
{
    return await _movieRepository.DeleteRangeAsync(ids);
}
```

### Security Considerations

**Input Validation:**

Always validate input at the service layer:

```csharp
public async Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie)
{
    if (string.IsNullOrWhiteSpace(movie.Title))
    {
        throw new ValidationException("Movie title is required");
    }
    
    if (movie.Title.Length > 200)
    {
        throw new ValidationException("Movie title cannot exceed 200 characters");
    }
    
    // Proceed with creation...
}
```

**Authorization:**

While authentication is handled at the API layer, services may need to enforce business-level authorization:

```csharp
public async Task<MovieDomainModel> UpdateMovieAsync(
    MovieDomainModel movie, 
    string userId)
{
    var existing = await _movieRepository.GetSingleAsync(movie.Id);
    
    if (existing.CreatedBy != userId && !IsAdmin(userId))
    {
        throw new UnauthorizedAccessException(
            "You can only update movies you created");
    }
    
    // Proceed with update...
}
```

---

This Services layer implementation provides a robust foundation for business logic encapsulation, following established patterns and best practices for .NET 6.0 applications. The architecture supports maintainability, testability, and scalability while integrating seamlessly with the repository layer and API controllers.