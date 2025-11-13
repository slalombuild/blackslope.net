# Testing Overview

## Testing Strategy

BlackSlope.NET implements a comprehensive testing strategy following the **test pyramid approach**, which emphasizes a solid foundation of fast, isolated unit tests, complemented by integration tests and behavior-driven development (BDD) tests for end-to-end scenarios.

### Test Pyramid Architecture

The testing strategy is structured in three layers:

1. **Unit Tests (Base Layer)** - Fast, isolated tests that verify individual components in isolation using mocking frameworks
2. **Integration Tests (Middle Layer)** - Tests that verify component interactions with real dependencies like databases and external APIs
3. **BDD/SpecFlow Tests (Top Layer)** - Acceptance tests written in Gherkin syntax that validate business requirements and user scenarios

### Testing Principles

- **Isolation**: Unit tests use mocking to isolate the system under test from external dependencies
- **Repeatability**: Tests produce consistent results regardless of execution order or environment
- **Fast Feedback**: Unit tests execute quickly to provide immediate feedback during development
- **Comprehensive Coverage**: Critical business logic and integration points are thoroughly tested
- **Maintainability**: Tests follow consistent patterns and naming conventions for easy maintenance

## Test Projects

BlackSlope.NET includes two dedicated test projects, each targeting .NET 6.0:

### BlackSlope.Api.Tests (Unit Tests)

The unit test project focuses on testing individual components in isolation using mocking frameworks.

**Project Configuration:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

**Key Dependencies:**

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.4.1 | Primary testing framework |
| xunit.runner.visualstudio | 2.4.3 | Visual Studio test runner integration |
| Moq | 4.16.1 | Mocking framework for creating test doubles |
| AutoFixture | 4.17.0 | Test data generation library |
| Microsoft.NET.Test.Sdk | 17.0.0 | .NET test SDK for test execution |

**Additional Dependencies for Testing Specific Components:**

- **Azure.Identity** (1.14.2) - For testing Azure AD authentication components
- **Microsoft.Data.SqlClient** (5.1.3) - For testing database-related functionality
- **Microsoft.Extensions.Caching.Memory** (6.0.2) - For testing caching implementations
- **Moq** (4.16.1) - For creating mock objects and verifying interactions
- **AutoFixture** (4.17.0) - For generating test data automatically

### BlackSlope.Api.IntegrationTests (Acceptance Tests)

BlackSlope.NET includes a SpecFlow-driven integration test project for acceptance testing:

**BlackSlope.Api.IntegrationTests** - SpecFlow BDD tests targeting .NET 6.0 with ReportPortal integration

**Project Configuration:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Key Dependencies:**

| Package | Version | Purpose |
|---------|---------|---------|
| SpecFlow | 3.9.40 | BDD framework for writing acceptance tests |
| SpecFlow.xUnit | 3.9.40 | xUnit integration for SpecFlow |
| SpecFlow.Tools.MsBuild.Generation | 3.9.40 | MSBuild code generation for feature files |
| xunit | 2.4.1 | Test execution framework |
| xunit.runner.visualstudio | 2.4.3 | Visual Studio test runner |
| AutoFixture | 4.17.0 | Test data generation |
| ReportPortal.SpecFlow | 3.2.1 | Test reporting and analytics integration |
| Microsoft.Extensions.Configuration.Json | 6.0.0 | JSON configuration support |

This project contains SpecFlow-based BDD acceptance tests that can be executed in Test Explorer like regular unit tests.

### Test Organization and Naming Conventions

Tests are organized following a consistent structure:

**Unit Test Structure:**

```
BlackSlope.Api.Tests/
├── ServicesTests/
│   ├── MoviesTests/
│   │   ├── MovieServiceTestsBase.cs
│   │   ├── GetMovieTests.cs
│   │   ├── CreateMovieTests.cs
│   │   └── UpdateMovieTests.cs
│   └── [OtherServices]Tests/
├── RepositoriesTests/
├── ValidatorsTests/
└── ControllersTests/
```

**Integration Test Structure:**

```
BlackSlope.Api.IntegrationTests/
├── Features/
│   └── [FeatureName].feature
├── StepDefinitions/
│   └── [FeatureName]Steps.cs
└── appsettings.test.json
```

> **Note**: The project explicitly excludes `Drivers/` and `Hooks/` directories from compilation and SpecFlow processing.

**Naming Conventions:**

- **Test Classes**: `[ComponentName]Tests` (e.g., `MovieServiceTests`)
- **Test Methods**: `[MethodName]_[Scenario]_[ExpectedResult]` (e.g., `GetMovie_WithValidId_ReturnsMovie`)
- **Base Test Classes**: `[ComponentName]TestsBase` for shared test setup and utilities

**Example Base Test Class Pattern:**

```csharp
using AutoFixture;
using AutoMapper;
using BlackSlope.Repositories.FakeApi;
using BlackSlope.Repositories.Movies;
using BlackSlope.Services.Movies;
using Moq;

namespace BlackSlope.Api.Tests.ServicesTests.MoviesTests
{
    public class MovieServiceTestsBase
    {
        protected readonly Fixture _fixture = new Fixture();
        protected Mock<IMapper> _mapper;
        protected readonly Mock<IMovieRepository> _movieRepository;
        protected readonly Mock<IFakeApiRepository> _fakeApiRepository;
        protected readonly IMovieService _service;

        public MovieServiceTestsBase()
        {
            _movieRepository = new Mock<IMovieRepository>();
            _fakeApiRepository = new Mock<IFakeApiRepository>();
            _mapper = new Mock<IMapper>();
            _service = new MovieService(_movieRepository.Object, _fakeApiRepository.Object ,_mapper.Object);
        }
    }
}
```

This base class pattern provides:
- **Shared Dependencies**: Common mocks and fixtures available to all derived test classes
- **Consistent Setup**: Standardized initialization of the system under test
- **AutoFixture Integration**: Automatic test data generation using the `_fixture` instance
- **Mock Management**: Centralized mock object creation and configuration

## Running Tests

### Command Line Execution

BlackSlope.NET uses the standard .NET CLI for test execution:

**Run All Tests:**

```bash
dotnet test ./src/
```

**Run Tests for a Specific Project:**

```bash
# Unit tests only
dotnet test ./src/BlackSlope.Api.Tests/BlackSlope.Api.Tests.csproj

# Acceptance/Integration tests
dotnet test ./src/BlackSlope.Api.IntegrationTests/AcceptanceTests/BlackSlope.Api.IntegrationTests.csproj
```

**Run Tests with Detailed Output:**

```bash
dotnet test ./src/ --verbosity detailed
```

**Run Tests with Code Coverage:**

```bash
dotnet test ./src/ --collect:"XPlat Code Coverage"
```

**Filter Tests by Category or Name:**

```bash
# Run tests matching a specific pattern
dotnet test ./src/ --filter "FullyQualifiedName~MovieService"

# Run tests by trait/category
dotnet test ./src/ --filter "Category=Integration"
```

### IDE Test Execution

**Visual Studio:**

1. Open **Test Explorer** (Test > Test Explorer)
2. Build the solution to discover tests
3. Run tests individually, by class, or by project using the Test Explorer UI
4. Use the search/filter functionality to locate specific tests
5. View test results, output, and stack traces directly in the Test Explorer

**Visual Studio Code:**

1. Install the **.NET Core Test Explorer** extension
2. Tests will appear in the Test sidebar
3. Click the play button next to individual tests or test groups
4. View results inline in the editor

**JetBrains Rider:**

1. Tests appear automatically in the **Unit Tests** window
2. Use the test runner toolbar to execute tests
3. Right-click on test classes or methods to run specific tests
4. View detailed test output and coverage information

### CI/CD Integration

BlackSlope.NET is designed for seamless integration with continuous integration pipelines:

**Azure DevOps Pipeline Example:**

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Unit Tests'
  inputs:
    command: 'test'
    projects: '**/BlackSlope.Api.Tests.csproj'
    arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage"'
    
- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    projects: '**/BlackSlope.Api.IntegrationTests.csproj'
    arguments: '--configuration $(buildConfiguration)'
```

**GitHub Actions Example:**

```yaml
- name: Run Tests
  run: dotnet test ./src/ --configuration Release --no-build --verbosity normal
  
- name: Publish Test Results
  uses: EnricoMi/publish-unit-test-result-action@v2
  if: always()
  with:
    files: '**/TestResults/*.trx'
```

**Docker-based Testing:**

```bash
# Build test image
docker build -t blackslope-tests -f Dockerfile.tests .

# Run tests in container
docker run --rm blackslope-tests dotnet test
```

## Test Configuration

### appsettings.test.json

Integration tests require specific configuration to connect to test environments and databases. The `appsettings.test.json` file provides this configuration:

```json
{
  "BlackSlopeHost": "https://localhost:44301",
  "DBConnectionString": "Server=localhost;Database=movies;Integrated Security=true;"
}
```

**Configuration Properties:**

| Property | Purpose | Example Value |
|----------|---------|---------------|
| BlackSlopeHost | Base URL for the API under test | `https://localhost:44301` |
| DBConnectionString | Connection string for test database | `Server=localhost;Database=movies_test;...` |

**File Configuration in Project:**

```xml
<ItemGroup>
  <Content Include="appsettings.test.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    <CopyToPublishDirectory>Always</CopyToPublishDirectory>
  </Content>
</ItemGroup>
```

This ensures the configuration file is copied to the output directory during build and available at test runtime.

### Test Environment Setup

**Prerequisites for Running Integration Tests:**

1. **SQL Server Instance**: A running SQL Server instance (Developer or Express edition)
2. **Test Database**: A dedicated test database separate from development/production
3. **API Instance**: The BlackSlope API running locally or in a test environment
4. **SpecFlow Plugins**: IDE plugins for SpecFlow feature file support

**Setting Up the Test Environment:**

```bash
# 1. Create test database
sqlcmd -S localhost -Q "CREATE DATABASE movies_test"

# 2. Apply migrations to test database
dotnet ef database update --project ./src/BlackSlope.Api/BlackSlope.Api.csproj --connection "Server=localhost;Database=movies_test;Integrated Security=true;"

# 3. Update appsettings.test.json with test database connection string

# 4. Start the API in a separate terminal/IDE instance
dotnet run --project ./src/BlackSlope.Api/BlackSlope.Api.csproj --environment Test
```

**Environment Isolation Best Practices:**

- **Separate Databases**: Always use a dedicated test database to avoid corrupting development data
- **Test Data Cleanup**: Implement teardown logic to clean up test data after execution
- **Configuration Management**: Use environment-specific configuration files (appsettings.test.json)
- **Port Configuration**: Use different ports for test API instances to avoid conflicts

### Test Data Management

**AutoFixture for Test Data Generation:**

BlackSlope.NET uses AutoFixture to automatically generate test data, reducing boilerplate and improving test maintainability:

```csharp
public class MovieServiceTestsBase
{
    protected readonly Fixture _fixture = new Fixture();
    
    // Generate a single object with random data
    var movie = _fixture.Create<Movie>();
    
    // Generate a collection
    var movies = _fixture.CreateMany<Movie>(5);
    
    // Customize specific properties
    var customMovie = _fixture.Build<Movie>()
        .With(m => m.Title, "Test Movie")
        .With(m => m.ReleaseYear, 2023)
        .Create();
}
```

**Test Data Strategies:**

1. **In-Memory Test Data**: Use AutoFixture for unit tests with mocked repositories
2. **Database Seeding**: Seed test databases with known data for integration tests
3. **Test Data Builders**: Create builder classes for complex domain objects
4. **Shared Fixtures**: Use xUnit class fixtures for expensive setup operations

**Example Test Data Builder Pattern:**

```csharp
public class MovieBuilder
{
    private string _title = "Default Title";
    private int _releaseYear = 2023;
    private string _director = "Default Director";
    
    public MovieBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }
    
    public MovieBuilder WithReleaseYear(int year)
    {
        _releaseYear = year;
        return this;
    }
    
    public Movie Build()
    {
        return new Movie
        {
            Title = _title,
            ReleaseYear = _releaseYear,
            Director = _director
        };
    }
}

// Usage in tests
var movie = new MovieBuilder()
    .WithTitle("Inception")
    .WithReleaseYear(2010)
    .Build();
```

### Integration Test Setup

**Prerequisites:**

1. Successfully complete the database build and application build steps (see [Installation](/getting_started/installation))
2. Update the `appsettings.test.json` file in your integration test project with:
   - Proper database connection string
   - Host URL for the BlackSlope API
3. Download the appropriate [SpecFlow plugins](https://docs.specflow.org/projects/specflow/en/latest/Installation/Installation.html) for your IDE

**Running API for Integration Tests:**

Integration tests require the BlackSlope API to be running. You have two options:

1. **Separate IDE Instance**: Run the API in a separate Visual Studio/Rider instance (required for the current integration test projects)
2. **WebApplicationFactory**: Use ASP.NET Core's `WebApplicationFactory` for in-process testing (alternative approach)

**WebApplicationFactory Example:**

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace production services with test doubles
            services.AddDbContext<MoviesContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });
        });
    }
}
```

**SpecFlow Configuration:**

SpecFlow tests require additional IDE plugins for feature file support:

- **Visual Studio**: Install the SpecFlow extension from the Visual Studio Marketplace
- **Rider**: SpecFlow support is built-in
- **VS Code**: Install the Cucumber (Gherkin) extension for syntax highlighting


## Related Documentation

For detailed information on specific testing approaches, refer to the following documentation:

- [Unit Tests](/testing/unit_tests.md) - Comprehensive guide to writing and organizing unit tests
- [Integration Tests](/testing/integration_tests.md) - Integration testing patterns and best practices
- [BDD Tests](/testing/bdd_tests.md) - SpecFlow and behavior-driven development guidelines

## Troubleshooting Common Issues

**Issue: Tests Cannot Find appsettings.test.json**

Ensure the file is configured to copy to the output directory:

```xml
<Content Include="appsettings.test.json">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
</Content>
```

**Issue: Integration Tests Fail with Connection Errors**

1. Verify SQL Server is running
2. Check the connection string in `appsettings.test.json`
3. Ensure the test database exists and migrations are applied
4. Verify firewall rules allow local connections

**Issue: SpecFlow Tests Not Discovered**

1. Install the appropriate SpecFlow IDE extension
2. Rebuild the solution to regenerate code-behind files
3. Check that `SpecFlow.Tools.MsBuild.Generation` is installed
4. Verify feature files have the `.feature` extension

**Issue: Mock Setup Not Working as Expected**

Ensure mock setups are configured before the system under test is instantiated:

```csharp
// Correct order
_movieRepository.Setup(x => x.GetById(It.IsAny<int>()))
    .ReturnsAsync(expectedMovie);
var result = await _service.GetMovie(1);

// Incorrect - setup after invocation won't work
var result = await _service.GetMovie(1);
_movieRepository.Setup(x => x.GetById(It.IsAny<int>()))
    .ReturnsAsync(expectedMovie);
```