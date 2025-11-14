# Seeding Data

## Overview

Data seeding is the process of populating a database with initial data during application setup or migration. In the BlackSlope.NET application, data seeding is implemented using Entity Framework Core's migration-based approach to ensure consistent initial state across development, testing, and production environments.

The application demonstrates seeding through a comprehensive movie database example, utilizing both migration-based seeding with the `HasData` method and ModelBuilder extensions for maintainable seed data configuration.

## Data Seeding Strategies

### Migration-Based Seeding

Migration-based seeding is the primary approach used in BlackSlope.NET. This strategy embeds seed data directly into Entity Framework Core migrations, ensuring that data is applied atomically with schema changes.

**Advantages:**
- Version controlled alongside schema changes
- Automatically applied during `dotnet ef database update`
- Supports rollback through the `Down()` method
- Idempotent by design (EF Core tracks applied migrations)
- Works consistently across all environments

**Implementation Location:**
- `src/BlackSlope.Api/Repositories/Migrations/20190814225910_seeded.cs`

### SQL Script Seeding

The application also includes a standalone SQL script for manual data seeding scenarios:

**Location:** `scripts/data.sql`

```sql
INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('The Shawshank Redemption','Two imprisoned men bond over a number of years, finding solace and eventual redemption through acts of common decency.')
GO
INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('The Godfather','The aging patriarch of an organized crime dynasty transfers control of his clandestine empire to his reluctant son.')
GO
```

**Use Cases:**
- Manual database initialization
- Quick data population during development
- Database restoration scenarios
- Testing with specific data sets

**Note:** This approach is less preferred for production deployments as it requires manual execution and doesn't integrate with EF Core's migration tracking.

### Application Startup Seeding

While not explicitly implemented in the current codebase, application startup seeding can be configured in `Program.cs` or `Startup.cs` using the `DbContext` during application initialization. This approach is useful for:
- Dynamic seed data based on configuration
- Environment-specific data
- Data that changes frequently

## Initial Seed Data

### Movie Data Seeding

The application seeds 50 movie records as demonstration data. This seed data is implemented in the `Seeded` migration class.

**Migration Structure:**

```csharp
public partial class Seeded : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        Contract.Requires(migrationBuilder != null);

        migrationBuilder.InsertData(
            table: "Movies",
            columns: new[] { "Id", "Description", "ReleaseDate", "Title" },
            values: new object[,]
            {
                { 1, "Lorem ipsum dolor sit amet...", 
                  new DateTime(2019, 8, 14, 17, 59, 10, 715, DateTimeKind.Local).AddTicks(5038), 
                  "The Shawshank Redemption" },
                // ... additional 49 records
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        Contract.Requires(migrationBuilder != null);
        
        migrationBuilder.DeleteData(
            table: "Movies",
            keyColumn: "Id",
            keyValue: 1);
        // ... delete statements for all 50 records
    }
}
```

**Key Implementation Details:**

1. **Explicit ID Assignment:** Each movie is assigned a specific ID (1-50), ensuring consistent references across environments
2. **DateTime Handling:** Release dates use fixed timestamps from migration creation time (August 14, 2019 at 5:59:10 PM local time), resulting in consistent timestamps across all environments
3. **Rollback Support:** The `Down()` method explicitly deletes each seeded record by ID
4. **Code Contracts:** Uses `Contract.Requires()` for null-checking (requires System.Diagnostics.Contracts)
5. **StyleCop Suppression:** Includes `[SuppressMessage]` attribute to allow multi-line parameter formatting for better readability

### data.sql Script

The SQL script provides a simplified version with only 5 movies:

```sql
INSERT INTO [dbo].[Movies] ([Title],[Description]) 
VALUES ('The Shawshank Redemption','Two imprisoned men bond over a number of years, finding solace and eventual redemption through acts of common decency.')
GO
```

**Differences from Migration Seeding:**
- No explicit ID assignment (relies on IDENTITY column)
- No ReleaseDate values (allows database defaults)
- Simpler syntax for quick manual execution
- Not tracked by EF Core migration system

### Seed Migration Execution

To apply the seed migration:

```bash
# From repository root
dotnet ef database update --project=.\src\BlackSlope.Api\BlackSlope.Api.csproj
```

**Expected Output:**
```
Build started...
Build succeeded.
Applying migration '20190814225754_initialized'.
Applying migration '20190814225910_seeded'.
Done.
```

**Troubleshooting:**
- Ensure SQL Server is running and accessible
- Verify connection string in `appsettings.json` (MoviesConnectionString)
- Confirm database exists before running migrations
- Check that `dotnet-ef` tool is installed globally

## ModelBuilder Extensions

### HasData Method

Entity Framework Core's `HasData` method provides a fluent API for configuring seed data within the `OnModelCreating` method of your `DbContext`.

**Implementation in MovieContext:**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    Contract.Requires(modelBuilder != null);
    
    modelBuilder.Entity<MovieDtoModel>(entity =>
    {
        entity.HasIndex(e => e.Title)
            .HasDatabaseName("IX_Movies_Title");
    });

    modelBuilder.Seed(); // Extension method call
}
```

### Seed Data Configuration

The seed data configuration is extracted into a reusable extension method for better organization and maintainability.

**Note:** The exact location and implementation details of the `ModelBuilderExtensions.Seed()` method are not shown in the `MovieContext.cs` file, but it is invoked in the `OnModelCreating` method.

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
                Description = "Lorem ipsum dolor sit amet, ut consul soluta persius quo, et eam mundi scribentur, eros invidunt dissentias no eum.", 
                ReleaseDate = DateTime.Now 
            },
            new MovieDtoModel 
            { 
                Id = 2, 
                Title = "The Godfather", 
                Description = "Eos dolor perpetua ne, cum agam causae petentium ei.", 
                ReleaseDate = DateTime.Now 
            },
            // ... 48 more records (total of 50 movies seeded)
        );
    }
}
```

**Design Benefits:**

1. **Separation of Concerns:** Seed logic is isolated from DbContext configuration
2. **Reusability:** Extension method can be called from multiple contexts if needed
3. **Maintainability:** Easier to update seed data without modifying core DbContext
4. **Testability:** Can be unit tested independently
5. **Readability:** Cleaner `OnModelCreating` method

### Relationship Seeding

While the current implementation only seeds a single entity (`MovieDtoModel`), the `HasData` method supports seeding related entities:

```csharp
// Example: Seeding related entities (not in current codebase)
modelBuilder.Entity<Genre>().HasData(
    new Genre { Id = 1, Name = "Drama" },
    new Genre { Id = 2, Name = "Crime" }
);

modelBuilder.Entity<MovieGenre>().HasData(
    new MovieGenre { MovieId = 1, GenreId = 1 }, // Shawshank -> Drama
    new MovieGenre { MovieId = 2, GenreId = 2 }  // Godfather -> Crime
);
```

**Important Considerations:**
- Foreign key values must reference existing seed data
- Seed data is applied in the order entities are configured
- Navigation properties are not set during seeding (only foreign keys)
- Many-to-many relationships require explicit join table seeding

## Best Practices

### Development vs Production Data

**Development Environment:**
```csharp
// Use realistic but fake data
new MovieDtoModel 
{ 
    Id = 1, 
    Title = "The Shawshank Redemption", 
    Description = "Lorem ipsum dolor sit amet...", // Placeholder text
    ReleaseDate = DateTime.Now 
}
```

**Production Environment:**
```csharp
// Use actual production data or minimal required data
new MovieDtoModel 
{ 
    Id = 1, 
    Title = "The Shawshank Redemption", 
    Description = "Two imprisoned men bond over a number of years...", 
    ReleaseDate = new DateTime(1994, 9, 23) // Actual release date
}
```

**Recommendations:**

1. **Environment-Specific Migrations:** Consider separate migrations for development and production seed data
2. **Configuration-Based Seeding:** Use `appsettings.{Environment}.json` to control seeding behavior
3. **Minimal Production Seeds:** Only seed essential reference data in production
4. **Test Data Isolation:** Use separate seed migrations for integration tests

### Idempotent Seeding

Idempotent seeding ensures that running seed operations multiple times produces the same result without errors or duplicate data.

**EF Core Migration Approach (Idempotent by Default):**
```csharp
// EF Core tracks applied migrations in __EFMigrationsHistory table
// Running 'dotnet ef database update' multiple times is safe
dotnet ef database update
```

**Manual SQL Script Approach (Requires Explicit Checks):**
```sql
-- Check before inserting
IF NOT EXISTS (SELECT 1 FROM Movies WHERE Id = 1)
BEGIN
    INSERT INTO Movies (Id, Title, Description, ReleaseDate)
    VALUES (1, 'The Shawshank Redemption', 'Description...', GETDATE())
END
GO
```

**Application Startup Approach:**
```csharp
// In Program.cs or Startup.cs
public static async Task SeedDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MovieContext>();
    
    // Check if data already exists
    if (!await context.Movies.AnyAsync())
    {
        context.Movies.AddRange(GetSeedMovies());
        await context.SaveChangesAsync();
    }
}
```

### Test Data Management

**Integration Test Seeding Strategy:**

```csharp
// Example: Test-specific seeding (not in current codebase)
public class TestDataSeeder
{
    public static void SeedTestData(MovieContext context)
    {
        // Clear existing data
        context.Movies.RemoveRange(context.Movies);
        context.SaveChanges();
        
        // Add test-specific data
        context.Movies.AddRange(
            new MovieDtoModel { Id = 1, Title = "Test Movie 1", ... },
            new MovieDtoModel { Id = 2, Title = "Test Movie 2", ... }
        );
        context.SaveChanges();
    }
}
```

**Best Practices for Test Data:**

1. **Isolated Test Databases:** Use separate database instances for testing
2. **Transaction Rollback:** Wrap tests in transactions that rollback after execution
3. **In-Memory Databases:** Consider EF Core's in-memory provider for unit tests
4. **Fixture Pattern:** Use test fixtures to manage seed data lifecycle
5. **Deterministic Data:** Avoid `DateTime.Now` in test seeds; use fixed dates

**Example Test Configuration:**

```csharp
// appsettings.test.json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=localhost;Database=BlackSlope_Test;..."
  }
}
```

### Common Pitfalls and Solutions

| Issue | Problem | Solution |
|-------|---------|----------|
| **Duplicate Keys** | Seeding same ID multiple times | Use explicit ID assignment and check for existing data |
| **DateTime Precision** | `DateTime.Now` causes migration regeneration | Use fixed dates or `DateTime.UtcNow` with specific precision |
| **Foreign Key Violations** | Related entities seeded in wrong order | Seed parent entities before children |
| **Migration Conflicts** | Multiple developers create seed migrations | Coordinate migration creation or use merge strategies |
| **Large Seed Data** | Migration files become unwieldy | Extract to separate files or use SQL scripts |

### Performance Considerations

**For Large Seed Data Sets:**

```csharp
// Instead of individual InsertData calls
migrationBuilder.InsertData(
    table: "Movies",
    columns: new[] { "Id", "Title", "Description", "ReleaseDate" },
    values: new object[,]
    {
        { 1, "Movie 1", "Description 1", date1 },
        { 2, "Movie 2", "Description 2", date2 },
        // ... thousands of records
    });

// Consider using SQL bulk insert
migrationBuilder.Sql(@"
    BULK INSERT Movies
    FROM 'C:\data\movies.csv'
    WITH (FIELDTERMINATOR = ',', ROWTERMINATOR = '\n')
");
```

## Related Documentation

- [Database Migrations](/database/migrations.md) - Comprehensive guide to EF Core migrations
- [Entity Framework Configuration](/database/entity_framework.md) - DbContext setup and configuration
- [Installation Guide](/getting_started/installation.md) - Initial database setup instructions

## Additional Resources

**Entity Framework Core Documentation:**
- [Data Seeding](https://docs.microsoft.com/en-us/ef/core/modeling/data-seeding)
- [Migrations Overview](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

**BlackSlope.NET Blog Posts:**
- [BlackSlope in Action: A Guide to Using our DotNet Reference Architecture](https://medium.com/slalom-build/blackslope-in-action-a-guide-to-using-our-dotnet-reference-architecture-d1e41eea8024)