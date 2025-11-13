# Utilities

## Overview

The BlackSlope project includes several development utilities designed to streamline common development tasks, provide command-line tooling, and offer reusable components across the application. These utilities range from bulk file renaming tools to Azure AD authentication helpers and custom extension methods that enhance developer productivity.

This documentation covers the purpose, usage, and implementation details of each utility component, along with best practices for creating and maintaining utilities within the project.

## RenameUtility

### Purpose and Usage

The **RenameUtility** is a console application built on .NET 6.0 that provides bulk renaming capabilities for project files, folders, and content. This utility is particularly useful when:

- Rebranding or renaming a project
- Creating new projects from existing templates
- Performing large-scale refactoring operations
- Standardizing naming conventions across a codebase

The utility performs three types of operations:
1. **File Content Replacement**: Searches and replaces text within files
2. **File Name Replacement**: Renames files containing the search term
3. **Folder Name Replacement**: Renames directories containing the search term

### Command-Line Interface

The RenameUtility can be invoked in two ways:

**Interactive Mode** (no arguments):
```bash
dotnet run -p ./src/RenameUtility/RenameUtility
```

The utility will prompt for:
- Project path
- Old name (search term)
- New name (replacement term)
- Confirmation before proceeding

**Command-Line Mode** (with arguments):
```bash
dotnet C:\Renamer\bin\Debug\netcoreapp2.0\Renamer.dll C:\blackslope.net BlackSlope BlockBuster
```

Or with dotnet run:
```bash
dotnet run -p ./src/RenameUtility/RenameUtility -- "C:\blackslope.net" "BlackSlope" "BlockBuster"
```

Arguments:
1. `projectPath`: Root directory to process
2. `oldName`: Text to search for
3. `newName`: Replacement text

### Configuration

#### IgnoreExtensions.RenameUtility File

The utility supports an ignore list to prevent processing certain file types. Create an `IgnoreExtensions.RenameUtility` file in the project root with comma-separated file extensions:

```
dll,exe,pdb
```

**Default Ignored Extensions:**
- `.dll` - Compiled assemblies
- `.exe` - Executable files
- `.pdb` - Debug symbol files

These binary files are excluded because:
- They cannot be safely text-processed
- They are build artifacts that should be regenerated
- Modifying them could corrupt the binaries

### Implementation Details

The core renaming logic is implemented in the `Renamer` method, which uses a recursive directory traversal pattern:

```csharp
private static void Renamer(string source, string search, string replace, ICollection<string> ignoreExts)
{
    var files = Directory.GetFiles(source);

    foreach (var filePath in files)
    {
        ReplaceFileText(search, replace, filePath, ignoreExts);

        var fileIdx = filePath.LastIndexOf('\\');

        if (fileIdx == -1) // is Linux machine
        {
            fileIdx = filePath.LastIndexOf('/');
        }

        var fileName = filePath.Substring(fileIdx);
        var ext = filePath.Split(".").Last();

        if (ignoreExts.Contains(ext) || !fileName.Contains(search)) continue;

        ReplaceFileName(search, replace, fileName, filePath, fileIdx);
    }

    var subdirectories = Directory.GetDirectories(source);
    foreach (var subdirectory in subdirectories)
    {
        Renamer(subdirectory, search, replace, ignoreExts);

        var folderNameIdx = subdirectory.LastIndexOf('\\') + 1;

        if (folderNameIdx == 0) // is Linux machine (LastIndexOf returns -1, +1 = 0)
        {
            folderNameIdx = subdirectory.LastIndexOf('/') + 1;
        }

        var folderName = subdirectory.Substring(folderNameIdx);

        if (!folderName.ToLower().Contains(search.ToLower())) continue;

        ReplaceFolderName(search, replace, subdirectory, folderNameIdx, folderName);
    }
}
```

**Key Design Decisions:**

1. **Cross-Platform Path Handling**: The utility detects the platform by checking for backslash (`\`) vs forward slash (`/`) separators, ensuring compatibility with both Windows and Linux environments.

2. **Recursive Processing**: Processes files before subdirectories to avoid path conflicts when renaming parent folders.

3. **Case-Insensitive Matching**: Folder name comparisons use `.ToLower()` for case-insensitive matching, while file content replacement preserves the original case.

4. **Error Handling**: Each operation is wrapped in try-catch blocks to prevent a single failure from halting the entire process.

### File Content Replacement

```csharp
private static void ReplaceFileText(string search, string replace, string filepath, ICollection<string> ignoreExts)
{
    var text = File.ReadAllText(filepath);
    var ext = filepath.Split(".").Last();
    if (ignoreExts.Contains(ext) || !text.Contains(search)) return;

    Console.WriteLine($"Replacing {search} with {replace} in file: {filepath}");

    text = text.Replace(search, replace);
    try
    {
        File.WriteAllText(filepath, text);
    }
    catch
    {
        Console.WriteLine($"ERROR - Failed to replace text in file: {filepath}.");
    }
}
```

**Important Considerations:**

- **Memory Usage**: The utility loads entire files into memory. For very large files (>100MB), this could cause performance issues.
- **Encoding Preservation**: Uses default encoding; special character encodings may not be preserved.
- **Binary File Detection**: Relies on extension checking rather than content analysis.

### File and Folder Renaming

The utility uses `File.Move()` and `Directory.Move()` for renaming operations:

```csharp
private static void ReplaceFileName(string search, string replace, string filename, string filepath, int fileindex)
{
    Console.WriteLine($"Replacing {search} with {replace} in file name: {filepath}");

    var startIndex = filename.IndexOf(search, StringComparison.OrdinalIgnoreCase);
    var endIndex = startIndex + search.Length;
    var newName = filename.Substring(0, startIndex);
    newName += replace;
    newName += filename.Substring(endIndex);

    var fileAddress = filepath.Substring(0, fileindex);
    fileAddress += newName;

    try
    {
        File.Move(filepath, fileAddress);
    }
    catch
    {
        Console.WriteLine($"ERROR - Failed to rename file: {filepath}.");
    }
}
```

**Potential Issues:**

- **File Locks**: Open files in IDEs or editors will cause rename failures
- **Case-Only Changes**: On case-insensitive file systems (Windows), renaming `File.cs` to `file.cs` may require a two-step process
- **Path Length Limits**: Windows has a 260-character path limit (unless long path support is enabled)

### Shell Script Integration

The `rename.sh` script provides a convenient wrapper for common renaming operations:

```bash
# Use: dotnet | Path to Rename Utility.dll | Path to Rename Directory | Search Name | Replace Name
dotnet run -p ./src/RenameUtility/RenameUtility -- "src" "BlackSlope" "Blockbuster"
```

**Usage Example:**

```bash
# Make the script executable (Linux/macOS)
chmod +x rename.sh

# Execute the rename operation
./rename.sh
```

## Console Host

### BlackSlope.Hosts.Console Project

The **BlackSlope.Hosts.Console** project provides a command-line interface for executing administrative tasks, background operations, and one-off scripts that require access to the application's core infrastructure.

**Project Structure:**
- **Output Type**: Console Application (Exe)
- **Framework**: .NET 6.0
- **SDK**: Microsoft.NET.Sdk

**Shared Infrastructure:**

The console host shares the following components with the web API:
- Azure AD authentication via `Azure.Identity` (1.14.2)
- SQL Server data access via `Microsoft.Data.SqlClient` (5.1.3)
- Entity Framework Core contexts
- Configuration management
- Dependency injection container

### Azure AD Authentication Helper

The console application includes an `AuthenticationToken` helper class for obtaining Azure AD tokens programmatically:

```csharp
using System;

namespace BlackSlope.Hosts.ConsoleApp
{
    static public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Welcome to the Blackslope Console");

            // execute program from here
            AuthenticationToken.GetAuthTokenAsync().Wait();

            Console.WriteLine("Press any button to continue...");
            Console.ReadLine();
        }
    }
}
```

**Authentication Flow:**

1. The `GetAuthTokenAsync()` method initiates the Azure AD authentication flow
2. Uses the `Azure.Identity` library for modern authentication patterns
3. Supports multiple credential types:
   - **DefaultAzureCredential**: Tries multiple authentication methods in sequence
   - **InteractiveBrowserCredential**: Opens browser for user authentication
   - **ClientSecretCredential**: Uses application credentials for service-to-service auth

**Configuration Requirements:**

The authentication helper requires the following configuration values (typically in `appsettings.json` or User Secrets):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

See [Authentication Documentation](/security/authentication.md) for detailed configuration instructions.

### Command-Line Operations

**Common Use Cases:**

1. **Database Migrations**: Execute Entity Framework Core migrations outside the web application lifecycle
2. **Data Seeding**: Populate initial or test data
3. **Batch Processing**: Process large datasets without HTTP timeout constraints
4. **Administrative Tasks**: User management, permission updates, data cleanup
5. **Integration Testing**: Validate external service integrations

**Example Implementation Pattern:**

```csharp
public static async Task Main(string[] args)
{
    // Build configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddUserSecrets<Program>()
        .Build();

    // Setup dependency injection
    var services = new ServiceCollection();
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
    
    var serviceProvider = services.BuildServiceProvider();

    // Execute operations
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Perform database operations
    await dbContext.Database.MigrateAsync();
    
    Console.WriteLine("Operations completed successfully.");
}
```

## Helper Scripts

### rename.sh Usage

The `rename.sh` script is a Bash wrapper for the RenameUtility that simplifies bulk renaming operations.

**Script Contents:**

```bash
# Use: dotnet | Path to Rename Utility.dll | Path to Rename Directory | Search Name | Replace Name
dotnet run -p ./src/RenameUtility/RenameUtility -- "src" "BlackSlope" "Blockbuster"
```

**Customization:**

To adapt the script for different renaming operations, modify the three parameters:

```bash
# Parameter 1: Target directory (relative or absolute path)
# Parameter 2: Search term (case-sensitive in file content)
# Parameter 3: Replacement term

# Example: Rename API-related files
dotnet run -p ./src/RenameUtility/RenameUtility -- "src/BlackSlope.Api" "Api" "WebApi"

# Example: Rename across entire solution
dotnet run -p ./src/RenameUtility/RenameUtility -- "." "OldCompany" "NewCompany"
```

**Best Practices:**

1. **Version Control**: Commit all changes before running the script
2. **Test Run**: Test on a copy of the project first
3. **Backup**: Create a backup before large-scale renames
4. **Review Changes**: Use `git diff` to review all modifications
5. **Incremental Approach**: Rename in stages rather than all at once

### run.sh Convenience Script

The `run.sh` script provides a quick way to start the web API application during development.

**Script Contents:**

```bash
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Usage:**

```bash
# Make executable (first time only)
chmod +x run.sh

# Run the API
./run.sh
```

**Advantages Over Direct dotnet run:**

1. **Consistent Path**: Always runs from the correct project directory
2. **Documentation**: Serves as documentation for the project structure
3. **Extensibility**: Can be enhanced with environment variables, build configurations, or pre-run checks

**Enhanced Version Example:**

```bash
#!/bin/bash

# Set environment
export ASPNETCORE_ENVIRONMENT=Development

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build project
echo "Building project..."
dotnet build --no-restore

# Run application
echo "Starting API..."
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj --no-build
```

See [Build Scripts Documentation](/development/build_scripts.md) for more automation examples.

## Custom Extensions

The project includes several custom extension methods that enhance the functionality of standard .NET types and simplify common operations.

### Enumeration Extensions

Extension methods for working with enumerations provide type-safe, readable code for enum operations.

**Common Patterns:**

```csharp
public static class EnumExtensions
{
    /// <summary>
    /// Gets the description attribute value from an enum member
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    /// <summary>
    /// Converts enum to a list of key-value pairs for dropdowns
    /// </summary>
    public static IEnumerable<KeyValuePair<int, string>> ToSelectList<TEnum>() 
        where TEnum : Enum
    {
        return Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(e => new KeyValuePair<int, string>(
                Convert.ToInt32(e), 
                e.GetDescription()));
    }

    /// <summary>
    /// Safely parses a string to an enum with a default fallback
    /// </summary>
    public static TEnum ParseOrDefault<TEnum>(this string value, TEnum defaultValue) 
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, true, out var result) 
            ? result 
            : defaultValue;
    }
}
```

**Usage Examples:**

```csharp
public enum OrderStatus
{
    [Description("Pending Approval")]
    Pending = 0,
    
    [Description("In Progress")]
    InProgress = 1,
    
    [Description("Completed Successfully")]
    Completed = 2,
    
    [Description("Cancelled by User")]
    Cancelled = 3
}

// Get friendly description
var status = OrderStatus.InProgress;
Console.WriteLine(status.GetDescription()); // Output: "In Progress"

// Create dropdown list
var statusList = EnumExtensions.ToSelectList<OrderStatus>();

// Safe parsing
var parsedStatus = "completed".ParseOrDefault(OrderStatus.Pending);
```

### Service Collection Extensions

Extension methods for `IServiceCollection` streamline dependency injection configuration and promote consistency across the application.

**Registration Patterns:**

```csharp
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services with their appropriate lifetimes
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Register scoped services (per-request lifetime)
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();

        // Register singleton services (application lifetime)
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        // Register transient services (per-resolution lifetime)
        services.AddTransient<IEmailService, EmailService>();

        return services;
    }

    /// <summary>
    /// Configures AutoMapper with all profiles in the assembly
    /// </summary>
    public static IServiceCollection AddAutoMapperProfiles(
        this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(Program).Assembly);
        });

        return services;
    }

    /// <summary>
    /// Configures FluentValidation with all validators in the assembly
    /// </summary>
    public static IServiceCollection AddFluentValidators(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
        return services;
    }

    /// <summary>
    /// Configures Polly resilience policies for HttpClient
    /// </summary>
    public static IServiceCollection AddResilientHttpClient(
        this IServiceCollection services,
        string clientName,
        string baseAddress)
    {
        services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
        .AddTransientHttpErrorPolicy(policy =>
            policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        return services;
    }
}
```

**Usage in Startup/Program.cs:**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Database
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

    // Application services
    services.AddApplicationServices();
    
    // AutoMapper
    services.AddAutoMapperProfiles();
    
    // FluentValidation
    services.AddFluentValidators();
    
    // Resilient HTTP clients
    services.AddResilientHttpClient("ExternalApi", "https://api.external.com");

    // Health checks
    services.AddHealthChecks()
        .AddSqlServer(Configuration.GetConnectionString("DefaultConnection"))
        .AddDbContextCheck<ApplicationDbContext>();
}
```

### Builder Extensions

Extension methods for application builders configure middleware pipelines and request processing.

**Middleware Configuration:**

```csharp
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures Swagger UI with custom settings
    /// </summary>
    public static IApplicationBuilder UseSwaggerDocumentation(
        this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackSlope API V1");
            options.RoutePrefix = "api-docs";
            options.DocumentTitle = "BlackSlope API Documentation";
            options.DisplayRequestDuration();
        });

        return app;
    }

    /// <summary>
    /// Configures global exception handling middleware
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        return app;
    }

    /// <summary>
    /// Configures health check endpoints
    /// </summary>
    public static IApplicationBuilder UseHealthCheckEndpoints(
        this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }
}
```

**Usage in Program.cs (.NET 6):**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddApplicationServices();
builder.Services.AddAutoMapperProfiles();

var app = builder.Build();

// Configure middleware pipeline
app.UseGlobalExceptionHandler(app.Environment);
app.UseSwaggerDocumentation();
app.UseHealthCheckEndpoints();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

## Best Practices

### When to Create Utilities

Create utility classes or methods when you encounter:

1. **Repeated Code Patterns**: Logic duplicated across 3+ locations
2. **Complex Operations**: Multi-step processes that obscure business logic
3. **Cross-Cutting Concerns**: Functionality needed across multiple layers
4. **Framework Limitations**: Gaps in standard library functionality
5. **Domain-Specific Operations**: Business logic that doesn't fit in a single service

**Anti-Patterns to Avoid:**

- **God Classes**: Utilities that do too many unrelated things
- **Premature Abstraction**: Creating utilities before patterns emerge
- **Leaky Abstractions**: Utilities that expose implementation details
- **Static Abuse**: Overusing static methods when dependency injection is more appropriate

### Reusable Component Design

**Principles for Utility Design:**

1. **Single Responsibility**: Each utility should have one clear purpose
2. **Immutability**: Prefer immutable operations that don't modify state
3. **Null Safety**: Handle null inputs gracefully with appropriate exceptions or defaults
4. **Performance**: Consider memory and CPU implications for frequently-called utilities
5. **Testability**: Design utilities to be easily unit tested

**Example: Well-Designed Utility Class**

```csharp
/// <summary>
/// Provides string manipulation utilities for business logic
/// </summary>
public static class StringUtilities
{
    /// <summary>
    /// Truncates a string to the specified length, adding ellipsis if truncated
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">Maximum length including ellipsis</param>
    /// <returns>Truncated string with ellipsis if needed</returns>
    /// <exception cref="ArgumentException">Thrown when maxLength is less than 3</exception>
    public static string Truncate(string value, int maxLength)
    {
        if (maxLength < 3)
            throw new ArgumentException("Max length must be at least 3", nameof(maxLength));

        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;

        return value.Substring(0, maxLength - 3) + "...";
    }

    /// <summary>
    /// Converts a string to title case, handling acronyms appropriately
    /// </summary>
    public static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(value.ToLower());
    }

    /// <summary>
    /// Removes all non-alphanumeric characters from a string
    /// </summary>
    public static string RemoveSpecialCharacters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return Regex.Replace(value, @"[^a-zA-Z0-9]", string.Empty);
    }
}
```

**Unit Test Example:**

```csharp
public class StringUtilitiesTests
{
    [Theory]
    [InlineData("Hello World", 10, "Hello W...")]
    [InlineData("Short", 10, "Short")]
    [InlineData(null, 10, "")]
    [InlineData("", 10, "")]
    public void Truncate_ReturnsExpectedResult(string input, int maxLength, string expected)
    {
        // Act
        var result = StringUtilities.Truncate(input, maxLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Truncate_ThrowsException_WhenMaxLengthTooSmall()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            StringUtilities.Truncate("test", 2));
    }
}
```

### Documentation

**Required Documentation Elements:**

1. **XML Documentation Comments**: For all public methods and classes
2. **Usage Examples**: In XML comments or separate example files
3. **Parameter Validation**: Document expected inputs and edge cases
4. **Exception Documentation**: List all exceptions that can be thrown
5. **Performance Notes**: Document complexity and resource usage for critical utilities

**Documentation Template:**

```csharp
/// <summary>
/// Brief description of what the utility does
/// </summary>
/// <remarks>
/// Detailed explanation of behavior, algorithms, or design decisions.
/// Include performance characteristics if relevant.
/// </remarks>
/// <param name="paramName">Description of parameter and valid values</param>
/// <returns>Description of return value</returns>
/// <exception cref="ExceptionType">When this exception is thrown</exception>
/// <example>
/// <code>
/// var result = UtilityClass.Method(parameter);
/// Console.WriteLine(result);
/// </code>
/// </example>
public static ReturnType Method(ParameterType paramName)
{
    // Implementation
}
```

**Maintenance Documentation:**

Create a `UTILITIES.md` file in the project root documenting:

- **Utility Inventory**: List of all utility classes and their purposes
- **Design Decisions**: Why certain approaches were chosen
- **Dependencies**: External libraries or frameworks used
- **Migration Notes**: Breaking changes or deprecations
- **Performance Benchmarks**: For performance-critical utilities

**Example UTILITIES.md Structure:**

```markdown
# Utility Components

## String Utilities
**Location**: `src/BlackSlope.Core/Utilities/StringUtilities.cs`
**Purpose**: Common string manipulation operations
**Dependencies**: System.Text.RegularExpressions

### Methods
- `Truncate(string, int)`: Truncates strings with ellipsis
- `ToTitleCase(string)`: Converts to title case
- `RemoveSpecialCharacters(string)`: Strips non-alphanumeric characters

## Date Utilities
**Location**: `src/BlackSlope.Core/Utilities/DateUtilities.cs`
**Purpose**: Business date calculations and formatting
**Dependencies**: NodaTime (for timezone handling)

### Methods
- `GetBusinessDays(DateTime, DateTime)`: Calculates business days between dates
- `ToIso8601(DateTime)`: Formats dates in ISO 8601 format
```

### Integration with Development Environment

Utilities should integrate seamlessly with the development workflow:

1. **IDE Support**: Ensure IntelliSense works correctly with XML documentation
2. **Code Analysis**: Configure StyleCop and .NET analyzers to enforce utility standards
3. **Build Integration**: Include utility projects in solution-wide builds
4. **Testing**: Maintain high test coverage (>80%) for utility code
5. **CI/CD**: Run utility tests in continuous integration pipelines

See [Environment Setup Documentation](/development/environment.md) for development environment configuration.

### Version Control Considerations

**Branching Strategy for Utilities:**

- **Feature Branches**: For new utility development
- **Hotfix Branches**: For critical utility bug fixes
- **Release Tags**: Tag versions when utilities have breaking changes

**Commit Message Guidelines:**

```
feat(utilities): Add string truncation utility
fix(utilities): Handle null input in DateUtilities.GetBusinessDays
refactor(utilities): Improve performance of EnumExtensions.ToSelectList
docs(utilities): Add XML documentation to StringUtilities
test(utilities): Add unit tests for edge cases in Truncate method
```

### Performance Optimization

**Benchmarking Utilities:**

Use BenchmarkDotNet for performance-critical utilities:

```csharp
[MemoryDiagnoser]
public class StringUtilitiesBenchmarks
{
    private const string TestString = "Lorem ipsum dolor sit amet...";

    [Benchmark]
    public string Truncate_Substring()
    {
        return StringUtilities.Truncate(TestString, 50);
    }

    [Benchmark]
    public string Truncate_Span()
    {
        return StringUtilities.TruncateWithSpan(TestString, 50);
    }
}
```

**Optimization Guidelines:**

1. **Avoid Allocations**: Use `Span<T>` and `Memory<T>` for string operations
2. **Cache Results**: Use `MemoryCache` for expensive computations
3. **Lazy Initialization**: Defer expensive operations until needed
4. **Async Operations**: Use async/await for I/O-bound utilities
5. **Parallel Processing**: Use `Parallel.ForEach` for CPU-bound batch operations

---

## Related Documentation

- [Environment Setup](/development/environment.md) - Development environment configuration
- [Authentication](/security/authentication.md) - Azure AD authentication setup
- [Build Scripts](/development/build_scripts.md) - Automation and build processes