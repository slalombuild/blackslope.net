# BDD with SpecFlow

## Behavior-Driven Development

Behavior-Driven Development (BDD) is a software development methodology that emphasizes collaboration between developers, QA engineers, and business stakeholders. In the BlackSlope.NET reference architecture, BDD is implemented using SpecFlow to create executable specifications that serve as both documentation and automated tests.

### BDD Concepts and Gherkin Syntax

BDD scenarios are written in **Gherkin**, a business-readable domain-specific language that describes software behavior without detailing implementation. Gherkin uses a structured format with the following keywords:

- **Feature**: A high-level description of a software feature
- **Scenario**: A concrete example of business behavior
- **Given**: Preconditions or initial context
- **When**: Actions or events that trigger behavior
- **Then**: Expected outcomes or assertions
- **And/But**: Additional steps of the same type

The Gherkin syntax enables non-technical stakeholders to understand test scenarios while providing a clear specification for developers to implement.

### Feature Files

Feature files (`.feature` extension) contain the Gherkin specifications. In BlackSlope.NET, these files are located in the `AcceptanceTests/Features` directory. Here's an example from the Movies API:

```gherkin
Feature: CreateMovie
	
	Use this operation to create a new movie and provide the following parameters 

	1. Title 
	2. Description
	3. ReleaseDate

@CreateMovie
Scenario: Add a new movie to the database 
	Given a user creates a new movie using post movie endpoint 
	And the movie is successfully created
```

**Key characteristics of feature files:**
- Written in plain English for business readability
- Tagged with `@CreateMovie` for test categorization and selective execution
- Describe the feature's purpose and required parameters
- Define concrete scenarios with Given-When-Then steps

### Step Definitions

Step definitions are C# methods that bind Gherkin steps to executable code. Each step in a feature file must have a corresponding step definition. SpecFlow uses regular expressions or method attributes to match steps to their implementations.

### Scenarios and Examples

Scenarios represent specific test cases. The BlackSlope implementation focuses on CRUD operations for the Movies API:
- **Create**: Adding new movies to the database
- **Update**: Modifying existing movie records
- **Delete**: Removing movies from the database
- **Get**: Retrieving movie information

Each scenario can be tagged for organization and selective test execution, enabling teams to run specific test suites (e.g., smoke tests, regression tests).

## SpecFlow Integration

SpecFlow is a .NET implementation of Cucumber that enables BDD practices in .NET projects. BlackSlope.NET integrates SpecFlow with xUnit as the test framework.

### SpecFlow Setup and Configuration

BlackSlope.NET provides SpecFlow-driven integration tests in the `BlackSlope.Api.IntegrationTests` project (located at `src/BlackSlope.Api.IntegrationTests/AcceptanceTests/`). These tests can be executed in Test Explorer like regular unit tests.

The integration test project includes the following SpecFlow-related NuGet packages:

```xml
<PackageReference Include="AutoFixture" Version="4.17.0" />
<PackageReference Include="Microsoft.AspNetCore.Hosting.Abstractions" Version="2.2.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="6.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="6.0.0" />
<PackageReference Include="ReportPortal.SpecFlow" Version="3.2.1" />
<PackageReference Include="SpecFlow" Version="3.9.40" />
<PackageReference Include="SpecFlow.Tools.MsBuild.Generation" Version="3.9.40" />
<PackageReference Include="SpecFlow.xUnit" Version="3.9.40" />
<PackageReference Include="xunit" Version="2.4.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.3">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Package purposes:**
- **AutoFixture**: Generates random test data to ensure unique values in each test run
- **Microsoft.AspNetCore.Hosting.Abstractions**: ASP.NET Core hosting abstractions for test setup
- **Microsoft.Extensions.Configuration**: Configuration framework support
- **Microsoft.Extensions.Configuration.Json**: JSON configuration provider for test settings
- **ReportPortal.SpecFlow**: Integration with ReportPortal for test reporting and analytics
- **SpecFlow**: Core BDD framework for .NET
- **SpecFlow.Tools.MsBuild.Generation**: Generates code-behind files from `.feature` files during build
- **SpecFlow.xUnit**: Integrates SpecFlow with xUnit test runner
- **xunit**: Test framework for executing tests
- **xunit.runner.visualstudio**: Enables test discovery and execution in Visual Studio Test Explorer

**Configuration file structure:**

The project includes `appsettings.test.json` for test-specific configuration:

```xml
<Content Include="appsettings.test.json">
  <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  <CopyToPublishDirectory>Always</CopyToPublishDirectory>
</Content>
```

This configuration file should contain:
- Database connection strings for the test environment
- API host URLs (can point to localhost or deployed test environments)
- Any environment-specific settings required for integration tests

### IDE Plugin Support

To work effectively with SpecFlow, developers should install IDE-specific plugins:

- **Visual Studio**: SpecFlow for Visual Studio extension
- **Visual Studio Code**: Cucumber (Gherkin) Full Support extension
- **Rider**: Built-in SpecFlow support

These plugins provide:
- Syntax highlighting for `.feature` files
- Navigation between steps and step definitions
- IntelliSense for Gherkin keywords
- Test execution integration

### .NET 6 Compatibility Notes

**Important**: As documented in the README.md, the integration test projects have been removed from the solution:

> NOTE: Per 6.x, these projects have been removed from the Solution until SpecFlow adds support for the latest version of .NET 6

**Current status:**
- The project targets `net6.0` framework
- SpecFlow version 3.9.40 is used (check for updates to ensure .NET 6 compatibility)
- Integration tests may need to be run separately or in a different IDE instance

**Workaround for running tests:**
1. Ensure the BlackSlope API is running (either on localhost or a test environment)
2. Update `appsettings.test.json` with correct connection strings and API URLs
3. Run tests from the command line using `dotnet test` or through Test Explorer
4. Consider running the API in a separate IDE instance to allow simultaneous test execution

## Movie Feature Tests

The BlackSlope.NET reference architecture demonstrates BDD testing through comprehensive Movie API scenarios. These tests validate the complete CRUD lifecycle of movie entities.

### Create Movie Scenarios

The create movie feature validates the ability to add new movies to the database through the POST endpoint.

**Feature file** (`CreateMovie.feature`):

```gherkin
Feature: CreateMovie
	
	Use this operation to create a new movie and provide the following parameters 

	1. Title 
	2. Description
	3. ReleaseDate

@CreateMovie
Scenario: Add a new movie to the database 
	Given a user creates a new movie using post movie endpoint 
	And the movie is successfully created
```

**Step implementation** (`CreateMovieSteps.cs`):

```csharp
[Binding]
public class CreateMovieSteps
{
    private readonly ITestServices _movieTestService;
    private readonly CreateMovieContext _createMovieContext;
    private MovieViewModel _response;
    private readonly Fixture _fixture = new();

    public CreateMovieSteps(ScenarioContext injectedContext,
        ITestServices movieTestService, CreateMovieContext createMovieContext)
    {
        _movieTestService = movieTestService;
        _createMovieContext = createMovieContext;
    }

    [Given(@"a user creates a new movie using post movie endpoint")]
    public async Task GivenAUserCreatesANewMovieUsingPostMovieEndpoint()
    {
        var createMovie = new CreateMovieViewModel
        {
            Title = _fixture.Create<string>(),
            Description = $"Create Movie_{_fixture.Create<string>()}",
            ReleaseDate = Convert.ToDateTime("2010/04/05")
        };
        _response = await _movieTestService.CreateMovie(createMovie);
    }

    [Given(@"the movie is successfully created")]
    public void GivenTheMovieIsSuccessfullyCreated()
    {
        Assert.NotNull(_response);
        Assert.True(_response.Id > 0);
    }

    [Given(@"a user gets the movie id of recently created movie")]
    public void GivenAUserGetsTheMovieIdOfRecentlyCreatedMovie()
    {
        _createMovieContext.MovieId = (int)_response.Id;
    }
}
```

**Key implementation details:**

1. **AutoFixture Integration**: Uses `AutoFixture` to generate random test data, ensuring each test run uses unique values
2. **Async/Await Pattern**: Step definitions support asynchronous operations for API calls
3. **Assertions**: Uses xUnit assertions (`Assert.NotNull`, `Assert.True`) to validate responses
4. **Context Sharing**: Stores the created movie ID in `CreateMovieContext` for use in subsequent scenarios

### Update Movie Scenarios

Update scenarios would follow a similar pattern, typically including:
- Given: A movie exists in the database (reusing the create movie context)
- When: The user updates the movie with new data
- Then: The movie is successfully updated and changes are persisted

**Example scenario structure:**

```gherkin
@UpdateMovie
Scenario: Update an existing movie
	Given a user creates a new movie using post movie endpoint
	And the movie is successfully created
	And a user gets the movie id of recently created movie
	When the user updates the movie with new title and description
	Then the movie is successfully updated
	And the updated values are persisted in the database
```

### Delete Movie Scenarios

Delete scenarios validate the removal of movies from the database:

**Example scenario structure:**

```gherkin
@DeleteMovie
Scenario: Delete an existing movie
	Given a user creates a new movie using post movie endpoint
	And the movie is successfully created
	And a user gets the movie id of recently created movie
	When the user deletes the movie
	Then the movie is successfully deleted
	And the movie no longer exists in the database
```

### Get Movie Scenarios

Get scenarios validate retrieval operations:

**Example scenario structure:**

```gherkin
@GetMovie
Scenario: Retrieve an existing movie by ID
	Given a user creates a new movie using post movie endpoint
	And the movie is successfully created
	And a user gets the movie id of recently created movie
	When the user retrieves the movie by ID
	Then the movie details are returned correctly
	And all properties match the created movie
```

## Step Implementations

Step implementations bridge the gap between Gherkin specifications and executable code. BlackSlope.NET follows established patterns for maintainable and reusable step definitions.

### Given-When-Then Patterns

The Given-When-Then pattern structures test scenarios into three distinct phases:

**Given (Arrange)**: Establishes the initial state
```csharp
[Given(@"a user creates a new movie using post movie endpoint")]
public async Task GivenAUserCreatesANewMovieUsingPostMovieEndpoint()
{
    // Arrange: Create test data
    var createMovie = new CreateMovieViewModel
    {
        Title = _fixture.Create<string>(),
        Description = $"Create Movie_{_fixture.Create<string>()}",
        ReleaseDate = Convert.ToDateTime("2010/04/05")
    };
    
    // Act: Execute the operation
    _response = await _movieTestService.CreateMovie(createMovie);
}
```

**When (Act)**: Performs the action being tested
```csharp
[When(@"the user updates the movie with new title and description")]
public async Task WhenTheUserUpdatesTheMovieWithNewTitleAndDescription()
{
    var updateMovie = new UpdateMovieViewModel
    {
        Id = _createMovieContext.MovieId,
        Title = _fixture.Create<string>(),
        Description = _fixture.Create<string>()
    };
    
    _response = await _movieTestService.UpdateMovie(updateMovie);
}
```

**Then (Assert)**: Verifies the expected outcome
```csharp
[Then(@"the movie is successfully created")]
public void ThenTheMovieIsSuccessfullyCreated()
{
    Assert.NotNull(_response);
    Assert.True(_response.Id > 0);
    Assert.NotEmpty(_response.Title);
}
```

### Shared Step Definitions

The `[Binding]` attribute marks classes as step definition containers. SpecFlow automatically discovers and registers these steps:

```csharp
[Binding]
public class CreateMovieSteps
{
    // Step definitions...
}
```

**Best practices for shared steps:**

1. **Reusable Given Steps**: Create common setup steps that can be used across multiple scenarios
2. **Parameterized Steps**: Use regular expressions to create flexible step definitions
3. **Step Argument Transformations**: Convert Gherkin table data into strongly-typed objects

**Example of parameterized step:**

```csharp
[Given(@"a movie with title ""(.*)"" exists")]
public async Task GivenAMovieWithTitleExists(string title)
{
    var createMovie = new CreateMovieViewModel
    {
        Title = title,
        Description = _fixture.Create<string>(),
        ReleaseDate = DateTime.Now
    };
    
    _response = await _movieTestService.CreateMovie(createMovie);
}
```

### Context Management

SpecFlow provides two primary mechanisms for sharing data between steps:

**1. ScenarioContext**: Built-in context for sharing data within a single scenario

```csharp
public CreateMovieSteps(ScenarioContext injectedContext,
    ITestServices movieTestService, CreateMovieContext createMovieContext)
{
    // ScenarioContext is automatically injected by SpecFlow
}
```

**2. Custom Context Classes**: Strongly-typed context objects for domain-specific data

```csharp
public class CreateMovieContext
{
    public int MovieId { get; set; }
}
```

**Context lifecycle:**
- **Scenario-scoped**: Context objects are created once per scenario and shared across all steps
- **Dependency Injection**: SpecFlow uses constructor injection to provide context objects
- **Thread-safe**: Each scenario runs in isolation with its own context instances

**Usage pattern:**

```csharp
[Given(@"a user gets the movie id of recently created movie")]
public void GivenAUserGetsTheMovieIdOfRecentlyCreatedMovie()
{
    // Store data in custom context for use in subsequent steps
    _createMovieContext.MovieId = (int)_response.Id;
}

// Later in another step class...
[When(@"the user deletes the movie")]
public async Task WhenTheUserDeletesTheMovie()
{
    // Retrieve data from shared context
    await _movieTestService.DeleteMovie(_createMovieContext.MovieId);
}
```

## Best Practices

### Writing Clear Scenarios

**1. Use Business Language**: Write scenarios in terms that business stakeholders understand

```gherkin
# Good: Business-focused
Scenario: User adds a movie to their watchlist
	Given the user is logged in
	When they add "Inception" to their watchlist
	Then "Inception" appears in their watchlist

# Avoid: Implementation-focused
Scenario: POST request to /api/watchlist endpoint
	Given an authenticated HTTP client
	When a POST request is sent with movie ID 123
	Then the response status is 201
```

**2. Keep Scenarios Independent**: Each scenario should be self-contained and not depend on the execution order

```gherkin
# Good: Self-contained scenario
Scenario: Delete a movie
	Given a movie "Test Movie" exists in the database
	When the user deletes "Test Movie"
	Then "Test Movie" no longer exists

# Avoid: Dependent on previous scenario
Scenario: Delete the movie created in the previous test
	When the user deletes the last created movie
	Then the movie no longer exists
```

**3. Use Scenario Outlines for Data Variations**: When testing multiple data combinations, use Scenario Outlines

```gherkin
Scenario Outline: Create movies with different release dates
	Given a user creates a movie with title "<title>" and release date "<date>"
	Then the movie is created successfully
	And the release date is "<date>"

Examples:
	| title          | date       |
	| Classic Movie  | 1990-01-01 |
	| Recent Movie   | 2023-06-15 |
	| Future Release | 2025-12-31 |
```

### Reusable Step Definitions

**1. Create Atomic Steps**: Design steps that perform single, well-defined actions

```csharp
// Good: Atomic step
[Given(@"a movie with title ""(.*)"" exists")]
public async Task GivenAMovieExists(string title)
{
    await CreateMovie(title);
}

// Avoid: Compound step doing multiple things
[Given(@"a movie exists and is added to watchlist")]
public async Task GivenAMovieExistsAndIsAddedToWatchlist()
{
    await CreateMovie();
    await AddToWatchlist();
}
```

**2. Use Step Argument Transformations**: Convert complex Gherkin data into domain objects

```csharp
[StepArgumentTransformation]
public CreateMovieViewModel TransformToCreateMovieViewModel(Table table)
{
    return new CreateMovieViewModel
    {
        Title = table.Rows[0]["Title"],
        Description = table.Rows[0]["Description"],
        ReleaseDate = DateTime.Parse(table.Rows[0]["ReleaseDate"])
    };
}

[Given(@"a user creates a movie with the following details")]
public async Task GivenAUserCreatesAMovieWithDetails(CreateMovieViewModel movie)
{
    _response = await _movieTestService.CreateMovie(movie);
}
```

**3. Leverage Dependency Injection**: Use constructor injection for services and contexts

```csharp
public class MovieSteps
{
    private readonly ITestServices _testServices;
    private readonly MovieContext _context;
    
    public MovieSteps(ITestServices testServices, MovieContext context)
    {
        _testServices = testServices;
        _context = context;
    }
}
```

### Test Data Management

**1. Use AutoFixture for Random Data**: Generate unique test data to avoid conflicts

```csharp
private readonly Fixture _fixture = new();

[Given(@"a user creates a new movie")]
public async Task GivenAUserCreatesANewMovie()
{
    var createMovie = new CreateMovieViewModel
    {
        Title = _fixture.Create<string>(), // Generates unique string
        Description = $"Test Movie_{_fixture.Create<string>()}",
        ReleaseDate = DateTime.Now
    };
    
    _response = await _movieTestService.CreateMovie(createMovie);
}
```

**2. Clean Up Test Data**: Implement hooks to clean up data after scenarios

```csharp
[AfterScenario]
public async Task CleanUpTestData()
{
    if (_createMovieContext.MovieId > 0)
    {
        await _movieTestService.DeleteMovie(_createMovieContext.MovieId);
    }
}
```

**3. Use Test-Specific Configuration**: Maintain separate configuration for test environments

```json
// appsettings.test.json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=localhost;Database=MoviesTest;..."
  },
  "ApiSettings": {
    "BaseUrl": "http://localhost:51385"
  }
}
```

**4. Isolate Test Database**: Use a dedicated test database to prevent interference with development or production data

**5. Consider Data Builders**: Create builder classes for complex test data

```csharp
public class MovieBuilder
{
    private string _title = "Default Title";
    private string _description = "Default Description";
    private DateTime _releaseDate = DateTime.Now;
    
    public MovieBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }
    
    public MovieBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }
    
    public CreateMovieViewModel Build()
    {
        return new CreateMovieViewModel
        {
            Title = _title,
            Description = _description,
            ReleaseDate = _releaseDate
        };
    }
}

// Usage in step definition
var movie = new MovieBuilder()
    .WithTitle("Inception")
    .WithDescription("A mind-bending thriller")
    .Build();
```

## Integration with BlackSlope Architecture

The SpecFlow integration tests work seamlessly with BlackSlope.NET's architecture:

**1. Test Services Layer**: The `ITestServices` interface abstracts HTTP communication with the API

**2. View Models**: Tests use the same view models (`CreateMovieViewModel`, `MovieViewModel`) as the API, ensuring contract consistency

**3. Configuration Management**: Leverages `Microsoft.Extensions.Configuration` for test-specific settings

**4. AutoMapper Integration**: Tests validate that AutoMapper configurations work correctly in real scenarios

**5. Health Checks**: Integration tests can verify health check endpoints before running test suites

## Related Documentation

For more information on testing and the Movies API, see:

- [Testing Overview](/testing/overview.md) - Comprehensive testing strategy and guidelines
- [Integration Tests](/testing/integration_tests.md) - Detailed integration testing documentation
- [Movies API Reference](/api_reference/movies_api.md) - Complete API endpoint documentation

## Troubleshooting

**Issue**: SpecFlow steps not discovered in Test Explorer

**Solution**: 
- Ensure SpecFlow IDE plugin is installed
- Rebuild the solution to regenerate code-behind files
- Check that `.feature` files have "SpecFlowSingleFileGenerator" as custom tool

**Issue**: Tests fail with connection errors

**Solution**:
- Verify `appsettings.test.json` has correct connection strings
- Ensure the BlackSlope API is running and accessible
- Check that the test database exists and is accessible

**Issue**: AutoFixture generates invalid data

**Solution**:
- Customize AutoFixture to generate valid data for your domain
- Use `_fixture.Build<T>().With()` to specify valid values
- Consider creating custom specimen builders for complex types