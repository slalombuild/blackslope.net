# Code Quality

## Overview

BlackSlope.NET implements a comprehensive code quality strategy using multiple analyzers and configuration files to maintain consistent code style, enforce best practices, and ensure code cleanliness across the solution. The project leverages both **StyleCop.Analyzers** (version 1.1.118) and **Microsoft.CodeAnalysis.NetAnalyzers** (version 6.0.0) to provide real-time feedback during development and enforce coding standards at build time.

This documentation outlines the code quality tools, configurations, suppressed rules with their justifications, and best practices for maintaining code quality in the BlackSlope.NET project.

---

## Code Analyzers

### StyleCop Analyzers

StyleCop is a static code analysis tool that enforces a set of style and consistency rules for C# code. The project uses **StyleCop.Analyzers** version 1.1.118 to ensure consistent formatting and code organization.

**Configuration Location:**
- Project-level configuration: `stylecop.json` files in each project directory
- Global suppressions: `src/BlackSlope.Api.Common/GlobalSuppressions.cs`

**Key Features:**
- Real-time analysis during development
- IDE integration with Visual Studio and Visual Studio Code
- Automatic code fixes for many violations
- Customizable rule sets per project

### .NET Analyzers

The **Microsoft.CodeAnalysis.NetAnalyzers** package (version 6.0.0) provides IDE-level code analysis covering both style formatting and deeper code quality issues including security, performance, and design concerns.

**Configuration Location:**
- Solution/project-level configuration: `.editorconfig` file at the repository root
- Global suppressions: `src/BlackSlope.Api.Common/GlobalSuppressions.cs`

**Key Features:**
- Comprehensive code analysis (CA rules)
- Security vulnerability detection
- Performance optimization suggestions
- Design pattern enforcement
- API usage validation

### Configuration Hierarchy

The analyzers consume configuration from multiple sources with the following precedence:

1. **Global Suppressions** (`GlobalSuppressions.cs`) - Assembly-level suppressions
2. **EditorConfig** (`.editorconfig`) - Solution/project-level settings for .NET Analyzers
3. **StyleCop Configuration** (`stylecop.json`) - Project-level settings for StyleCop
4. **Default Rules** - Built-in analyzer defaults

---

## EditorConfig Setup

The `.editorconfig` file at the repository root defines coding conventions and analyzer settings for the entire solution.

### General Settings

```ini
# top-most EditorConfig file
root = true

# Don't use tabs for indentation.
[*]
indent_style = space

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
insert_final_newline = true
charset = utf-8-bom
```

**Key Conventions:**
- **Indentation:** Spaces only (no tabs)
- **Indent Size:** 4 spaces for C# files, 2 spaces for XML/JSON
- **Line Endings:** Final newline required in all code files
- **Character Encoding:** UTF-8 with BOM for C# files

### C# Code Style Settings

#### Type References

```ini
# Use language keywords instead of framework type names for type references
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
```

**Preferred:**
```csharp
int count = 10;
string name = "BlackSlope";
```

**Avoid:**
```csharp
Int32 count = 10;
String name = "BlackSlope";
```

#### Variable Declarations

```ini
# Prefer "var" everywhere
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
```

**Preferred:**
```csharp
var count = 10;
var movies = new List<Movie>();
var result = movieRepository.GetById(id);
```

#### Expression Bodies

```ini
# Prefer method-like constructs to have a block body
csharp_style_expression_bodied_methods = false:none
csharp_style_expression_bodied_constructors = false:none
csharp_style_expression_bodied_operators = false:none

# Prefer property-like constructs to have an expression-body
csharp_style_expression_bodied_properties = true:none
csharp_style_expression_bodied_indexers = true:none
csharp_style_expression_bodied_accessors = true:none
```

**Methods (Block Body Preferred):**
```csharp
public async Task<Movie> GetMovieAsync(int id)
{
    return await _context.Movies.FindAsync(id);
}
```

**Properties (Expression Body Preferred):**
```csharp
public string FullName => $"{FirstName} {LastName}";
public bool IsValid => !string.IsNullOrEmpty(Name);
```

#### Modern Language Features

```ini
# Suggest more modern language features when available
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:suggestion
```

**Examples:**

```csharp
// Object initializer
var movie = new Movie
{
    Title = "Inception",
    Year = 2010,
    Director = "Christopher Nolan"
};

// Null coalescing
var title = movie?.Title ?? "Unknown";

// Explicit tuple names
(string title, int year) movieInfo = ("Inception", 2010);
Console.WriteLine(movieInfo.title); // Preferred over movieInfo.Item1

// Pattern matching over 'is' with cast
if (obj is Movie movie)
{
    Console.WriteLine(movie.Title);
}

// Pattern matching over 'as' with null check
// Avoid: var movie = obj as Movie; if (movie != null) { ... }
// Preferred: if (obj is Movie movie) { ... }

// Inlined variable declaration
// Preferred: if (int.TryParse(input, out var value))
// Avoid: int value; if (int.TryParse(input, out value))

// Throw expression
public Movie(string title) => Title = title ?? throw new ArgumentNullException(nameof(title));

// Conditional delegate call
OnMovieAdded?.Invoke(movie); // Preferred over if (OnMovieAdded != null) OnMovieAdded(movie);
```

#### Newline Settings

```ini
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
```

**Enforced Style:**
```csharp
public class MovieService
{
    public async Task<Movie> GetMovieAsync(int id)
    {
        try
        {
            return await _repository.GetByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movie");
            throw;
        }
        finally
        {
            _logger.LogInformation("GetMovie operation completed");
        }
    }
}
```

### Specific Rule Configurations

```ini
# CA2007: Consider calling ConfigureAwait on the awaited task
dotnet_diagnostic.CA2007.severity = suggestion
```

This rule is set to `suggestion` rather than `warning` or `error`, allowing developers flexibility in ASP.NET Core contexts where `ConfigureAwait(false)` is often unnecessary.

---

## StyleCop Rules

### StyleCop Configuration

Both `BlackSlope.Api` and `BlackSlope.Api.Common` projects use identical `stylecop.json` configurations:

```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "orderingRules": {
      "usingDirectivesPlacement": "outsideNamespace"
    },
    "layoutRules": {
      "newlineAtEndOfFile": "require"
    },
    "documentationRules": {
      "documentExposedElements": false,
      "documentInternalElements": false
    }
  }
}
```

### Configuration Breakdown

#### Ordering Rules

**Using Directives Placement:**
```csharp
// Correct: Using directives outside namespace
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace BlackSlope.Api.Controllers
{
    public class MoviesController : ControllerBase
    {
        // Controller implementation
    }
}
```

#### Layout Rules

**Newline at End of File:**
Every source file must end with a newline character. This is automatically enforced by the analyzer and prevents issues with version control systems and text processing tools.

#### Documentation Rules

**Documentation Requirements:**
```json
"documentationRules": {
  "documentExposedElements": false,
  "documentInternalElements": false
}
```

Public and internal elements are currently **not required** to have XML documentation comments. This is a temporary configuration (see Suppressed Rules section).

### Enabled Rules

StyleCop enforces hundreds of rules by default. Key categories include:

#### Spacing Rules
- Proper spacing around operators, keywords, and symbols
- Consistent spacing in method calls and declarations
- No trailing whitespace

#### Readability Rules
- Meaningful variable and method names
- Proper use of parentheses for clarity
- Consistent use of string interpolation

#### Ordering Rules
- Using directives sorted alphabetically with System namespaces first
- Class members ordered by access level and type
- Consistent element ordering within classes

#### Naming Rules
- PascalCase for public members
- camelCase for parameters and local variables
- Meaningful, descriptive names

#### Maintainability Rules
- Appropriate file organization
- Single responsibility per file
- Proper access modifier usage

---

## Suppressed Rules and Rationale

The project maintains a centralized list of suppressed rules in `src/BlackSlope.Api.Common/GlobalSuppressions.cs`. Each suppression includes a clear justification.

### StyleCop Suppressions

```csharp
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", 
    Justification = "BlackSlope currently prefixes local class field names with underscores.")]
```

**SA1101: Prefix Local Calls With This**

**Rationale:** BlackSlope uses underscore notation for private fields (`_fieldName`), making the `this.` prefix redundant and verbose.

**Example:**
```csharp
public class MovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly ILogger<MovieService> _logger;

    public MovieService(IMovieRepository movieRepository, ILogger<MovieService> logger)
    {
        // Preferred: No 'this.' prefix needed
        _movieRepository = movieRepository;
        _logger = logger;
    }

    public async Task<Movie> GetMovieAsync(int id)
    {
        // Clear that _logger is a field due to underscore prefix
        _logger.LogInformation("Retrieving movie {MovieId}", id);
        return await _movieRepository.GetByIdAsync(id);
    }
}
```

---

```csharp
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", 
    Justification = "BlackSlope uses underscore notation to identify local class fields.")]
```

**SA1309: Field Names Must Not Begin With Underscore**

**Rationale:** The project follows the common C# convention of prefixing private fields with underscores to distinguish them from local variables and parameters.

**Example:**
```csharp
public class MovieValidator : AbstractValidator<Movie>
{
    private readonly IMovieRepository _repository;
    private readonly int _maxTitleLength = 200;

    public MovieValidator(IMovieRepository repository)
    {
        _repository = repository;

        RuleFor(m => m.Title)
            .NotEmpty()
            .MaximumLength(_maxTitleLength);
    }
}
```

---

```csharp
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1629:DocumentationTextMustEndWithAPeriod", 
    Justification = "Too pedantic.")]
```

**SA1629: Documentation Text Must End With A Period**

**Rationale:** Requiring periods at the end of all documentation comments is overly strict and doesn't significantly improve code quality.

---

```csharp
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:FileMustHaveHeader", 
    Justification = "Pending.")]
```

**SA1633: File Must Have Header**

**Rationale:** File headers (copyright notices, license information) are pending organizational decision. This suppression is temporary.

---

```csharp
// Remove this when ready to begin documenting
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", 
    Justification = "To be done later.")]
```

**SA1600: Elements Must Be Documented**

**Rationale:** **TEMPORARY SUPPRESSION** - XML documentation comments are planned but not yet implemented. This should be removed once the team begins comprehensive documentation efforts.

**Future Implementation:**
```csharp
/// <summary>
/// Retrieves a movie by its unique identifier
/// </summary>
/// <param name="id">The movie identifier</param>
/// <returns>The movie if found, null otherwise</returns>
public async Task<Movie> GetMovieAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}
```

---

```csharp
// Remove these when ready to document parameters and return statements
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1614:ElementParameterDocumentationMustHaveText", 
    Justification = "To be done later.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1616:ElementReturnValueDocumentationMustHaveText", 
    Justification = "To be done later.")]
```

**SA1614 & SA1616: Parameter and Return Value Documentation**

**Rationale:** **TEMPORARY SUPPRESSION** - These rules require meaningful text in `<param>` and `<returns>` tags. Suppressed until comprehensive documentation effort begins.

---

### Code Analysis (CA) Suppressions

```csharp
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", 
    Justification = "Global exception logger", 
    Scope = "member", 
    Target = "~M:BlackSlope.Api.Common.Middleware.ExceptionHandling.ExceptionHandlingMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
```

**CA1031: Do Not Catch General Exception Types**

**Rationale:** The `ExceptionHandlingMiddleware` is a global exception handler that must catch all exception types to log them and return appropriate HTTP responses. This is an intentional design decision for centralized error handling.

**Context:**
```csharp
public class ExceptionHandlingMiddleware
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) // Intentionally catching all exceptions
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

---

```csharp
[assembly: SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", 
    Justification = "Composite makes the intention clearer", 
    Scope = "type", 
    Target = "~T:BlackSlope.Api.Common.Validators.CompositeValidator`1")]
```

**CA1710: Identifiers Should Have Correct Suffix**

**Rationale:** The `CompositeValidator<T>` class name clearly communicates its purpose (composing multiple validators). Adding a suffix like `Collection` would reduce clarity.

**Context:**
```csharp
// Clear and intentional naming
public class CompositeValidator<T> : IValidator<T>
{
    private readonly IEnumerable<IValidator<T>> _validators;

    public CompositeValidator(IEnumerable<IValidator<T>> validators)
    {
        _validators = validators;
    }

    public ValidationResult Validate(T instance)
    {
        // Compose validation results from multiple validators
    }
}
```

---

```csharp
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", 
    Justification = "Class organization", 
    Scope = "member", 
    Target = "~M:BlackSlope.Api.Common.Exceptions.HandledException.#ctor(BlackSlope.Api.Common.Exceptions.ExceptionType,System.String,System.Net.HttpStatusCode,System.String)")]
```

**SA1201: Elements Should Appear In The Correct Order**

**Rationale:** The `HandledException` class uses a specific constructor ordering that improves readability and logical flow, even if it doesn't match StyleCop's default ordering rules.

---

## Code Analysis Rules

### CA Rule Categories

The .NET Analyzers enforce rules across multiple categories:

#### Design Rules (CA1xxx)
- API design best practices
- Inheritance and interface design
- Type design guidelines

#### Globalization Rules (CA2xxx)
- Culture-specific formatting
- String handling
- Localization support

#### Maintainability Rules (CA15xx)
- Code complexity metrics
- Maintainability index
- Cyclomatic complexity

#### Naming Rules (CA17xx)
- Identifier naming conventions
- Abbreviation usage
- Terminology consistency

#### Performance Rules (CA18xx)
- Memory allocation optimization
- String concatenation
- Collection usage

#### Reliability Rules (CA20xx)
- Exception handling
- Disposal patterns
- Thread safety

#### Security Rules (CA21xx-CA51xx)
- SQL injection prevention
- XSS protection
- Cryptography usage
- Authentication and authorization

#### Usage Rules (CA22xx)
- Correct API usage
- Framework guidelines
- Best practice enforcement

### Justification Documentation

All suppressed CA rules in `GlobalSuppressions.cs` include:

1. **Rule ID**: The specific CA rule being suppressed
2. **Justification**: Clear explanation of why the suppression is necessary
3. **Scope**: Whether the suppression applies to a member, type, or assembly
4. **Target**: The specific code element affected (using documentation ID format)

**Example Documentation:**
```csharp
[assembly: SuppressMessage(
    "Design",                    // Category
    "CA1031:Do not catch general exception types",  // Rule
    Justification = "Global exception logger",      // Why
    Scope = "member",                               // Where
    Target = "~M:BlackSlope.Api.Common.Middleware.ExceptionHandling.ExceptionHandlingMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
```

### Global Suppressions

Global suppressions are preferred over inline suppressions (`#pragma warning disable`) for several reasons:

1. **Centralized Management**: All suppressions in one location
2. **Documentation**: Requires explicit justification
3. **Visibility**: Easy to review and audit
4. **Maintainability**: Simpler to update or remove when no longer needed

---

## Best Practices

### Consistent Code Style

#### Naming Conventions

```csharp
// Public members: PascalCase
public class MovieService { }
public string Title { get; set; }
public async Task<Movie> GetMovieAsync(int id) { }

// Private fields: _camelCase with underscore prefix
private readonly IMovieRepository _movieRepository;
private readonly ILogger<MovieService> _logger;

// Parameters and local variables: camelCase
public MovieService(IMovieRepository movieRepository, ILogger<MovieService> logger)
{
    var validationResult = ValidateRepository(movieRepository);
}

// Constants: PascalCase
private const int MaxRetryAttempts = 3;
private const string DefaultConnectionString = "Server=localhost;";
```

#### File Organization

```csharp
// 1. Using directives (outside namespace, sorted)
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// 2. Namespace declaration
namespace BlackSlope.Api.Services
{
    // 3. Class declaration with XML comments (when implemented)
    public class MovieService
    {
        // 4. Private fields
        private readonly IMovieRepository _repository;
        private readonly ILogger<MovieService> _logger;

        // 5. Constructors
        public MovieService(IMovieRepository repository, ILogger<MovieService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // 6. Public properties
        public int MaxResults { get; set; } = 100;

        // 7. Public methods
        public async Task<IEnumerable<Movie>> GetAllMoviesAsync()
        {
            return await _repository.GetAllAsync();
        }

        // 8. Private methods
        private void ValidateMovie(Movie movie)
        {
            // Validation logic
        }
    }
}
```

### Code Review Standards

#### Pre-Commit Checklist

1. **Build Succeeds**: No compilation errors or analyzer warnings
2. **Tests Pass**: All unit and integration tests pass
3. **Analyzer Violations**: Address or justify any new violations
4. **Code Coverage**: Maintain or improve test coverage
5. **Documentation**: Update relevant documentation

#### Review Focus Areas

**Code Quality:**
- Adherence to naming conventions
- Proper use of async/await patterns
- Appropriate exception handling
- Dependency injection usage
- SOLID principles compliance

**Security:**
- Input validation
- SQL injection prevention
- Authentication/authorization checks
- Sensitive data handling

**Performance:**
- Efficient database queries
- Proper use of caching
- Async operations for I/O-bound work
- Memory allocation patterns

**Maintainability:**
- Clear, self-documenting code
- Appropriate abstraction levels
- Single responsibility principle
- Testability

### Refactoring Guidelines

#### When to Refactor

1. **Code Smells Detected:**
   - Long methods (>50 lines)
   - Large classes (>500 lines)
   - Duplicate code
   - Complex conditionals
   - High cyclomatic complexity

2. **Analyzer Suggestions:**
   - Performance warnings
   - Maintainability issues
   - Design pattern violations

3. **Technical Debt:**
   - Temporary suppressions that can be resolved
   - Outdated patterns or practices
   - Missing documentation

#### Refactoring Process

```csharp
// Before: Long method with multiple responsibilities
public async Task<ActionResult<Movie>> CreateMovie(MovieDto dto)
{
    // Validation
    if (string.IsNullOrEmpty(dto.Title))
        return BadRequest("Title is required");
    
    if (dto.Year < 1888 || dto.Year > DateTime.Now.Year)
        return BadRequest("Invalid year");
    
    // Check for duplicates
    var existing = await _context.Movies
        .FirstOrDefaultAsync(m => m.Title == dto.Title && m.Year == dto.Year);
    
    if (existing != null)
        return Conflict("Movie already exists");
    
    // Create entity
    var movie = new Movie
    {
        Title = dto.Title,
        Year = dto.Year,
        Director = dto.Director
    };
    
    // Save
    _context.Movies.Add(movie);
    await _context.SaveChangesAsync();
    
    // Log
    _logger.LogInformation("Created movie {Title}", movie.Title);
    
    return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
}

// After: Refactored with single responsibility
public async Task<ActionResult<Movie>> CreateMovie(MovieDto dto)
{
    var validationResult = await _validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
        return BadRequest(validationResult.Errors);
    
    var movie = _mapper.Map<Movie>(dto);
    
    var result = await _movieService.CreateMovieAsync(movie);
    
    return result.Match<ActionResult<Movie>>(
        success => CreatedAtAction(nameof(GetMovie), new { id = success.Id }, success),
        conflict => Conflict(conflict.Message),
        error => StatusCode(500, error.Message)
    );
}
```

#### Safe Refactoring Steps

1. **Ensure Test Coverage**: Write tests before refactoring
2. **Small Changes**: Make incremental, testable changes
3. **Run Tests Frequently**: After each small change
4. **Use IDE Tools**: Leverage automated refactoring tools
5. **Review Analyzer Feedback**: Address new violations immediately
6. **Commit Frequently**: Small, focused commits

---

## Tools

### Running Analyzers

#### Command Line

**Build with analyzer output:**
```bash
dotnet build /p:TreatWarningsAsErrors=true
```

**Analyze specific project:**
```bash
dotnet build src/BlackSlope.Api/BlackSlope.Api.csproj
```

**Generate analyzer report:**
```bash
dotnet build /p:RunAnalyzersDuringBuild=true /p:AnalysisLevel=latest
```

#### Visual Studio

1. **Build Menu** → **Build Solution** (Ctrl+Shift+B)
2. **View** → **Error List** to see analyzer warnings
3. **Analyze** → **Run Code Analysis** → **On Solution**

#### Visual Studio Code

1. Install **C# extension** (OmniSharp)
2. Analyzers run automatically on save
3. View problems in **Problems panel** (Ctrl+Shift+M)

### Fixing Violations

#### Automatic Fixes

Many analyzer violations can be fixed automatically:

**Visual Studio:**
1. Click on the lightbulb icon next to the violation
2. Select **Fix all occurrences in [scope]**
3. Review and apply changes

**Visual Studio Code:**
1. Click on the code with the squiggly underline
2. Press **Ctrl+.** (Quick Fix)
3. Select the appropriate fix

#### Bulk Fixes

**Format entire solution:**
```bash
dotnet format
```

**Format with analyzer fixes:**
```bash
dotnet format --verify-no-changes --severity info
```

**Fix specific analyzer:**
```bash
dotnet format --diagnostics SA1309
```

### IDE Integration

#### Visual Studio Configuration

**Enable StyleCop:**
1. **Tools** → **Options** → **Text Editor** → **C#** → **Code Style**
2. Ensure **Enable StyleCop Analyzers** is checked
3. Set **Run code analysis on build** to **All projects**

**Configure Severity:**
1. Right-click on analyzer warning in Error List
2. **Set Severity** → Choose level (None, Suggestion, Warning, Error)
3. Updates `.editorconfig` automatically

#### Visual Studio Code Configuration

**settings.json:**
```json
{
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll": true
  }
}
```

#### Rider Configuration

1. **Settings** → **Editor** → **Inspection Settings**
2. Enable **Solution-wide analysis**
3. **Code Inspection** → **C#** → Configure severity levels
4. **Code Style** → Import from `.editorconfig`

### Continuous Integration

#### Build Pipeline Configuration

**Azure DevOps (azure-pipelines.yml):**
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Build with Code Analysis'
  inputs:
    command: 'build'
    arguments: '/p:TreatWarningsAsErrors=true /p:RunAnalyzersDuringBuild=true'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Run Code Analysis'
  inputs:
    command: 'custom'
    custom: 'format'
    arguments: '--verify-no-changes --severity warning'
```

**GitHub Actions (.github/workflows/build.yml):**
```yaml
- name: Build with analyzers
  run: dotnet build --configuration Release /p:TreatWarningsAsErrors=true

- name: Check code formatting
  run: dotnet format --verify-no-changes --severity warning
```

### Suppression Management

#### Adding Suppressions

**Inline (not recommended):**
```csharp
#pragma warning disable CA1031 // Do not catch general exception types
try
{
    // Code that needs suppression
}
catch (Exception ex)
{
    // Handle exception
}
#pragma warning restore CA1031
```

**Global (recommended):**
```csharp
// In GlobalSuppressions.cs
[assembly: SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Specific reason for suppression",
    Scope = "member",
    Target = "~M:Namespace.Class.Method")]
```

#### Reviewing Suppressions

**Periodic Review Checklist:**
1. Review all suppressions in `GlobalSuppressions.cs`
2. Verify justifications are still valid
3. Remove temporary suppressions (SA1600, SA1614, SA1616)
4. Update documentation for remaining suppressions
5. Consider if violations can now be fixed

**Recommended Schedule:**
- **Monthly**: Quick review of new suppressions
- **Quarterly**: Comprehensive review of all suppressions
- **Major Releases**: Full audit and cleanup

---

## Related Documentation

For additional information on development practices and environment setup, see:

- [Development Environment Setup](/development/environment.md) - IDE configuration, tool installation, and local development setup
- [Contributing Guidelines](/development/contributing.md) - Code contribution process, pull request standards, and team workflows

---

## Summary

BlackSlope.NET maintains high code quality through:

1. **Dual Analyzer Strategy**: StyleCop for style consistency, .NET Analyzers for code quality
2. **Centralized Configuration**: `.editorconfig` and `stylecop.json` for consistent settings
3. **Documented Suppressions**: All rule suppressions include clear justifications
4. **Automated Enforcement**: IDE integration and CI/CD pipeline checks
5. **Continuous Improvement**: Regular review and removal of temporary suppressions

By following these guidelines and leveraging the configured tools, the development team can maintain a clean, consistent, and high-quality codebase that is easy to understand, maintain, and extend.