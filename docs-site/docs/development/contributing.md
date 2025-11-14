# Contributing Guidelines

This document provides comprehensive guidelines for contributing to the BlackSlope.NET reference architecture. BlackSlope is a .NET 6.0-based reference architecture designed to demonstrate best practices for building enterprise-grade web APIs with robust authentication, data access, and resilience patterns.

## Getting Started

### Forking the Repository

1. **Fork the Repository**: Navigate to the BlackSlope.NET repository and create a fork to your personal GitHub account.

2. **Clone Your Fork**: Clone your forked repository to your local development machine:
   ```bash
   git clone https://github.com/YOUR_USERNAME/BlackSlope.NET.git
   cd BlackSlope.NET
   ```

3. **Add Upstream Remote**: Configure the original repository as an upstream remote to keep your fork synchronized:
   ```bash
   git remote add upstream https://github.com/ORIGINAL_OWNER/BlackSlope.NET.git
   git fetch upstream
   ```

### Setting Up Development Environment

#### Prerequisites

Before setting up the development environment, ensure you have the following installed:

- **.NET 6.0 SDK**: Download and install from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- **SQL Server Developer 2019**: Download from [Microsoft SQL Server Downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **Docker Desktop** (optional, for containerized development): [Docker Desktop](https://www.docker.com/products/docker-desktop)
- **Visual Studio 2022** or **Visual Studio Code** with C# extensions
- **Git** for version control

#### Environment Configuration

1. **Install .NET Entity Framework Tools**:
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. **Database Setup**:
   
   Create a SQL Server database for the Movies application:
   
   ```sql
   CREATE DATABASE Movies;
   ```

3. **Configure Connection Strings**:
   
   Update the `appsettings.json` file in `src/BlackSlope.Api/` with your SQL Server connection details:
   
   ```json
   {
     "ConnectionStrings": {
       "MoviesConnectionString": "Server=YOUR_SERVER_NAME;Database=Movies;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

   For local development with sensitive credentials, use **User Secrets**:
   
   ```bash
   cd src/BlackSlope.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:MoviesConnectionString" "Server=YOUR_SERVER;Database=Movies;User Id=YOUR_USER;Password=YOUR_PASSWORD"
   ```

4. **Apply Database Migrations**:
   
   From the repository root directory:
   
   ```bash
   dotnet ef database update --project=.\src\BlackSlope.Api\BlackSlope.Api.csproj
   ```

   Expected output:
   ```
   Build started...
   Build succeeded.
   Applying migration '20190814225754_initialized'.
   Applying migration '20190814225910_seeded'.
   Done.
   ```

5. **Build the Solution**:
   ```bash
   dotnet build src/BlackSlope.NET.sln
   ```

6. **Verify Installation**:
   ```bash
   dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
   ```
   
   Navigate to `http://localhost:51385/swagger` to verify the API is running correctly.

#### Docker Setup (Optional)

For containerized development:

1. **Build Docker Image**:
   ```bash
   cd src
   docker build -t blackslope.api -f Dockerfile .
   ```

2. **Create and Start Container**:
   ```bash
   docker create --name blackslope-container blackslope.api
   docker start blackslope-container
   ```

3. **Verify Container Status**:
   ```bash
   docker ps
   ```

### Running Tests

Execute the full test suite to ensure your environment is correctly configured:

```bash
dotnet test ./src/
```

**Test Framework**: The project uses **xUnit** (version 2.4.1) as its testing framework, with Visual Studio Test SDK integration.

**Note**: Integration test projects using SpecFlow have been removed from the solution until SpecFlow adds support for .NET 6. Unit tests remain fully functional.

#### Integration Tests (Currently Unavailable)

BlackSlope provides two SpecFlow-driven integration test projects for Quality Engineers (QE):

- **BlackSlope.Api.Tests.IntegrationTests**: Using `System.Net.Http.HttpClient` implementation
- **BlackSlope.Api.Tests.RestSharpIntegrationTests**: Using RestSharp HttpClient implementation

**To Setup (when available)**:

1. Ensure you've successfully completed the database setup and application build steps
2. Update the `appsettings.test.json` file in your integration test project with the proper DB connection string and host URL for the BlackSlope API
   - **Note**: The BlackSlope API can be run on a localhost configuration if desired, but needs to be done so in a separate instance of your IDE to allow tests to run
3. Download the appropriate [SpecFlow plugins](https://docs.specflow.org/projects/specflow/en/latest/Installation/Installation.html) for your IDE

These can be executed in Test Explorer like regular unit tests, and teams can choose which implementation best suits their project needs.

For detailed testing guidelines, refer to [Testing Overview](/testing/overview.md).

## Development Workflow

### Files to Never Commit

The project's `.gitignore` file automatically excludes the following from version control:

**Build Artifacts**:
- Compiled binaries (`.dll`, `.exe`, `.pdb`)
- Build output directories (`bin/`, `obj/`)
- Package files (`.nupkg`)

**IDE and Tool Files**:
- Visual Studio user settings (`.suo`, `.user`, `.cache`)
- ReSharper files (`_ReSharper.*`, `*.ReSharper.*`)
- VS Code settings (`.vscode/`)
- Test results (`TestResults/`, `.ncrunchsolution`, `.dotCover`)

**Logs and Temporary Files**:
- Log files (`*.log`, `Logs/`)
- OS-generated files (`.DS_Store`, `Thumbs.db`)

**Documentation Build**:
- DocFX generated files (`_site/`, `.manifest`, `api/*.yml`)

**Important**: Always review changes before committing to ensure no sensitive data, user-specific configuration, or build artifacts are included. Use `git status` and `git diff` to verify staged changes.

### Branch Naming Conventions

Follow these branch naming conventions to maintain consistency and clarity:

| Branch Type | Pattern | Example | Purpose |
|-------------|---------|---------|---------|
| Feature | `feature/short-description` | `feature/add-movie-rating` | New features or enhancements |
| Bug Fix | `bugfix/issue-number-description` | `bugfix/123-fix-null-reference` | Bug fixes |
| Hotfix | `hotfix/critical-issue` | `hotfix/security-vulnerability` | Critical production fixes |
| Refactor | `refactor/component-name` | `refactor/movie-repository` | Code refactoring without functional changes |
| Documentation | `docs/topic` | `docs/contributing-guide` | Documentation updates |
| Chore | `chore/task-description` | `chore/update-dependencies` | Maintenance tasks |

**Branch Workflow**:

1. **Create Feature Branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Keep Branch Updated**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

3. **Push to Your Fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

### Commit Message Format

BlackSlope follows the **Conventional Commits** specification for clear, structured commit messages:

```
<type>(<scope>): <subject>

<body>

<footer>
```

#### Commit Types

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style changes (formatting, missing semicolons, etc.)
- **refactor**: Code refactoring without functional changes
- **perf**: Performance improvements
- **test**: Adding or updating tests
- **chore**: Maintenance tasks, dependency updates
- **ci**: CI/CD configuration changes

#### Examples

```bash
# Feature commit
git commit -m "feat(api): add movie rating endpoint

Implements GET /api/movies/{id}/rating endpoint with caching support.
Includes validation for rating range 1-10.

Closes #42"

# Bug fix commit
git commit -m "fix(repository): resolve null reference in GetMovieById

Added null check before accessing movie properties.
Updated unit tests to cover null scenarios."

# Documentation commit
git commit -m "docs(contributing): update branch naming conventions"
```

### Pull Request Process

#### Before Creating a Pull Request

1. **Ensure Code Quality**:
   - All tests pass: `dotnet test ./src/`
   - Code builds without warnings: `dotnet build src/BlackSlope.NET.sln`
   - StyleCop and NetAnalyzers rules are satisfied (see [Code Standards](#code-standards))

2. **Update Documentation**:
   - Update relevant documentation in `/docs` if applicable
   - Add XML documentation comments for public APIs
   - Update README.md if introducing new features or dependencies

3. **Rebase on Latest Main**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   git push origin feature/your-feature-name --force-with-lease
   ```

#### Creating the Pull Request

1. **Navigate to Your Fork** on GitHub and click "New Pull Request"

2. **Fill Out PR Template**:
   ```markdown
   ## Description
   Brief description of changes and motivation.

   ## Type of Change
   - [ ] Bug fix (non-breaking change which fixes an issue)
   - [ ] New feature (non-breaking change which adds functionality)
   - [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
   - [ ] Documentation update

   ## Testing
   - [ ] Unit tests added/updated
   - [ ] Integration tests added/updated (if applicable)
   - [ ] Manual testing performed

   ## Checklist
   - [ ] Code follows project style guidelines
   - [ ] Self-review completed
   - [ ] Documentation updated
   - [ ] No new warnings introduced
   - [ ] Tests pass locally

   ## Related Issues
   Closes #issue_number
   ```

3. **Request Reviewers**: Tag appropriate team members for review

4. **Link Related Issues**: Reference any related issues using GitHub keywords (Closes, Fixes, Resolves)

## Code Standards

BlackSlope enforces code quality through **StyleCop.Analyzers** and **Microsoft.CodeAnalysis.NetAnalyzers**. Understanding and adhering to these standards is critical for maintaining code consistency.

### Code Style Configuration

#### StyleCop Configuration

StyleCop rules are configured via `stylecop.json` at the project level:

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

**Key StyleCop Rules**:

- **Using Directives**: Must be placed outside namespaces
- **File Layout**: Files must end with a newline character
- **Documentation**: Public elements should be documented (though currently relaxed)

#### EditorConfig Configuration

The `.editorconfig` file defines cross-editor coding standards:

```ini
# General settings
[*]
indent_style = space

# Code files
[*.{cs,csx,vb,vbx}]
indent_size = 4
insert_final_newline = true
charset = utf-8-bom

# Xml project files
[*.{csproj,vbproj,vcxproj,vcxproj.filters,proj,projitems,shproj}]
indent_size = 2

# Xml config files
[*.{props,targets,ruleset,config,nuspec,resx,vsixmanifest,vsct}]
indent_size = 2

# JSON files
[*.json]
indent_size = 2

# Dotnet code style settings
[*.{cs,vb}]
dotnet_sort_system_directives_first = true
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion

# CSharp code style settings
[*.cs]
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
csharp_style_expression_bodied_methods = false:none
csharp_style_expression_bodied_constructors = false:none
csharp_style_expression_bodied_operators = false:none
csharp_style_expression_bodied_properties = true:none
csharp_style_expression_bodied_indexers = true:none
csharp_style_expression_bodied_accessors = true:none
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:suggestion
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true

# Code Analysis
dotnet_diagnostic.CA2007.severity = suggestion
```

**Key Conventions**:

- **Indentation**: Spaces (not tabs); 4 spaces for C# files, 2 spaces for JSON/XML/project files
- **Encoding**: UTF-8 with BOM for code files
- **Var Usage**: Prefer `var` for built-in types, when type is apparent, and elsewhere
- **Qualification**: Avoid `this.` and `Me.` for fields, properties, methods, and events
- **Type References**: Use language keywords (e.g., `int`, `string`) instead of framework types (e.g., `Int32`, `String`)
- **Modern Features**: Prefer object initializers, collection initializers, coalesce expressions, null propagation, explicit tuple names
- **Expression Bodies**: Use for properties, indexers, and accessors; avoid for methods, constructors, and operators
- **Pattern Matching**: Prefer pattern matching over `is` with cast checks and `as` with null checks
- **Braces**: Opening braces on new lines (Allman style)
- **Newlines**: New lines before `else`, `catch`, `finally`, and members in object initializers/anonymous types
- **System Directives**: `System.*` namespaces appear first in using statements
- **CA2007**: ConfigureAwait warnings shown as suggestions

### Suppressed Rules

Certain rules are globally suppressed in `BlackSlope.Api.Common.GlobalSuppressions` to balance code quality with pragmatism:

#### StyleCop Suppressions

| Rule ID | Rule Title | Rationale |
|---------|------------|-----------|
| SA1101 | Prefix local calls with this | Reduces verbosity; modern C# convention |
| SA1309 | Field names should not begin with underscore | Conflicts with common private field naming |
| SA1600 | Elements should be documented | Documentation enforced selectively |
| SA1614 | Element parameter documentation must have text | Reduces documentation burden |
| SA1616 | Element return value documentation must have text | Reduces documentation burden |
| SA1629 | Documentation text should end with a period | Minor formatting preference |
| SA1633 | File should have header | Not required for this project |

#### CodeAnalysis Suppressions

| Rule ID | Rule Title | Scope | Rationale |
|---------|------------|-------|-----------|
| CA1031 | Do not catch general exception types | `ExceptionHandlingMiddleware.Invoke` | Middleware must catch all exceptions for proper error handling |
| CA1710 | Identifiers should have correct suffix | `CompositeValidator<T>` | Naming convention preference |

### Writing Tests

All new features and bug fixes must include appropriate test coverage. For detailed testing practices, see [Testing Overview](/testing/overview.md).

#### Unit Test Structure

```csharp
using Xunit;
using Moq;
using AutoFixture;
using BlackSlope.Api.Services;

namespace BlackSlope.Api.Tests.Services
{
    public class MovieServiceTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IMovieRepository> _mockRepository;
        private readonly MovieService _sut; // System Under Test

        public MovieServiceTests()
        {
            _fixture = new Fixture();
            _mockRepository = new Mock<IMovieRepository>();
            _sut = new MovieService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetMovieById_WithValidId_ReturnsMovie()
        {
            // Arrange
            var expectedMovie = _fixture.Create<Movie>();
            _mockRepository
                .Setup(x => x.GetByIdAsync(expectedMovie.Id))
                .ReturnsAsync(expectedMovie);

            // Act
            var result = await _sut.GetMovieByIdAsync(expectedMovie.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedMovie.Id, result.Id);
            _mockRepository.Verify(x => x.GetByIdAsync(expectedMovie.Id), Times.Once);
        }

        [Fact]
        public async Task GetMovieById_WithInvalidId_ThrowsNotFoundException()
        {
            // Arrange
            var invalidId = _fixture.Create<int>();
            _mockRepository
                .Setup(x => x.GetByIdAsync(invalidId))
                .ReturnsAsync((Movie)null);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _sut.GetMovieByIdAsync(invalidId)
            );
        }
    }
}
```

**Testing Best Practices**:

- Use **AutoFixture** (version 4.17.0) for test data generation
- Use **Moq** (version 4.16.1) for mocking dependencies
- Follow **AAA pattern** (Arrange, Act, Assert)
- Use **xUnit** attributes (`[Fact]`, `[Theory]`) for test methods
- One assertion per test (when practical)
- Test both happy path and error scenarios
- Verify mock interactions when testing side effects

#### Test Coverage Requirements

- **Minimum Coverage**: 80% code coverage for new code
- **Critical Paths**: 100% coverage for authentication, authorization, and data access layers
- **Edge Cases**: Test null inputs, boundary conditions, and exception scenarios

### Documentation Requirements

#### XML Documentation Comments

All public APIs must include XML documentation:

```csharp
/// <summary>
/// Retrieves a movie by its unique identifier.
/// </summary>
/// <param name="id">The unique identifier of the movie.</param>
/// <returns>A <see cref="MovieDto"/> representing the movie, or null if not found.</returns>
/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is less than or equal to zero.</exception>
public async Task<MovieDto> GetMovieByIdAsync(int id)
{
    if (id <= 0)
    {
        throw new ArgumentException("Movie ID must be greater than zero.", nameof(id));
    }

    var movie = await _repository.GetByIdAsync(id);
    return _mapper.Map<MovieDto>(movie);
}
```

#### README Updates

When adding new features or dependencies:

1. Update the **Technology Versions** table with new package versions
2. Add setup instructions for new infrastructure requirements
3. Document new configuration settings in `appsettings.json`
4. Update the **What is it?** section if the feature significantly changes the architecture

#### Code Comments

Use inline comments sparingly and only when:

- Explaining complex algorithms or business logic
- Documenting workarounds for known issues
- Clarifying non-obvious design decisions

```csharp
// Polly retry policy: 3 attempts with exponential backoff
// Handles transient HTTP errors (5xx, 408) from external API
services.AddHttpClient<IFakeApiRepository, FakeApiRepository>()
    .AddPolicyHandler(GetRetryPolicy());
```

For more information on code quality standards, see [Code Quality Documentation](/development/code_quality.md).

## Review Process

### Code Review Checklist

Reviewers should verify the following before approving a pull request:

#### Functionality
- [ ] Code implements the stated requirements
- [ ] Edge cases are handled appropriately
- [ ] Error handling is comprehensive and appropriate
- [ ] No obvious bugs or logical errors

#### Code Quality
- [ ] Code follows project style guidelines (StyleCop/NetAnalyzers)
- [ ] No code duplication; DRY principle followed
- [ ] Appropriate design patterns used
- [ ] SOLID principles respected
- [ ] Dependency injection used correctly

#### Testing
- [ ] Unit tests cover new/modified code
- [ ] Tests are meaningful and test actual behavior
- [ ] Tests follow AAA pattern
- [ ] Mock usage is appropriate
- [ ] All tests pass

#### Security
- [ ] No sensitive data (passwords, keys) in code
- [ ] Input validation implemented
- [ ] SQL injection risks mitigated (parameterized queries)
- [ ] Authentication/authorization properly enforced

#### Performance
- [ ] No obvious performance issues (N+1 queries, unnecessary loops)
- [ ] Caching used appropriately
- [ ] Async/await used correctly
- [ ] Database queries optimized

#### Documentation
- [ ] XML comments for public APIs
- [ ] README updated if necessary
- [ ] Complex logic explained with comments
- [ ] Migration guide provided for breaking changes

### Addressing Feedback

When receiving review feedback:

1. **Respond Promptly**: Acknowledge feedback within 24 hours

2. **Ask Questions**: If feedback is unclear, ask for clarification rather than guessing

3. **Make Changes**: Address all feedback or provide rationale for not addressing it

4. **Update PR**: Push changes to the same branch:
   ```bash
   git add .
   git commit -m "refactor: address PR feedback"
   git push origin feature/your-feature-name
   ```

5. **Mark Conversations Resolved**: Once feedback is addressed, mark the conversation as resolved in GitHub

6. **Request Re-review**: After addressing all feedback, request a re-review from the original reviewers

### Merge Requirements

Pull requests can be merged when:

1. **All Checks Pass**:
   - Build succeeds
   - All tests pass
   - Code analysis produces no errors

2. **Approvals Received**:
   - At least 2 approvals from team members
   - No unresolved change requests

3. **Branch Updated**:
   - Branch is up-to-date with main
   - No merge conflicts

4. **Documentation Complete**:
   - All required documentation updated
   - PR description complete

**Merge Strategy**: Use **Squash and Merge** to maintain a clean commit history on the main branch.

## Best Practices

### Small, Focused Changes

**Principle**: Each pull request should address a single concern.

**Benefits**:
- Easier to review
- Faster to merge
- Simpler to revert if issues arise
- Reduces merge conflicts

**Guidelines**:
- Limit PRs to ~300 lines of code changes (excluding tests)
- If a feature requires more, break it into multiple PRs
- Use feature flags for incomplete features that need to be merged

**Example - Breaking Down a Large Feature**:

Instead of one large PR:
```
PR #1: Add movie rating feature (1200 lines)
  - Database migration
  - Repository changes
  - Service layer
  - API endpoints
  - Validation
  - Tests
```

Break into smaller PRs:
```
PR #1: Add movie rating database schema (150 lines)
PR #2: Add movie rating repository (200 lines)
PR #3: Add movie rating service layer (250 lines)
PR #4: Add movie rating API endpoints (300 lines)
PR #5: Add movie rating validation (150 lines)
```

### Clear Communication

**In Pull Requests**:
- Write descriptive PR titles and descriptions
- Explain the "why" behind changes, not just the "what"
- Include screenshots for UI changes
- Link to relevant issues, documentation, or design documents

**In Code Reviews**:
- Be respectful and constructive
- Explain the reasoning behind suggestions
- Distinguish between blocking issues and suggestions
- Use GitHub's suggestion feature for small changes

**In Commits**:
- Write clear, descriptive commit messages
- Reference issue numbers
- Explain non-obvious changes in commit body

### Testing Thoroughly

**Before Submitting PR**:

1. **Run Full Test Suite**:
   ```bash
   dotnet test ./src/
   ```

2. **Test Locally**:
   - Run the application locally
   - Test all affected endpoints using Swagger UI
   - Verify database migrations apply cleanly

3. **Test Edge Cases**:
   - Null inputs
   - Empty collections
   - Boundary values
   - Concurrent requests (if applicable)

4. **Performance Testing** (for performance-sensitive changes):
   - Profile code with performance tools
   - Compare before/after metrics
   - Test with realistic data volumes

**Integration Testing Considerations**:

While SpecFlow integration tests are temporarily unavailable for .NET 6, consider:

- Manual integration testing using Swagger UI
- Postman collections for API testing
- Database state verification after operations

### Working with Dependencies

#### Adding New NuGet Packages

1. **Evaluate Necessity**: Ensure the package is truly needed and not duplicating existing functionality

2. **Check Compatibility**: Verify .NET 6.0 compatibility

3. **Review Security**: Check for known vulnerabilities using:
   ```bash
   dotnet list package --vulnerable
   ```

4. **Add to Appropriate Project**:
   ```bash
   dotnet add src/BlackSlope.Api/BlackSlope.Api.csproj package PackageName
   ```

5. **Update Documentation**: Add to the Technology Versions table in README.md

6. **Verify `.gitignore`**: Ensure only project files are committed, not package binaries or build artifacts

7. **Commit Package Changes**:
   ```bash
   git add src/BlackSlope.Api/BlackSlope.Api.csproj
   git commit -m "chore: add PackageName dependency"
   ```

#### Updating Existing Packages

1. **Check for Updates**:
   ```bash
   dotnet list package --outdated
   ```

2. **Update Package**:
   ```bash
   dotnet add package PackageName --version X.Y.Z
   ```

3. **Test Thoroughly**: Ensure no breaking changes affect functionality

4. **Update Documentation**: Reflect version changes in README.md

### Working with Entity Framework Core

#### Creating Migrations

1. **Make Model Changes**: Update entity classes in the domain model

2. **Add Migration**:
   ```bash
   dotnet ef migrations add MigrationName --project src/BlackSlope.Api/BlackSlope.Api.csproj
   ```

3. **Review Generated Migration**: Inspect the migration file for correctness

4. **Apply Migration Locally**:
   ```bash
   dotnet ef database update --project src/BlackSlope.Api/BlackSlope.Api.csproj
   ```

5. **Test Migration**: Verify database schema and data integrity

6. **Include in PR**: Commit migration files with your changes

#### Migration Best Practices

- **Descriptive Names**: Use clear, descriptive migration names (e.g., `AddMovieRatingColumn`)
- **Data Migrations**: Include data migration logic when schema changes affect existing data
- **Rollback Testing**: Test that migrations can be rolled back cleanly
- **Production Considerations**: Consider downtime and data volume for production migrations

### Implementing Resilience with Polly

BlackSlope uses **Polly** for implementing resilience patterns in HTTP clients. When adding external API integrations:

```csharp
// In Startup.cs or Program.cs
services.AddHttpClient<IExternalApiClient, ExternalApiClient>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
```

**Resilience Best Practices**:
- Use retry policies for transient failures
- Implement circuit breakers to prevent cascading failures
- Configure timeouts appropriate to the external service SLA
- Log retry attempts and circuit breaker state changes
- Test failure scenarios in development

For more details, see:
- [Using HttpClientFactory to Implement Resilient HTTP Requests](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
- [Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)

### Azure Integration

BlackSlope is designed for Azure deployment. When working with Azure services:

#### Authentication with Azure Identity

Use **Azure.Identity** for modern Azure AD authentication:

```csharp
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

// In Startup.cs
var keyVaultUrl = new Uri(configuration["KeyVault:Url"]);
var client = new SecretClient(keyVaultUrl, new DefaultAzureCredential());
```

**DefaultAzureCredential** provides a seamless authentication experience across:
- Local development (Visual Studio, Azure CLI, environment variables)
- Azure-hosted environments (Managed Identity)

#### Legacy Authentication

For legacy scenarios, **Microsoft.IdentityModel.Clients.ActiveDirectory** (ADAL) is available but should be migrated to Azure.Identity when possible.

### Health Checks

BlackSlope includes comprehensive health check endpoints for monitoring:

```csharp
// Health check configuration
services.AddHealthChecks()
    .AddSqlServer(
        connectionString: configuration.GetConnectionString("MoviesConnectionString"),
        name: "sql-server",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddDbContextCheck<MoviesDbContext>(
        name: "ef-core-context",
        tags: new[] { "db", "ef-core" });
```

**Health Check Best Practices**:
- Add health checks for all external dependencies
- Use appropriate tags for filtering
- Configure timeouts to prevent hanging health checks
- Monitor health check endpoints in production
- Use health checks for container orchestration (Kubernetes liveness/readiness probes)

### Caching Strategy

Use **Microsoft.Extensions.Caching.Memory** for in-memory caching:

```csharp
public class MovieService
{
    private readonly IMemoryCache _cache;
    private readonly IMovieRepository _repository;

    public MovieService(IMemoryCache cache, IMovieRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<MovieDto> GetMovieByIdAsync(int id)
    {
        var cacheKey = $"movie_{id}";
        
        if (!_cache.TryGetValue(cacheKey, out MovieDto movie))
        {
            movie = await _repository.GetByIdAsync(id);
            
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));
            
            _cache.Set(cacheKey, movie, cacheOptions);
        }
        
        return movie;
    }
}
```

**Caching Best Practices**:
- Cache expensive operations (database queries, external API calls)
- Use appropriate expiration policies
- Implement cache invalidation for data mutations
- Consider distributed caching (Redis) for multi-instance deployments
- Monitor cache hit rates

## Additional Resources

- **Environment Setup**: [Development Environment Guide](/development/environment.md)
- **Code Quality**: [Code Quality Standards](/development/code_quality.md)
- **Testing**: [Testing Overview](/testing/overview.md)
- **BlackSlope Blog Series**:
  - [Introducing BlackSlope](https://medium.com/slalom-build/introducing-black-slope-a-dotnet-core-reference-architecture-from-slalom-build-3f1452eb62ef)
  - [BlackSlope Components Deep Dive](https://medium.com/slalom-build/blackslope-a-deeper-look-at-the-components-of-our-dotnet-reference-architecture-b7b3a9d6e43b)
  - [BlackSlope in Action](https://medium.com/slalom-build/blackslope-in-action-a-guide-to-using-our-dotnet-reference-architecture-d1e41eea8024)

## Getting Help

If you encounter issues or have questions:

1. **Check Documentation**: Review existing documentation and README
2. **Search Issues**: Look for similar issues in the GitHub issue tracker
3. **Ask Questions**: Open a GitHub issue with the `question` label
4. **Team Communication**: Reach out to the development team through your organization's communication channels

Thank you for contributing to BlackSlope.NET! Your contributions help make this reference architecture better for the entire community.