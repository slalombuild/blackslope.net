# Test Utilities

## Overview

The test utilities provide a comprehensive suite of reusable components designed to streamline test development across unit, integration, and BDD acceptance tests. These utilities abstract common testing patterns, reduce code duplication, and establish consistent testing practices throughout the application.

The test infrastructure is organized into several key areas:
- **Test Helpers**: Utility functions for data formatting and environment configuration
- **Test Services**: High-level service wrappers for API interactions
- **Test Data Management**: Context objects and data builders for test scenarios
- **Mock and Stub Utilities**: Base classes and patterns for mocking dependencies

## Test Helpers

### String Helpers for Test Data

The `StringHelper` class provides utility methods for formatting and manipulating string data in tests, particularly for JSON serialization and output formatting.

```csharp
using Newtonsoft.Json;

namespace AcceptanceTests.Helpers
{
    public class StringHelper
    {
        public static string FormatJSON(string unformattedJson)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(unformattedJson);
            string formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);

            return formattedJson;
        }
    }
}
```

**Key Features:**
- **JSON Formatting**: Converts compact JSON strings into human-readable, indented format
- **Test Output Enhancement**: Improves readability of JSON responses in test output logs
- **Dynamic Parsing**: Uses dynamic deserialization to handle any JSON structure

**Usage Example:**
```csharp
var response = await client.Get("/api/movies");
var formattedResponse = StringHelper.FormatJSON(response);
outputHelper.WriteLine(formattedResponse); // Pretty-printed JSON in test output
```

### Environment Configuration

The `Constants` class centralizes environment-specific configuration values used across acceptance tests:

```csharp
namespace AcceptanceTests.Helpers
{
    public static class Constants
    {
        public const string BaseRoute = "https://localhost:5001/api";
        public const string Movies = "/movies";
        // Additional route constants...
    }
}
```

**Best Practices:**
- Store base URLs, API routes, and common test values as constants
- Use environment variables for sensitive data (connection strings, API keys)
- Maintain separate configuration for different test environments (dev, staging, production)

### Constants Management

Constants should be organized by functional area and accessibility:

```csharp
public static class TestConstants
{
    // API Routes
    public const string BaseRoute = "https://localhost:5001/api";
    public const string Movies = "/movies";
    
    // Test Data
    public const string DefaultMovieTitle = "Test Movie";
    public const int DefaultMovieYear = 2023;
    
    // Timeouts
    public const int DefaultTimeoutSeconds = 30;
    public const int LongRunningTimeoutSeconds = 120;
}
```

## Test Services

### Movie Test Service

The `MovieService` class provides a high-level abstraction for interacting with the Movies API during acceptance tests. It encapsulates HTTP client operations and provides strongly-typed methods for common API operations.

```csharp
using System.Threading.Tasks;
using AcceptanceTests.Client;
using AcceptanceTests.Helpers;
using BlackSlope.Api.Operations.Movies.ViewModels;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace AcceptanceTests.TestServices
{
    public class MovieService : ITestServices
    {
        protected readonly ITestOutputHelper outputHelper;

        public MovieService(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        public async Task<MovieViewModel[]> GetMovies()
        {
            var client = new Client<MovieViewModel[]>(outputHelper);
            return await client.Get($"{Constants.BaseRoute}{Constants.Movies}");
        }

        public async Task<MovieViewModel> UpdateMovieById(CreateMovieViewModel movie, int movieId)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            var data = JsonConvert.SerializeObject(movie).ToString();
            var movieEditResponse = await client.UpdateAsStringAsync(
                data, 
                $"{Constants.BaseRoute}{Constants.Movies}/{movieId}"
            );
            return movieEditResponse;
        }

        public async Task<MovieViewModel> CreateMovie(CreateMovieViewModel movie)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            var body = JsonConvert.SerializeObject(movie).ToString();
            var url = $"{Constants.BaseRoute}{Constants.Movies}";
            var movieResponse = await client.CreateAsStringAsync(body, url);
            return movieResponse;
        }

        public async Task DeleteMovie(int movieId)
        {
            var client = new Client<object>(outputHelper);
            await client.Delete($"{Constants.BaseRoute}{Constants.Movies}/{movieId}");
        }

        public async Task<MovieViewModel> GetMovieById(int targetMovieId)
        {
            var client = new Client<MovieViewModel>(outputHelper);
            return await client.Get($"{Constants.BaseRoute}{Constants.Movies}/{targetMovieId}");
        }
    }
}
```

**Design Patterns:**

1. **Service Layer Pattern**: Abstracts HTTP operations behind domain-specific methods
2. **Dependency Injection**: Accepts `ITestOutputHelper` for test logging integration
3. **Generic Client**: Uses a generic `Client<T>` for type-safe HTTP operations
4. **Async/Await**: All operations are asynchronous for better test performance

**Key Benefits:**
- **Type Safety**: Returns strongly-typed view models instead of raw HTTP responses
- **Reusability**: Single service instance can be used across multiple test scenarios
- **Maintainability**: API endpoint changes only require updates in one location
- **Logging Integration**: Automatic test output through `ITestOutputHelper`

**Usage in BDD Tests:**
```csharp
[Given(@"I have created a movie with title ""(.*)""")]
public async Task GivenIHaveCreatedAMovie(string title)
{
    var movieService = new MovieService(_outputHelper);
    var createModel = new CreateMovieViewModel { Title = title };
    var createdMovie = await movieService.CreateMovie(createModel);
    _context.MovieId = createdMovie.Id;
}
```

### Test Client Wrappers

The generic `Client<T>` wrapper (referenced but not shown in source files) should implement:

```csharp
public class Client<T>
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly HttpClient _httpClient;

    public Client(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _httpClient = new HttpClient();
    }

    public async Task<T> Get(string url)
    {
        _outputHelper.WriteLine($"GET {url}");
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        _outputHelper.WriteLine($"Response: {StringHelper.FormatJSON(content)}");
        return JsonConvert.DeserializeObject<T>(content);
    }

    // Additional methods: Post, Put, Delete, etc.
}
```

### Request Helpers (RestSharp Implementation)

For tests using RestSharp, the `RequestHelper` provides static methods for common HTTP operations:

```csharp
using RestSharp;

namespace AcceptanceTestsRestSharp.Helpers
{
    public static class RequestHelper
    {
        public static IRestResponse Get(string endpoint)
        {
            RestClient rc = new RestClient(endpoint);
            return rc.Get(new RestRequest(""));
        }

        public static IRestResponse Get(string baseEndpoint, string endpoint)
        {
            RestClient rc = new RestClient(baseEndpoint);
            return rc.Get(new RestRequest(endpoint));
        }

        public static IRestResponse Post(string baseEndpoint, string endpoint, string value)
        {
            var rc = new RestClient(baseEndpoint);
            return Post(endpoint, value, rc);
        }

        public static IRestResponse Post(string endpoint, string value)
        {
            var rc = new RestClient(endpoint);
            return Post("", value, rc);
        }

        public static IRestResponse Put(string endpoint)
        {
            RestClient rc = new RestClient(endpoint);
            return rc.Put(new RestRequest(""));
        }

        public static IRestResponse Put(string endpoint, string value)
        {
            var rc = new RestClient(endpoint);
            return Put("", value, rc);
        }

        private static IRestResponse Put(string endpoint, string value, IRestClient rc)
        {
            var restRequest = new RestRequest(endpoint)
            {
                RequestFormat = DataFormat.Json
            }.AddParameter("application/json", value, ParameterType.RequestBody);
            return rc.Put(restRequest);
        }

        private static IRestResponse Post(string endpoint, string value, IRestClient rc)
        {
            var restRequest = new RestRequest(endpoint)
            {
                RequestFormat = DataFormat.Json
            }.AddParameter("application/json", value, ParameterType.RequestBody);
            return rc.Post(restRequest);
        }
    }
}
```

**RestSharp vs HttpClient:**

| Feature | RestSharp | HttpClient |
|---------|-----------|------------|
| **Ease of Use** | Simpler API, less boilerplate | More verbose, requires manual serialization |
| **Performance** | Good for most scenarios | Better for high-throughput scenarios |
| **Flexibility** | Limited customization | Full control over HTTP pipeline |
| **Maintenance** | External dependency | Built into .NET |

**When to Use Each:**
- **RestSharp**: Quick acceptance tests, simple CRUD operations, legacy test suites
- **HttpClient**: Performance-critical tests, custom authentication flows, modern test infrastructure

## Test Data Management

### Creating Test Data

Test data should be created using a combination of builders and factory methods:

```csharp
public class MovieTestDataBuilder
{
    private string _title = "Default Movie";
    private int _year = 2023;
    private string _director = "Default Director";

    public MovieTestDataBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public MovieTestDataBuilder WithYear(int year)
    {
        _year = year;
        return this;
    }

    public MovieTestDataBuilder WithDirector(string director)
    {
        _director = director;
        return this;
    }

    public CreateMovieViewModel Build()
    {
        return new CreateMovieViewModel
        {
            Title = _title,
            Year = _year,
            Director = _director
        };
    }
}
```

**Usage:**
```csharp
var movie = new MovieTestDataBuilder()
    .WithTitle("Inception")
    .WithYear(2010)
    .WithDirector("Christopher Nolan")
    .Build();
```

### Test Context Objects

Context objects maintain state across test steps in BDD scenarios:

```csharp
namespace AcceptanceTests.Models
{
    public class CreateMovieContext
    {
        public int MovieId { get; set; }
    }
}
```

This simple context class stores the ID of a created movie, allowing it to be referenced across multiple test steps in a BDD scenario. For more complex scenarios, context objects can be extended to include additional properties such as created entities, response data, test state, and cleanup tracking.

**Context Usage in BDD Steps:**
```csharp
public class MovieSteps
{
    private readonly CreateMovieContext _context;
    private readonly MovieService _movieService;

    public MovieSteps(CreateMovieContext context, ITestOutputHelper outputHelper)
    {
        _context = context;
        _movieService = new MovieService(outputHelper);
    }

    [Given(@"I have created a movie")]
    public async Task GivenIHaveCreatedAMovie()
    {
        var movie = new MovieTestDataBuilder().Build();
        var createdMovie = await _movieService.CreateMovie(movie);
        _context.MovieId = createdMovie.Id;
    }

    [Then(@"the movie should be retrievable")]
    public async Task ThenTheMovieShouldBeRetrievable()
    {
        var retrievedMovie = await _movieService.GetMovieById(_context.MovieId);
        Assert.NotNull(retrievedMovie);
    }
}
```

### Data Cleanup

Implement cleanup strategies to maintain test isolation:

```csharp
public class TestDataCleanup : IDisposable
{
    private readonly MovieService _movieService;
    private readonly List<int> _movieIdsToCleanup;

    public TestDataCleanup(MovieService movieService)
    {
        _movieService = movieService;
        _movieIdsToCleanup = new List<int>();
    }

    public void TrackForCleanup(int movieId)
    {
        _movieIdsToCleanup.Add(movieId);
    }

    public void Dispose()
    {
        foreach (var movieId in _movieIdsToCleanup)
        {
            try
            {
                _movieService.DeleteMovie(movieId).Wait();
            }
            catch (Exception ex)
            {
                // Log but don't fail cleanup
                Console.WriteLine($"Failed to cleanup movie {movieId}: {ex.Message}");
            }
        }
    }
}
```

**Usage with xUnit:**
```csharp
public class MovieTests : IDisposable
{
    private readonly TestDataCleanup _cleanup;
    private readonly MovieService _movieService;

    public MovieTests(ITestOutputHelper outputHelper)
    {
        _movieService = new MovieService(outputHelper);
        _cleanup = new TestDataCleanup(_movieService);
    }

    [Fact]
    public async Task CreateMovie_ShouldReturnCreatedMovie()
    {
        var movie = new MovieTestDataBuilder().Build();
        var created = await _movieService.CreateMovie(movie);
        _cleanup.TrackForCleanup(created.Id);
        
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
    }

    public void Dispose()
    {
        _cleanup.Dispose();
    }
}
```

## Mock and Stub Utilities

### Service Mocking Patterns

The `MovieServiceTestsBase` class demonstrates the standard pattern for setting up mocked dependencies in unit tests:

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

**Key Components:**

1. **AutoFixture**: Generates test data automatically, reducing boilerplate
2. **Moq**: Creates mock objects for dependency injection
3. **Base Class Pattern**: Provides common setup for all service tests

**Derived Test Class Example:**
```csharp
public class GetMovieByIdTests : MovieServiceTestsBase
{
    [Fact]
    public async Task GetMovieById_WhenMovieExists_ReturnsMovie()
    {
        // Arrange
        var movieId = 1;
        var movieEntity = _fixture.Create<Movie>();
        var movieViewModel = _fixture.Create<MovieViewModel>();
        
        _movieRepository
            .Setup(x => x.GetByIdAsync(movieId))
            .ReturnsAsync(movieEntity);
        
        _mapper
            .Setup(x => x.Map<MovieViewModel>(movieEntity))
            .Returns(movieViewModel);

        // Act
        var result = await _service.GetMovieByIdAsync(movieId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(movieViewModel.Id, result.Id);
        _movieRepository.Verify(x => x.GetByIdAsync(movieId), Times.Once);
    }

    [Fact]
    public async Task GetMovieById_WhenMovieDoesNotExist_ReturnsNull()
    {
        // Arrange
        var movieId = 999;
        _movieRepository
            .Setup(x => x.GetByIdAsync(movieId))
            .ReturnsAsync((Movie)null);

        // Act
        var result = await _service.GetMovieByIdAsync(movieId);

        // Assert
        Assert.Null(result);
        _movieRepository.Verify(x => x.GetByIdAsync(movieId), Times.Once);
    }
}
```

### Repository Stubs

For integration tests that require database interaction without full database setup:

```csharp
public class InMemoryMovieRepository : IMovieRepository
{
    private readonly Dictionary<int, Movie> _movies = new Dictionary<int, Movie>();
    private int _nextId = 1;

    public Task<Movie> GetByIdAsync(int id)
    {
        _movies.TryGetValue(id, out var movie);
        return Task.FromResult(movie);
    }

    public Task<IEnumerable<Movie>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<Movie>>(_movies.Values.ToList());
    }

    public Task<Movie> CreateAsync(Movie movie)
    {
        movie.Id = _nextId++;
        _movies[movie.Id] = movie;
        return Task.FromResult(movie);
    }

    public Task<Movie> UpdateAsync(Movie movie)
    {
        if (!_movies.ContainsKey(movie.Id))
            throw new KeyNotFoundException($"Movie with ID {movie.Id} not found");
        
        _movies[movie.Id] = movie;
        return Task.FromResult(movie);
    }

    public Task DeleteAsync(int id)
    {
        _movies.Remove(id);
        return Task.CompletedTask;
    }
}
```

### HTTP Client Mocking

For testing services that make external HTTP calls:

```csharp
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> _responses;

    public MockHttpMessageHandler()
    {
        _responses = new Dictionary<string, HttpResponseMessage>();
    }

    public void AddResponse(string url, HttpResponseMessage response)
    {
        _responses[url] = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri.ToString();
        if (_responses.TryGetValue(url, out var response))
        {
            return Task.FromResult(response);
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
```

**Usage:**
```csharp
[Fact]
public async Task ExternalApiCall_ReturnsExpectedData()
{
    // Arrange
    var mockHandler = new MockHttpMessageHandler();
    mockHandler.AddResponse(
        "https://external-api.com/movies",
        new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"title\":\"Test Movie\"}")
        }
    );

    var httpClient = new HttpClient(mockHandler);
    var service = new ExternalMovieService(httpClient);

    // Act
    var result = await service.GetMoviesFromExternalApi();

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Movie", result.Title);
}
```

## Best Practices

### Reusable Test Utilities

**Principle: DRY (Don't Repeat Yourself)**

Create utility methods for common operations:

```csharp
public static class TestUtilities
{
    public static async Task<MovieViewModel> CreateTestMovie(
        MovieService service, 
        string title = null)
    {
        var movie = new MovieTestDataBuilder()
            .WithTitle(title ?? $"Test Movie {Guid.NewGuid()}")
            .Build();
        
        return await service.CreateMovie(movie);
    }

    public static void AssertMoviesAreEqual(
        MovieViewModel expected, 
        MovieViewModel actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Year, actual.Year);
        Assert.Equal(expected.Director, actual.Director);
    }

    public static async Task<List<MovieViewModel>> CreateMultipleMovies(
        MovieService service, 
        int count)
    {
        var movies = new List<MovieViewModel>();
        for (int i = 0; i < count; i++)
        {
            var movie = await CreateTestMovie(service, $"Movie {i + 1}");
            movies.Add(movie);
        }
        return movies;
    }
}
```

### Test Data Builders

**Builder Pattern Benefits:**
- Fluent, readable test setup
- Default values for optional properties
- Easy to create variations of test data

```csharp
public class MovieTestDataBuilder
{
    private string _title = "Default Movie";
    private int _year = DateTime.Now.Year;
    private string _director = "Default Director";
    private List<string> _genres = new List<string> { "Drama" };
    private decimal _rating = 7.5m;

    public MovieTestDataBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public MovieTestDataBuilder WithYear(int year)
    {
        _year = year;
        return this;
    }

    public MovieTestDataBuilder WithDirector(string director)
    {
        _director = director;
        return this;
    }

    public MovieTestDataBuilder WithGenres(params string[] genres)
    {
        _genres = genres.ToList();
        return this;
    }

    public MovieTestDataBuilder WithRating(decimal rating)
    {
        _rating = rating;
        return this;
    }

    public MovieTestDataBuilder AsClassic()
    {
        _year = 1950;
        _rating = 9.0m;
        return this;
    }

    public MovieTestDataBuilder AsRecent()
    {
        _year = DateTime.Now.Year;
        return this;
    }

    public CreateMovieViewModel Build()
    {
        return new CreateMovieViewModel
        {
            Title = _title,
            Year = _year,
            Director = _director,
            Genres = _genres,
            Rating = _rating
        };
    }

    public static MovieTestDataBuilder Default() => new MovieTestDataBuilder();
}
```

**Usage Examples:**
```csharp
// Simple case
var movie = MovieTestDataBuilder.Default().Build();

// Custom configuration
var movie = MovieTestDataBuilder.Default()
    .WithTitle("The Godfather")
    .WithYear(1972)
    .WithDirector("Francis Ford Coppola")
    .WithGenres("Crime", "Drama")
    .WithRating(9.2m)
    .Build();

// Preset configurations
var classicMovie = MovieTestDataBuilder.Default().AsClassic().Build();
var recentMovie = MovieTestDataBuilder.Default().AsRecent().Build();
```

### Helper Method Organization

Organize helper methods by functional area:

```
TestUtilities/
├── Data/
│   ├── MovieDataBuilder.cs
│   ├── UserDataBuilder.cs
│   └── TestDataFactory.cs
├── Assertions/
│   ├── MovieAssertions.cs
│   └── ApiResponseAssertions.cs
├── Helpers/
│   ├── StringHelper.cs
│   ├── DateHelper.cs
│   └── JsonHelper.cs
├── Services/
│   ├── MovieService.cs
│   ├── UserService.cs
│   └── ITestServices.cs
└── Mocks/
    ├── MockRepositories.cs
    ├── MockHttpClients.cs
    └── TestDoubles.cs
```

**Naming Conventions:**
- **Builders**: `{Entity}TestDataBuilder` or `{Entity}Builder`
- **Services**: `{Entity}Service` or `{Entity}TestService`
- **Helpers**: `{Purpose}Helper` (e.g., `StringHelper`, `DateHelper`)
- **Assertions**: `{Entity}Assertions` or `Assert{Entity}`
- **Mocks**: `Mock{Interface}` or `Fake{Class}`

### Common Pitfalls and Solutions

| Pitfall | Problem | Solution |
|---------|---------|----------|
| **Shared State** | Tests interfere with each other | Use test context objects, implement proper cleanup |
| **Hard-coded Values** | Tests break when data changes | Use constants, builders, and configuration |
| **Tight Coupling** | Tests depend on implementation details | Test through interfaces, use dependency injection |
| **Slow Tests** | Integration tests take too long | Use in-memory databases, mock external services |
| **Flaky Tests** | Tests pass/fail inconsistently | Avoid time-dependent logic, use deterministic data |
| **Poor Readability** | Tests are hard to understand | Use descriptive names, arrange-act-assert pattern |

### Integration with Test Frameworks

**xUnit Integration:**
```csharp
public class MovieIntegrationTests : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly MovieService _movieService;
    private readonly TestDataCleanup _cleanup;

    public MovieIntegrationTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        var client = _factory.CreateClient();
        _movieService = new MovieService(client);
        _cleanup = new TestDataCleanup(_movieService);
    }

    [Fact]
    public async Task CreateMovie_Integration_Success()
    {
        var movie = MovieTestDataBuilder.Default().Build();
        var created = await _movieService.CreateMovie(movie);
        _cleanup.TrackForCleanup(created.Id);

        Assert.NotNull(created);
        Assert.True(created.Id > 0);
    }

    public void Dispose()
    {
        _cleanup.Dispose();
    }
}
```

## Related Documentation

For more information on testing strategies and implementation:

- [Unit Tests](/testing/unit_tests.md) - Detailed unit testing patterns and examples
- [Integration Tests](/testing/integration_tests.md) - Integration testing setup and best practices
- [BDD Tests](/testing/bdd_tests.md) - Behavior-driven development test implementation

## Summary

The test utilities framework provides a robust foundation for testing across all layers of the application. By following these patterns and practices:

- **Reduce duplication** through reusable services and helpers
- **Improve maintainability** with centralized test infrastructure
- **Enhance readability** using builders and fluent APIs
- **Ensure reliability** through proper cleanup and isolation
- **Accelerate development** with consistent testing patterns

These utilities should be continuously refined as new testing needs emerge, always prioritizing clarity, reusability, and maintainability.