# Entity Framework Core

Entity Framework Core (EF Core) serves as the Object-Relational Mapping (ORM) framework for this application, providing a robust data access layer for interacting with Microsoft SQL Server. The implementation follows best practices for separation of concerns, performance optimization, and maintainable code architecture.

## EF Core Setup

### DbContext Configuration

The application uses **Entity Framework Core 6.0.1** with the SQL Server provider. The `MovieContext` class serves as the primary database context, inheriting from `DbContext` and managing all database operations for movie-related entities.

```csharp
public class MovieContext : DbContext
{
    private readonly MovieRepositoryConfiguration _config;

    public MovieContext(DbContextOptions<MovieContext> options)
        : base(options)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        _config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
            .Get<MovieRepositoryConfiguration>();
    }

    public virtual DbSet<MovieDtoModel> Movies { get; set; }
}
```

**Key Implementation Details:**

- **Constructor Injection**: The context accepts `DbContextOptions<MovieContext>` through dependency injection, enabling flexible configuration in the startup pipeline
- **Configuration Loading**: The constructor loads configuration from `appsettings.json` using the assembly name as the configuration section key
- **Virtual DbSets**: The `Movies` DbSet is marked as `virtual` to support mocking in unit tests and enable lazy loading if configured

### Connection String Management

Connection strings are managed through the ASP.NET Core configuration system, leveraging multiple configuration sources:

1. **appsettings.json**: Base configuration file for default connection strings
2. **User Secrets**: Development-time secrets management (User Secrets ID: `eeaaec3a-f784-4d04-8b1d-8fe6d9637231`)
3. **Environment Variables**: Production configuration through Azure App Service or container orchestration
4. **Azure Key Vault**: Secure secret storage via `Azure.Identity` (version 1.14.2)

**Configuration Structure:**

```json
{
  "BlackSlope.Api": {
    "ConnectionString": "Server=localhost;Database=MovieDb;Trusted_Connection=True;",
    "CommandTimeout": 30
  }
}
```

The configuration is bound to the `MovieRepositoryConfiguration` class, providing strongly-typed access to database settings throughout the repository layer.

**Best Practices:**

- Never commit connection strings with credentials to source control
- Use User Secrets for local development
- Leverage Azure Managed Identity in production environments
- Configure appropriate command timeouts based on query complexity

### Provider Configuration (SQL Server)

The SQL Server provider is configured through the `Microsoft.EntityFrameworkCore.SqlServer` package (version 6.0.1), which provides:

- **Native SQL Server Features**: Support for SQL Server-specific data types, functions, and optimizations
- **Connection Resilience**: Built-in retry logic for transient failures
- **Performance Optimizations**: Query compilation caching and efficient SQL generation

**Typical Startup Configuration:**

```csharp
services.AddDbContext<MovieContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("MovieDatabase"),
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlServerOptions.CommandTimeout(60);
            sqlServerOptions.MigrationsAssembly("BlackSlope.Api");
        }));
```

**Health Check Integration:**

The application includes comprehensive health checks using `AspNetCore.HealthChecks.SqlServer` (version 5.0.3) and `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (version 6.0.1):

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<MovieContext>()
    .AddSqlServer(configuration.GetConnectionString("MovieDatabase"));
```

These health checks enable monitoring of database connectivity and EF Core context health in production environments.

## Entity Configuration

### Entity Models

The `MovieDtoModel` represents the data transfer object for movie entities, mapping directly to the database schema:

```csharp
public class MovieDtoModel
{
    public int Id { get; set; }
    
    public string Title { get; set; }
    
    public string Description { get; set; }
    
    public DateTime? ReleaseDate { get; set; }
}
```

**Design Considerations:**

- **DTO Naming Convention**: The `DtoModel` suffix clearly indicates this is a data transfer object, not a domain model
- **Nullable Types**: `ReleaseDate` is nullable to accommodate movies without confirmed release dates
- **Simple Properties**: All properties use simple types for optimal serialization and database mapping
- **No Navigation Properties**: The model maintains a flat structure, avoiding complex object graphs

**Separation from Domain Models:**

This application follows the DTO pattern, separating database entities from domain models. The repository layer works with `MovieDtoModel`, while business logic operates on domain models. **AutoMapper** (version 10.1.1) handles transformations between these layers, ensuring clean separation of concerns.

### Fluent API Configuration

The `OnModelCreating` method provides Fluent API configuration for entity customization:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    Contract.Requires(modelBuilder != null);
    
    modelBuilder.Entity<MovieDtoModel>(entity =>
    {
        entity.HasIndex(e => e.Title)
            .HasDatabaseName("IX_Movies_Title");
    });

    modelBuilder.Seed();
}
```

**Configuration Details:**

- **Code Contracts**: Uses `System.Diagnostics.Contracts` for precondition validation
- **Index Creation**: Creates a non-clustered index on the `Title` column for optimized search queries
- **Explicit Index Naming**: Uses `HasDatabaseName` to ensure consistent index naming across environments
- **Seed Data**: Calls the `Seed()` extension method to populate initial data

**Index Strategy:**

The `IX_Movies_Title` index improves performance for:
- Title-based searches and filters
- Sorting operations on the Title column
- Duplicate title detection queries

**Performance Considerations:**

- Indexes improve read performance but add overhead to write operations
- The Title column is frequently queried, justifying the index
- Consider adding composite indexes if filtering by multiple columns becomes common

### Model Builder Extensions

The `ModelBuilderExtensions` class provides a clean, reusable approach to seed data:

```csharp
public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        Contract.Requires(modelBuilder != null);
        
        modelBuilder.Entity<MovieDtoModel>().HasData(
            new MovieDtoModel 
            { 
                Id = 1, 
                Title = "The Shawshank Redemption", 
                Description = "Lorem ipsum dolor sit amet...", 
                ReleaseDate = DateTime.Now 
            },
            new MovieDtoModel 
            { 
                Id = 2, 
                Title = "The Godfather", 
                Description = "Eos dolor perpetua ne...", 
                ReleaseDate = DateTime.Now 
            }
            // ... 48 additional movies
        );
    }
}
```

**Seed Data Strategy:**

- **Extension Method Pattern**: Keeps `OnModelCreating` clean and focused
- **Explicit IDs**: Manually assigned IDs ensure consistent data across environments
- **Development Data**: Provides 50 sample movies for testing and development
- **DateTime.Now Usage**: Note that this generates different timestamps on each migration

**Important Considerations:**

⚠️ **Production Warning**: The current implementation uses `DateTime.Now`, which generates different values each time migrations are applied. For production environments, consider:

```csharp
// Better approach for production
ReleaseDate = new DateTime(1994, 9, 23) // Actual release date
```

⚠️ **Migration Impact**: Seed data is included in migrations. Modifying seed data requires a new migration and careful consideration of existing production data.

## Database Context

### MovieContext Implementation

The `MovieContext` serves as the central hub for all database operations related to movies:

```csharp
public class MovieContext : DbContext
{
    private readonly MovieRepositoryConfiguration _config;

    public MovieContext(DbContextOptions<MovieContext> options)
        : base(options)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        _config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
            .Get<MovieRepositoryConfiguration>();
    }

    public virtual DbSet<MovieDtoModel> Movies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Contract.Requires(modelBuilder != null);
        modelBuilder.Entity<MovieDtoModel>(entity =>
        {
            entity.HasIndex(e => e.Title)
                .HasDatabaseName("IX_Movies_Title");
        });

        modelBuilder.Seed();
    }
}
```

### DbSet Definitions

**Movies DbSet:**

The `Movies` property provides a queryable collection of `MovieDtoModel` entities:

```csharp
public virtual DbSet<MovieDtoModel> Movies { get; set; }
```

**Usage Patterns:**

```csharp
// Querying
var movies = await _context.Movies.ToListAsync();
var movie = await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);
var exists = await _context.Movies.AnyAsync(m => m.Title == title);

// Adding
await _context.Movies.AddAsync(movie);
await _context.SaveChangesAsync();

// Updating
var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
dto.Title = movie.Title;
await _context.SaveChangesAsync();

// Deleting (Note: Uses synchronous FirstOrDefault)
var dto = _context.Movies.FirstOrDefault(x => x.Id == id);
_context.Movies.Remove(dto);
await _context.SaveChangesAsync();
```

**Virtual Modifier Benefits:**

- Enables mocking in unit tests using frameworks like Moq
- Supports Entity Framework proxies for lazy loading (if enabled)
- Allows derived classes to override behavior

### OnModelCreating Configuration

The `OnModelCreating` method is the central location for Fluent API configuration:

**Configuration Responsibilities:**

1. **Entity Mapping**: Defines how entities map to database tables
2. **Relationships**: Configures foreign keys and navigation properties (when applicable)
3. **Indexes**: Creates database indexes for performance optimization
4. **Constraints**: Defines unique constraints, check constraints, and default values
5. **Seed Data**: Populates initial data through the `Seed()` extension method

**Execution Timing:**

- Called once when the model is first created
- Results are cached for the lifetime of the application
- Changes require application restart to take effect

**Code Contracts Integration:**

```csharp
Contract.Requires(modelBuilder != null);
```

This precondition check ensures the `modelBuilder` parameter is never null, providing runtime validation in debug builds. While EF Core would never pass a null `modelBuilder`, this defensive programming practice documents the method's expectations.

## Best Practices

### Separation of Concerns

The application maintains clear boundaries between layers:

**Repository Layer** (`MovieRepository.cs`):
```csharp
public class MovieRepository : IMovieRepository
{
    private readonly MovieContext _context;

    public MovieRepository(MovieContext movieContext)
    {
        _context = movieContext;
    }

    public async Task<IEnumerable<MovieDtoModel>> GetAllAsync() =>
        await _context.Movies.ToListAsync();

    public async Task<MovieDtoModel> GetSingleAsync(int id) =>
        await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);
}
```

**Layer Responsibilities:**

| Layer | Responsibility | Example |
|-------|---------------|---------|
| **DbContext** | Database connection and entity tracking | `MovieContext` |
| **Repository** | Data access abstraction | `MovieRepository` |
| **Service/Business Logic** | Domain rules and orchestration | Movie validation, business rules |
| **Controller/API** | HTTP request handling | REST endpoints |

**Benefits:**

- **Testability**: Each layer can be tested independently
- **Maintainability**: Changes to data access don't affect business logic
- **Flexibility**: Easy to swap implementations (e.g., different databases)
- **Clarity**: Clear responsibilities reduce cognitive load

### Repository Pattern Integration

The repository pattern provides an abstraction over EF Core, offering several advantages:

**Interface Definition:**

```csharp
public interface IMovieRepository
{
    Task<IEnumerable<MovieDtoModel>> GetAllAsync();
    Task<MovieDtoModel> GetSingleAsync(int id);
    Task<MovieDtoModel> Create(MovieDtoModel movie);
    Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie);
    Task<int> DeleteAsync(int id);
    Task<bool> MovieExistsAsync(string title, DateTime? releaseDate);
}
```

**Implementation Highlights:**

```csharp
public async Task<MovieDtoModel> Create(MovieDtoModel movie)
{
    await _context.Movies.AddAsync(movie);
    await _context.SaveChangesAsync();
    return movie;
}

public async Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie)
{
    Contract.Requires(movie != null);
    var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
    dto.Title = movie.Title;
    dto.Description = movie.Description;
    await _context.SaveChangesAsync();
    return movie;
}

public async Task<bool> MovieExistsAsync(string title, DateTime? releaseDate) =>
    await _context.Movies.AnyAsync(m => m.Title == title && m.ReleaseDate == releaseDate);
```

**Pattern Benefits:**

- **Abstraction**: Business logic doesn't depend on EF Core specifics
- **Testing**: Easy to mock `IMovieRepository` for unit tests
- **Consistency**: Standardized data access methods across the application
- **Encapsulation**: Database queries are centralized and reusable

**Dependency Injection Registration:**

```csharp
services.AddScoped<IMovieRepository, MovieRepository>();
services.AddScoped<MovieContext>();
```

For more details on repository implementation, see [Repository Pattern Documentation](/features/repositories.md).

### Performance Optimization

**Async/Await Pattern:**

All database operations use async methods to prevent thread blocking:

```csharp
public async Task<IEnumerable<MovieDtoModel>> GetAllAsync() =>
    await _context.Movies.ToListAsync();
```

**Benefits:**
- Improved scalability under load
- Better resource utilization
- Non-blocking I/O operations

**Query Optimization Techniques:**

1. **Projection**: Select only required columns
```csharp
var titles = await _context.Movies
    .Select(m => new { m.Id, m.Title })
    .ToListAsync();
```

2. **Filtering**: Apply WHERE clauses before materialization
```csharp
var recentMovies = await _context.Movies
    .Where(m => m.ReleaseDate > DateTime.Now.AddYears(-1))
    .ToListAsync();
```

3. **Indexing**: Leverage the `IX_Movies_Title` index
```csharp
var movie = await _context.Movies
    .FirstOrDefaultAsync(m => m.Title == searchTitle); // Uses index
```

4. **AsNoTracking**: Disable change tracking for read-only queries
```csharp
var movies = await _context.Movies
    .AsNoTracking()
    .ToListAsync();
```

**Change Tracking Considerations:**

The current implementation uses tracked entities for updates:

```csharp
var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
dto.Title = movie.Title;
dto.Description = movie.Description;
await _context.SaveChangesAsync(); // EF Core detects changes automatically
```

**Alternative Approach (Explicit Update):**

```csharp
_context.Movies.Update(movie);
await _context.SaveChangesAsync();
```

**Caching Strategy:**

The application includes `Microsoft.Extensions.Caching.Memory` (version 6.0.2) for caching frequently accessed data:

```csharp
public async Task<IEnumerable<MovieDtoModel>> GetAllAsync()
{
    return await _cache.GetOrCreateAsync("all_movies", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        return await _context.Movies.ToListAsync();
    });
}
```

**Connection Resilience:**

The application uses **Polly** (version 7.2.2) for resilience patterns:

```csharp
services.AddDbContext<MovieContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));
```

**Migration Management:**

Migrations are managed through the `Microsoft.EntityFrameworkCore.Design` package (version 6.0.1):

```bash
# Create a new migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

For detailed migration procedures, see [Database Migrations Documentation](/database/migrations.md).

**Configuration Best Practices:**

For application settings and configuration management, refer to [Application Settings Documentation](/configuration/application_settings.md).

---

## Common Pitfalls and Troubleshooting

**Issue: DbContext Disposed Exception**

```csharp
// ❌ Bad: Context disposed before async operation completes
public IEnumerable<MovieDtoModel> GetAll()
{
    return _context.Movies.ToList(); // Synchronous
}

// ✅ Good: Proper async pattern
public async Task<IEnumerable<MovieDtoModel>> GetAllAsync()
{
    return await _context.Movies.ToListAsync();
}
```

**Issue: N+1 Query Problem**

```csharp
// ❌ Bad: Generates multiple queries
foreach (var movie in movies)
{
    var details = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movie.Id);
}

// ✅ Good: Single query with proper filtering
var movieIds = movies.Select(m => m.Id).ToList();
var details = await _context.Movies.Where(m => movieIds.Contains(m.Id)).ToListAsync();
```

**Issue: DateTime.Now in Seed Data**

The current seed data implementation uses `DateTime.Now`, which generates different timestamps on each migration. For production:

```csharp
// ✅ Better: Use fixed dates
new MovieDtoModel 
{ 
    Id = 1, 
    Title = "The Shawshank Redemption",
    ReleaseDate = new DateTime(1994, 9, 23)
}
```

---

## Related Documentation

- [Database Migrations](/database/migrations.md) - Migration creation and management
- [Repository Pattern](/features/repositories.md) - Repository implementation details
- [Application Settings](/configuration/application_settings.md) - Configuration management