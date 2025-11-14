# Repository Pattern

The BlackSlope application implements the Repository pattern to abstract data access logic and provide a clean separation between the business logic layer and data persistence layer. This pattern centralizes data access code, making it easier to maintain, test, and modify database operations without affecting the rest of the application.

## Repository Architecture

The repository architecture follows a contract-based design using interfaces to define data access operations. This approach provides several key benefits:

- **Abstraction**: Business logic depends on interfaces rather than concrete implementations
- **Testability**: Repositories can be easily mocked for unit testing
- **Flexibility**: Database implementations can be swapped without changing consuming code
- **Maintainability**: Data access logic is centralized in dedicated classes

### Repository Interface Design

The `IMovieRepository` interface defines the contract for all movie-related data operations:

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

**Key Design Decisions:**

- **Async/Await Pattern**: All methods return `Task` or `Task<T>` to support asynchronous database operations, preventing thread blocking during I/O operations
- **DTO Models**: Methods accept and return `MovieDtoModel` objects rather than domain entities, maintaining separation between database schema and business logic
- **Explicit Method Naming**: Method names clearly indicate their purpose (`GetAllAsync`, `GetSingleAsync`, etc.)
- **Existence Checking**: The `MovieExistsAsync` method provides a dedicated way to check for duplicate movies based on title and release date

### Data Access Abstraction

The repository pattern abstracts Entity Framework Core operations behind a clean interface. This abstraction layer:

1. **Hides EF Core Complexity**: Consuming code doesn't need to know about `DbContext`, `DbSet`, or LINQ queries
2. **Enforces Consistency**: All data access follows the same patterns and conventions
3. **Enables Testing**: Services can be tested with mock repositories without requiring a database
4. **Supports Future Changes**: The underlying data access technology can be changed without affecting consumers

For more information on how repositories fit into the overall application architecture, see [Architecture Layers](/architecture/layers.md).

### DTO Models

The `MovieDtoModel` serves as the data transfer object between the repository and service layers:

```csharp
public class MovieDtoModel
{
    public int Id { get; set; }
    
    public string Title { get; set; }
    
    public string Description { get; set; }
    
    public DateTime? ReleaseDate { get; set; }
}
```

**DTO Design Considerations:**

- **Simple Properties**: DTOs contain only data properties without business logic
- **Nullable Types**: `ReleaseDate` is nullable to handle movies without confirmed release dates
- **Database Mapping**: Properties map directly to database columns via Entity Framework conventions
- **Separation of Concerns**: DTOs are distinct from domain models or view models, serving specifically as database entities

The DTO pattern prevents tight coupling between the database schema and business logic. Services can use AutoMapper (version 10.1.1) to transform DTOs into domain models or view models as needed. See [Services](/features/services.md) for more details on how DTOs are transformed.

## Movie Repository

The `MovieRepository` class implements `IMovieRepository` using Entity Framework Core 6.0.1 as the data access technology.

### Entity Framework Context

The repository depends on `MovieContext`, which is injected via constructor dependency injection:

```csharp
public class MovieRepository : IMovieRepository
{
    private readonly MovieContext _context;

    public MovieRepository(MovieContext movieContext)
    {
        _context = movieContext;
    }
    
    // ... implementation methods
}
```

**Dependency Injection Benefits:**

- **Lifetime Management**: The DI container manages the `DbContext` lifetime (typically scoped per HTTP request)
- **Configuration**: Connection strings and options are configured centrally in the DI container
- **Testing**: Mock contexts can be injected for unit testing

The repository is registered in the DI container with a scoped lifetime, ensuring each HTTP request gets its own repository instance with its own `DbContext`.

### CRUD Operations

#### Create Operation

```csharp
public async Task<MovieDtoModel> Create(MovieDtoModel movie)
{
    await _context.Movies.AddAsync(movie);
    await _context.SaveChangesAsync();

    return movie;
}
```

**Implementation Notes:**

- Uses `AddAsync` for asynchronous entity tracking
- `SaveChangesAsync` persists changes to the database
- Returns the created entity, which now includes the database-generated `Id`
- **Gotcha**: The returned `movie` object is the same reference passed in, but with the `Id` property populated by EF Core after insertion

#### Read Operations

**Get All Movies:**

```csharp
public async Task<IEnumerable<MovieDtoModel>> GetAllAsync() =>
    await _context.Movies.ToListAsync();
```

**Get Single Movie:**

```csharp
public async Task<MovieDtoModel> GetSingleAsync(int id) =>
    await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);
```

**Implementation Notes:**

- `ToListAsync()` executes the query and materializes all results into memory
- `FirstOrDefaultAsync()` returns `null` if no movie with the specified ID exists
- Both methods use expression-bodied syntax for concise implementation
- **Performance Consideration**: `GetAllAsync` loads all movies into memory; consider pagination for large datasets

#### Update Operation

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

**Implementation Notes:**

- Uses `Contract.Requires` for precondition validation (from `System.Diagnostics.Contracts`)
- Retrieves the existing entity from the context before updating
- Manually assigns changed properties rather than using `Update()` method
- **Gotcha**: This implementation doesn't update `ReleaseDate`, which may be intentional or an oversight
- **Potential Issue**: No null check on `dto` after retrieval; will throw `NullReferenceException` if movie doesn't exist
- Change tracking automatically detects modifications; `SaveChangesAsync` generates the UPDATE statement

**Recommended Improvement:**

```csharp
public async Task<MovieDtoModel> UpdateAsync(MovieDtoModel movie)
{
    Contract.Requires(movie != null);
    var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == movie.Id);
    
    if (dto == null)
    {
        throw new InvalidOperationException($"Movie with ID {movie.Id} not found");
    }
    
    dto.Title = movie.Title;
    dto.Description = movie.Description;
    dto.ReleaseDate = movie.ReleaseDate; // Include all updatable properties
    await _context.SaveChangesAsync();

    return dto; // Return the tracked entity
}
```

#### Delete Operation

```csharp
public async Task<int> DeleteAsync(int id)
{
    var dto = _context.Movies.FirstOrDefault(x => x.Id == id);
    _context.Movies.Remove(dto);
    await _context.SaveChangesAsync();

    return id;
}
```

**Implementation Notes:**

- Uses synchronous `FirstOrDefault` (inconsistent with other methods)
- **Potential Issue**: No null check; will throw exception if movie doesn't exist
- `Remove()` marks the entity for deletion
- Returns the deleted ID for confirmation
- **Gotcha**: If the entity doesn't exist, this will throw an exception rather than returning a failure indicator

**Recommended Improvement:**

```csharp
public async Task<int> DeleteAsync(int id)
{
    var dto = await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);
    
    if (dto == null)
    {
        throw new InvalidOperationException($"Movie with ID {id} not found");
    }
    
    _context.Movies.Remove(dto);
    await _context.SaveChangesAsync();

    return id;
}
```

#### Existence Check

```csharp
public async Task<bool> MovieExistsAsync(string title, DateTime? releaseDate) =>
    await _context.Movies.AnyAsync(m => m.Title == title && m.ReleaseDate == releaseDate);
```

**Implementation Notes:**

- Uses `AnyAsync` for efficient existence checking (doesn't load entities into memory)
- Checks both title and release date for duplicate detection
- Returns `true` if any matching movie exists
- **Performance**: More efficient than loading entities and checking in memory
- **Case Sensitivity**: String comparison is case-sensitive by default in SQL Server; consider using case-insensitive comparison if needed

### Query Optimization

The current repository implementation uses basic Entity Framework queries. Here are optimization considerations:

**Current State:**

- No explicit query optimization (relying on EF Core defaults)
- No pagination support in `GetAllAsync`
- No filtering or sorting capabilities
- No eager loading configuration

**Optimization Opportunities:**

1. **Pagination**: Implement skip/take for large datasets
2. **Filtering**: Add methods with predicate parameters
3. **Projection**: Use `Select()` to load only required columns
4. **Indexing**: The `Title` column has an index (configured in `OnModelCreating`)
5. **AsNoTracking**: Use for read-only queries to improve performance

**Example Optimized Query:**

```csharp
public async Task<IEnumerable<MovieDtoModel>> GetPagedAsync(int pageNumber, int pageSize)
{
    return await _context.Movies
        .AsNoTracking() // Read-only, no change tracking overhead
        .OrderBy(m => m.Title)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}
```

For more information on Entity Framework optimization techniques, see [Entity Framework](/database/entity_framework.md).

## Database Context

The `MovieContext` class extends `DbContext` and configures the Entity Framework Core data access layer.

### DbContext Configuration

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
    
    // ... OnModelCreating implementation
}
```

**Configuration Approach:**

- **Options Pattern**: Accepts `DbContextOptions<MovieContext>` for configuration
- **Configuration Loading**: Reads `appsettings.json` directly in the constructor
- **Assembly-Based Section**: Uses the executing assembly name as the configuration section key
- **Virtual DbSet**: The `Movies` property is virtual to support mocking in tests

**Gotcha**: Loading configuration in the constructor is unusual and potentially problematic:

- Configuration is loaded on every context instantiation
- File I/O occurs during object construction
- This approach is primarily needed for design-time tooling (migrations)
- In production, configuration should be injected via DI

**Recommended Production Approach:**

```csharp
public class MovieContext : DbContext
{
    public MovieContext(DbContextOptions<MovieContext> options)
        : base(options)
    {
        // Configuration handled by DI container
    }

    public virtual DbSet<MovieDtoModel> Movies { get; set; }
}
```

### Entity Configuration

The `OnModelCreating` method configures entity mappings and database schema:

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

1. **Precondition Check**: Uses `Contract.Requires` to validate the `modelBuilder` parameter
2. **Index Configuration**: Creates a non-clustered index on the `Title` column
3. **Explicit Index Naming**: Uses `HasDatabaseName` to specify the index name
4. **Seed Data**: Calls an extension method to populate initial data

**Index Benefits:**

- Improves query performance for title-based searches
- Supports the `MovieExistsAsync` method efficiently
- Named explicitly for easier identification in database tools

**Entity Configuration Best Practices:**

For complex entities, consider using separate configuration classes:

```csharp
public class MovieConfiguration : IEntityTypeConfiguration<MovieDtoModel>
{
    public void Configure(EntityTypeBuilder<MovieDtoModel> builder)
    {
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(m => m.Description)
            .HasMaxLength(1000);
            
        builder.HasIndex(m => m.Title)
            .HasDatabaseName("IX_Movies_Title");
    }
}

// In OnModelCreating:
modelBuilder.ApplyConfiguration(new MovieConfiguration());
```

### Connection String Management

Connection strings are managed through the `MovieRepositoryConfiguration` class:

**Configuration Structure:**

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "Server=...;Database=Movies;..."
  }
}
```

**Configuration Class:**

```csharp
public class MovieRepositoryConfiguration
{
    public string MoviesConnectionString { get; set; }
}
```

**Registration in Startup/Program:**

```csharp
services.AddDbContext<MovieContext>(options =>
    options.UseSqlServer(
        configuration.GetSection("BlackSlope.Api:MoviesConnectionString").Value));
```

**Security Considerations:**

- **User Secrets**: For local development, use User Secrets (supported via `Microsoft.Extensions.Configuration.UserSecrets`)
- **Azure Key Vault**: For production, store connection strings in Azure Key Vault
- **Environment Variables**: Use environment-specific configuration files
- **Never Commit**: Ensure connection strings are never committed to source control

**Connection String Features:**

The application uses Microsoft SQL Server (via `Microsoft.Data.SqlClient` 5.1.3) with the following capabilities:

- **Connection Pooling**: Enabled by default for performance
- **Retry Logic**: Can be configured with Polly (7.2.2) for transient fault handling
- **Health Checks**: Monitored via `AspNetCore.HealthChecks.SqlServer` (5.0.3)

For more details on database health monitoring, see [Entity Framework](/database/entity_framework.md).

## Design-Time Factory

The `DesignTimeDbContextFactory` enables Entity Framework Core tools (like migrations) to create instances of `MovieContext` at design time.

### Supporting EF Migrations

```csharp
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MovieContext>
{
    public MovieContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

        var builder = new DbContextOptionsBuilder<MovieContext>();
        var config = configuration.GetSection(Assembly.GetExecutingAssembly().GetName().Name)
                        .Get<MovieRepositoryConfiguration>();
        builder.UseSqlServer(config.MoviesConnectionString);

        return new MovieContext(builder.Options);
    }
}
```

**Purpose and Functionality:**

The design-time factory is used by EF Core tools when running commands like:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

**How It Works:**

1. EF Core tools discover the factory by scanning for `IDesignTimeDbContextFactory<T>` implementations
2. The factory creates a `MovieContext` instance without requiring a running application
3. Configuration is loaded from `appsettings.json` in the current directory
4. The context is configured with the connection string from configuration
5. Tools use this context to generate migrations or update the database

**Key Implementation Details:**

- **Configuration Loading**: Reads `appsettings.json` from the current directory (typically the project root)
- **Assembly-Based Section**: Uses the same configuration section pattern as the runtime context
- **SQL Server Provider**: Explicitly configures `UseSqlServer` with the connection string
- **No DI Container**: Creates the context manually without dependency injection

### Development vs Production Contexts

The design-time factory creates a context specifically for development tooling, separate from the runtime context:

| Aspect | Design-Time Factory | Runtime Context |
|--------|-------------------|-----------------|
| **Purpose** | EF Core tooling (migrations, scaffolding) | Application data access |
| **Creation** | Manual instantiation | Dependency injection |
| **Configuration** | Direct file reading | DI configuration system |
| **Lifetime** | Short-lived (per tool invocation) | Scoped (per HTTP request) |
| **Connection String** | Development database | Environment-specific database |

**Development Workflow:**

1. **Create Migration**: Developer runs `dotnet ef migrations add <name>`
   - Factory creates context using development connection string
   - EF Core compares model to database schema
   - Migration code is generated

2. **Update Database**: Developer runs `dotnet ef database update`
   - Factory creates context using development connection string
   - EF Core applies pending migrations to the database

3. **Runtime Execution**: Application starts
   - DI container creates context using environment-specific configuration
   - Application uses production/staging/development database as configured

**Best Practices:**

1. **Separate Databases**: Use different databases for development, staging, and production
2. **Migration Testing**: Test migrations on a copy of production data before deploying
3. **Source Control**: Commit migration files to source control
4. **Deployment**: Apply migrations as part of the deployment pipeline

**Common Issues:**

- **Wrong Directory**: If `appsettings.json` isn't found, ensure you're running commands from the project directory
- **Configuration Section**: Verify the configuration section name matches the assembly name
- **Connection String**: Ensure the development connection string is valid and accessible
- **Multiple Contexts**: If you have multiple contexts, ensure each has its own factory

For detailed information on creating and managing migrations, see [Database Migrations](/database/migrations.md).

**Migration Command Examples:**

```bash
# Add a new migration
dotnet ef migrations add AddMovieReleaseDate --project src/BlackSlope.Api

# Update database to latest migration
dotnet ef database update --project src/BlackSlope.Api

# Generate SQL script for migration
dotnet ef migrations script --project src/BlackSlope.Api --output migration.sql

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/BlackSlope.Api
```

---

## Summary

The Repository pattern implementation in BlackSlope provides a clean, maintainable approach to data access:

- **Clear Abstraction**: The `IMovieRepository` interface defines a contract independent of implementation details
- **EF Core Integration**: The `MovieRepository` leverages Entity Framework Core 6.0.1 for database operations
- **Proper Configuration**: The `MovieContext` configures entity mappings and database schema
- **Tooling Support**: The `DesignTimeDbContextFactory` enables EF Core migrations and scaffolding

**Areas for Improvement:**

1. Add null checks in update and delete operations
2. Implement pagination for `GetAllAsync`
3. Consider using `AsNoTracking()` for read-only queries
4. Add more comprehensive error handling
5. Include all properties in update operations
6. Use consistent async patterns throughout

For information on how services consume repositories, see [Services](/features/services.md). For broader architectural context, see [Architecture Layers](/architecture/layers.md).