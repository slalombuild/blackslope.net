# Database Migrations

This document provides comprehensive guidance on managing database migrations in the BlackSlope application using Entity Framework Core 6.0. The migration system enables version-controlled, incremental changes to the database schema while maintaining data integrity across development, staging, and production environments.

## EF Core Migrations

Entity Framework Core migrations provide a code-first approach to database schema management, allowing developers to define database structure through C# code and apply changes incrementally.

### Creating Migrations

To create a new migration, use the Entity Framework Core CLI tools from the project directory containing your `DbContext`:

```bash
cd src/BlackSlope.Api
dotnet ef migrations add <MigrationName> -v
```

**Best Practices for Migration Names:**
- Use PascalCase naming convention (e.g., `AddUserEmailIndex`)
- Choose descriptive names that clearly indicate the schema change
- Avoid generic names like "Update" or "Changes"
- Use past tense verbs (e.g., `AddedMovieRatings`, `RemovedObsoleteColumns`)

The `-v` (verbose) flag provides detailed output during migration creation, which is useful for troubleshooting configuration issues.

### Migration File Structure

Each migration generates two files in the `Migrations` folder:

1. **Migration Class** (`YYYYMMDDHHMMSS_<MigrationName>.cs`): Contains `Up()` and `Down()` methods
2. **Model Snapshot** (`MovieContextModelSnapshot.cs`): Represents the current state of the database model

Example migration structure from the codebase:

```csharp
using System;
using System.Diagnostics.Contracts;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BlackSlope.Api.Migrations
{
    /// <summary>
    /// Manages initialization and cleanup of database tables.
    /// </summary>
    public partial class Initialized : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Contract.Requires(migrationBuilder != null);
            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", 
                            SqlServerValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    ReleaseDate = table.Column<DateTime>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Title",
                table: "Movies",
                column: "Title");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Contract.Requires(migrationBuilder != null);
            migrationBuilder.DropTable(name: "Movies");
        }
    }
}
```

**Key Components:**

- **Up() Method**: Defines forward migration operations (schema changes to apply)
- **Down() Method**: Defines rollback operations (how to undo the migration)
- **Contract.Requires()**: Code contract validation ensuring `migrationBuilder` is not null
- **Annotations**: SQL Server-specific configurations (e.g., identity column generation)

### Applying Migrations

Apply pending migrations to update the database schema:

```bash
# Apply all pending migrations
dotnet ef database update -v

# Apply migrations up to a specific migration
dotnet ef database update <MigrationName>

# Rollback to a previous migration
dotnet ef database update <PreviousMigrationName>
```

The application uses **Microsoft.EntityFrameworkCore.SqlServer** as the database provider, ensuring compatibility with Microsoft SQL Server features. The model snapshot indicates EF Core version 2.2.4 was used for migration generation.

### Migration History

Entity Framework Core tracks applied migrations in the `__EFMigrationsHistory` table within your database. This table contains:

| Column | Type | Description |
|--------|------|-------------|
| MigrationId | nvarchar(150) | Unique identifier (timestamp + migration name) |
| ProductVersion | nvarchar(32) | EF Core version used to create the migration |

**Viewing Migration History:**

```bash
# List all migrations and their status
dotnet ef migrations list

# View SQL that would be executed
dotnet ef migrations script
```

**Important Considerations:**

- Never manually modify the `__EFMigrationsHistory` table
- Migrations are applied in chronological order based on the timestamp prefix
- Once a migration is applied in production, avoid modifying its code
- Create a new migration to correct issues rather than editing existing ones

## Initial Migration

The initial migration establishes the foundational database schema and is critical for new environment setup.

### Database Initialization

The `20190814225754_initialized.cs` migration creates the core `Movies` table with the following schema:

```csharp
migrationBuilder.CreateTable(
    name: "Movies",
    columns: table => new
    {
        Id = table.Column<int>(nullable: false)
            .Annotation("SqlServer:ValueGenerationStrategy", 
                SqlServerValueGenerationStrategy.IdentityColumn),
        Title = table.Column<string>(nullable: true),
        Description = table.Column<string>(nullable: true),
        ReleaseDate = table.Column<DateTime>(nullable: true),
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Movies", x => x.Id);
    });
```

**Schema Details:**

- **Primary Key**: `Id` column with SQL Server identity auto-increment
- **Nullable Columns**: `Title`, `Description`, and `ReleaseDate` allow NULL values
- **Data Types**: Uses appropriate SQL Server types (int, nvarchar(max), datetime2)

### Schema Creation

The initial migration includes performance optimization through indexing:

```csharp
migrationBuilder.CreateIndex(
    name: "IX_Movies_Title",
    table: "Movies",
    column: "Title");
```

This index improves query performance for title-based searches, which are common in movie lookup operations.

**Index Naming Convention:**
- Format: `IX_<TableName>_<ColumnName>`
- Provides clear identification of indexed columns
- Follows Entity Framework Core conventions

### Seeding Initial Data

The application uses model-level data seeding configured in the `MovieContext` and captured in the `MovieContextModelSnapshot.cs`. This approach embeds seed data in the model configuration using the `HasData()` method:

```csharp
modelBuilder.Entity("BlackSlope.Repositories.Movies.DtoModels.MovieDtoModel", b =>
{
    // ... property and key configuration
    
    b.HasData(
        new
        {
            Id = 1,
            Description = "Lorem ipsum dolor sit amet...",
            ReleaseDate = new DateTime(2019, 8, 14, 17, 59, 10, 715, DateTimeKind.Local).AddTicks(5038),
            Title = "The Shawshank Redemption"
        },
        new
        {
            Id = 2,
            Description = "Eos dolor perpetua ne...",
            ReleaseDate = new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8797),
            Title = "The Godfather"
        },
        // ... additional seed data
    );
});
```

**Seed Data Characteristics:**

- **50 Movie Records**: Provides substantial test data for development
- **Explicit IDs**: Ensures consistent identity values across environments
- **Timestamp Precision**: Uses `DateTimeKind.Local` with tick-level precision
- **Model-Level Seeding**: Data is defined in the entity configuration and tracked in the model snapshot

For detailed information on data seeding strategies, see [/database/seeding_data.md](/database/seeding_data.md).

## Migration Scripts

The project includes automation scripts to streamline migration execution and reduce human error.

### db-update.sh Script

Located at `scripts/db-update.sh`, this shell script automates the migration process:

```bash
cd ../src/BlackSlope.Api
dotnet ef database update -v 
```

**Script Functionality:**

1. **Directory Navigation**: Changes to the API project directory containing the `DbContext`
2. **Migration Execution**: Runs `dotnet ef database update` with verbose output
3. **Error Handling**: Shell will exit with non-zero status if migration fails

**Usage:**

```bash
# From the scripts directory
./db-update.sh

# From project root
./scripts/db-update.sh
```

**Prerequisites:**

- Entity Framework Core CLI tools installed globally:
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- Proper connection string configuration in `appsettings.json`
- Appropriate database permissions for the configured user

### Automated Migration Execution

For CI/CD pipelines and automated deployments, consider these approaches:

**Option 1: Application Startup Migration**

Apply migrations automatically when the application starts (suitable for development/staging):

```csharp
public static void Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();
    
    using (var scope = host.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MovieContext>();
        context.Database.Migrate(); // Applies pending migrations
    }
    
    host.Run();
}
```

**⚠️ Production Warning**: Automatic migrations on startup can cause issues in production:
- Application downtime during migration
- Potential data loss if migrations fail
- Difficulty coordinating migrations across multiple instances

**Option 2: Deployment Pipeline Migration**

Execute migrations as a separate deployment step:

```yaml
# Example Azure DevOps pipeline step
- task: DotNetCoreCLI@2
  displayName: 'Apply Database Migrations'
  inputs:
    command: 'custom'
    custom: 'ef'
    arguments: 'database update --project src/BlackSlope.Api'
```

**Option 3: SQL Script Generation**

Generate idempotent SQL scripts for DBA review and execution:

```bash
# Generate script for all pending migrations
dotnet ef migrations script --idempotent --output migrations.sql

# Generate script for specific migration range
dotnet ef migrations script <FromMigration> <ToMigration> --output migrations.sql
```

The `--idempotent` flag ensures the script can be run multiple times safely, checking for existing schema changes before applying them.

### Rollback Procedures

When issues occur after migration, follow these rollback procedures:

**1. Identify Target Migration**

```bash
# List all migrations
dotnet ef migrations list

# Identify the last known good migration
```

**2. Execute Rollback**

```bash
# Rollback to specific migration
dotnet ef database update <LastGoodMigration>
```

**3. Verify Database State**

```bash
# Check migration history
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC;

# Verify schema changes were reverted
```

**4. Remove Failed Migration Files**

If the migration was never successfully applied in production:

```bash
# Remove the migration files
dotnet ef migrations remove
```

**⚠️ Critical Rollback Considerations:**

- **Data Loss Risk**: Down migrations may drop tables or columns containing data
- **Production Coordination**: Ensure application code is compatible with rolled-back schema
- **Backup First**: Always backup production databases before rollback operations
- **Test Rollback**: Verify rollback procedures in staging environment first

**Rollback Limitations:**

Some operations cannot be safely rolled back:
- Dropped columns with data
- Renamed tables referenced by external systems
- Data transformations that lose information

For these scenarios, create forward-fixing migrations instead of rolling back.

## Design-Time Support

Entity Framework Core requires design-time services to generate migrations and scaffold database contexts. The application implements this through the `DesignTimeDbContextFactory`.

### DesignTimeDbContextFactory

Located at `src/BlackSlope.Api/Repositories/Movies/Context/DesignTimeDbContextFactory.cs`, this factory enables EF Core tooling to instantiate the `MovieContext` at design time:

```csharp
using System.IO;
using System.Reflection;
using BlackSlope.Repositories.Movies.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BlackSlope.Repositories.Movies.Context
{
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
}
```

**Implementation Details:**

1. **IDesignTimeDbContextFactory Interface**: EF Core discovers this implementation automatically
2. **Configuration Loading**: Reads `appsettings.json` from the current directory
3. **Assembly-Specific Configuration**: Uses the executing assembly name as the configuration section key
4. **Connection String Resolution**: Extracts `MoviesConnectionString` from `MovieRepositoryConfiguration`
5. **SQL Server Provider**: Configures the context with SQL Server-specific options

**Configuration Structure:**

The factory expects configuration in this format:

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "Server=localhost;Database=BlackSlopeMovies;Trusted_Connection=True;"
  }
}
```

### Supporting Tooling

The design-time factory enables these EF Core CLI commands:

```bash
# Add new migration
dotnet ef migrations add <MigrationName>

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script

# Update database
dotnet ef database update

# Drop database
dotnet ef database drop
```

**Tooling Dependencies:**

The project includes **Microsoft.EntityFrameworkCore.Design**, which provides:
- Migration generation logic
- Database scaffolding capabilities
- Design-time service resolution
- PowerShell and CLI command support

**Troubleshooting Design-Time Issues:**

| Issue | Solution |
|-------|----------|
| "Unable to create an object of type 'MovieContext'" | Ensure `DesignTimeDbContextFactory` is in the same assembly as `MovieContext` |
| "No connection string found" | Verify `appsettings.json` exists in the project directory and contains the connection string |
| "Could not load assembly" | Check that all referenced packages are restored (`dotnet restore`) |
| "Multiple DbContext types found" | Specify the context explicitly: `dotnet ef migrations add <Name> --context MovieContext` |

### Development vs Production

The design-time factory is specifically for development tooling and should not be used in production runtime scenarios.

**Development Environment:**

- **Purpose**: Enable migration generation and database updates during development
- **Configuration Source**: Local `appsettings.json` file
- **Connection String**: Points to local development database
- **Security**: May use integrated authentication or development credentials

**Production Environment:**

- **Purpose**: Run the application with production database
- **Configuration Source**: Environment variables, Azure Key Vault, or secure configuration providers
- **Connection String**: Points to production database with restricted permissions
- **Security**: Uses managed identities or secure credential storage

**Runtime Context Configuration:**

In production, the `MovieContext` is configured through dependency injection in `Startup.cs` or `Program.cs`:

```csharp
services.AddDbContext<MovieContext>(options =>
{
    var connectionString = configuration.GetConnectionString("MoviesDatabase");
    options.UseSqlServer(connectionString);
    
    // Production-specific options
    options.EnableSensitiveDataLogging(false);
    options.EnableDetailedErrors(false);
});
```

For more information on Entity Framework Core configuration and usage patterns, see [/database/entity_framework.md](/database/entity_framework.md).

**Environment-Specific Configuration:**

Use different `appsettings.{Environment}.json` files:

```
appsettings.json                    # Base configuration
appsettings.Development.json        # Development overrides
appsettings.Staging.json           # Staging overrides
appsettings.Production.json        # Production overrides (typically not checked into source control)
```

The design-time factory will use the base `appsettings.json`, while the runtime application respects the `ASPNETCORE_ENVIRONMENT` variable to load environment-specific settings.

**Security Best Practices:**

- Never commit production connection strings to source control
- Use **User Secrets** for local development (see [/getting_started/installation.md](/getting_started/installation.md))
- Leverage **Azure.Identity** (version 1.14.2) for managed identity authentication in Azure environments
- Implement least-privilege database access with separate credentials for migrations vs. runtime operations
- Encrypt connection strings in configuration files when possible
- Rotate database credentials regularly and update configuration accordingly

---

## Related Documentation

- [Entity Framework Core Configuration](/database/entity_framework.md) - Detailed EF Core setup and usage patterns
- [Installation Guide](/getting_started/installation.md) - Initial project setup and configuration
- [Data Seeding Strategies](/database/seeding_data.md) - Approaches for populating initial and test data