# Integration Testing

## Overview

The BlackSlope.NET application provides comprehensive integration testing capabilities through two distinct test projects, each utilizing different HTTP client implementations. These integration tests validate end-to-end API functionality, including request/response handling, database interactions, and service integrations.

> **Note**: As of .NET 6.x, the integration test projects have been temporarily removed from the main solution pending SpecFlow's full support for .NET 6. However, the architecture and patterns remain valid for implementation.

## Integration Test Architecture

### Test Project Structure

BlackSlope provides two parallel integration test implementations:

1. **BlackSlope.Api.Tests.IntegrationTests**
   - Uses standard `System.Net.Http.HttpClient`
   - Lightweight and framework-native approach

2. **BlackSlope.Api.Tests.RestSharpIntegrationTests**
   - Uses RestSharp HTTP client library
   - Provides additional abstraction and convenience methods

Both implementations follow the **SpecFlow BDD (Behavior-Driven Development)** pattern, allowing tests to be written in Gherkin syntax and executed as integration tests.

### Test Environment Setup

The integration test environment is configured through the `EnvironmentSetup` class, which serves as the central configuration point for all test scenarios:

```csharp
[Binding]
public sealed class EnvironmentSetup
{
    private readonly IObjectContainer objectContainer;
    private readonly ITestOutputHelper _outputHelper;

    public EnvironmentSetup(IObjectContainer objectContainer, ITestOutputHelper outputHelper)
    {
        this.objectContainer = objectContainer;
        _outputHelper = outputHelper;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        // Load test configuration from appsettings.test.json
        var configuration = new ConfigurationBuilder()
                                  .SetBasePath(Directory.GetCurrentDirectory())
                                  .AddJsonFile("appsettings.test.json")
                                  .Build(); 
        
        // Set global environment variables
        Environments.BaseUrl = configuration["BlackSlopeHost"];
        Environments.DBConnection = configuration["DBConnectionString"];
    }

    [BeforeScenario]
    public void InitializeWebServices()
    {
        // Register test services for dependency injection
        var movieService = new MovieService(_outputHelper);
        objectContainer.RegisterInstanceAs<ITestServices>(movieService);
    }
}
```

**Key Components:**

- **`[BeforeTestRun]`**: Executes once before all tests, loading configuration from `appsettings.test.json`
- **`[BeforeScenario]`**: Executes before each test scenario, initializing required services
- **`IObjectContainer`**: SpecFlow's dependency injection container (BoDi)
- **`ITestOutputHelper`**: xUnit's test output interface for logging

### Configuration File Structure

Create an `appsettings.test.json` file in your integration test project:

```json
{
  "BlackSlopeHost": "http://localhost:51385",
  "DBConnectionString": "Server=localhost;Database=BlackSlopeMovies_Test;Trusted_Connection=True;"
}
```

> **Important**: Ensure the API is running on the specified host before executing integration tests. You can run the API in a separate IDE instance or as a standalone process.

### API Test Client

The test projects provide two distinct client implementations for interacting with the API.

## HTTP Client Approaches

### Standard HttpClient Tests

The `Client<T>` class provides a generic, strongly-typed wrapper around `System.Net.Http.HttpClient`:

```csharp
public class Client<T>
{
    private HttpClient client;
    private readonly string baseUrl;
    private readonly ITestOutputHelper _output;

    public Client(ITestOutputHelper output)
    {
        baseUrl = Environments.BaseUrl;
        _output = output;
    }

    private void ClientSetup()
    {
        client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
    }

    public async Task<T> CreateAsStringAsync(string body, string path)
    {
        ClientSetup();
        HttpContent payload = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(path, payload);
        var json = await response.Content.ReadAsStringAsync();
        var jsonModel = Deserialize(json, response);
        
        // Comprehensive logging for debugging
        _output.WriteLine(" client {0}", response.RequestMessage);
        _output.WriteLine(" payload Data for Uri {0}", payload.ToString());
        _output.WriteLine(" Response Data for Uri {0}", response.ToString());
        _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
        _output.WriteLine(" Response Content: {0} ", json.ToString());

        return jsonModel;
    }

    public async Task<T> Get(string path)
    {
        ClientSetup();
        var response = await client.GetAsync(path);
        var json = await response.Content.ReadAsStringAsync();
        var jsonModel = Deserialize(json, response);
        
        _output.WriteLine(" client {0}", response.RequestMessage);
        _output.WriteLine(" Response Data for Uri {0}", response.ToString());
        _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
        _output.WriteLine(" Response Content: {0} ", json.ToString());

        return jsonModel;
    }

    public async Task<T> Delete(string path)
    {
        ClientSetup();
        var response = await client.DeleteAsync(path);
        var json = await response.Content.ReadAsStringAsync();
        var jsonModel = Deserialize(json, response);
        
        _output.WriteLine(" client {0}", response.RequestMessage);
        _output.WriteLine(" Response Data for Uri {0}", response.ToString());
        _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
        _output.WriteLine(" Response Content: {0} ", json.ToString());
        
        return jsonModel;
    }

    public async Task<T> UpdateAsStringAsync(string body, string path)
    {
        ClientSetup();
        HttpContent payload = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PutAsync(path, payload);
        var json = await response.Content.ReadAsStringAsync();
        var jsonModel = Deserialize(json, response);
        
        _output.WriteLine(" client {0}", response.RequestMessage);
        _output.WriteLine(" payload Data for Uri {0}", payload.ToString());
        _output.WriteLine(" Response Data for Uri {0}", response.ToString());
        _output.WriteLine(" Response Status: {0}", response.StatusCode.ToString());
        _output.WriteLine(" Response Content: {0} ", json.ToString());

        return jsonModel;
    }

    private T Deserialize(string json, HttpResponseMessage response)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        try
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
        catch (Exception e)
        {
            _output.WriteLine("Error deserializing response from {0} : {1}", 
                response.RequestMessage.ToString(), e.Message);
            _output.WriteLine("Error Response Status: {0}", response.StatusCode.ToString());
            _output.WriteLine("Error Response Content: {0} ", 
                StringHelper.FormatJSON(response.Content.ToString()));

            throw;
        }
    }
}
```

**Key Features:**

- **Generic Type Parameter**: Allows strongly-typed responses for any model
- **Comprehensive Logging**: All requests and responses are logged to test output
- **Error Handling**: Detailed error information for deserialization failures
- **Flexible Deserialization**: Ignores null values and missing members to handle API changes gracefully
- **Fresh Client Per Request**: `ClientSetup()` creates a new client for each operation to avoid state issues

**Usage Example:**

```csharp
var client = new Client<MovieResponse>(_outputHelper);
var movie = await client.Get("/api/v1/movies/123");
```

### RestSharp Tests

The `ApiClient` class provides a synchronous, RestSharp-based alternative:

```csharp
public class ApiClient
{
    private readonly ITestOutputHelper _output;

    public ApiClient(ITestOutputHelper output)
    {
        _output = output;
    }

    public IRestResponse Get(string path)
    {
        return SendRequest(path, Method.GET);
    }

    public IRestResponse Put(string path, string content)
    {
        return SendRequest(path, Method.PUT, content);
    }

    public IRestResponse Post(string path, string content)
    {
        return SendRequest(path, Method.POST, content);
    }

    public IRestResponse Delete(string path)
    {
        return SendRequest(path, Method.DELETE);
    }

    private IRestResponse SendRequest(string path, Method method, string content)
    {
        var client = new RestClient(Environments.BaseUrl);
        var request = new RestRequest(path, method);
        request.AddHeader("Cache-Control", "no-cache");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", content, ParameterType.RequestBody);
        
        _output.WriteLine(" Request Data for Uri:{0}", content);
        
        return client.Execute(request);
    }

    private IRestResponse SendRequest(string path, Method method)
    {
        var client = new RestClient(Environments.BaseUrl);
        var request = new RestRequest(path, method);
        request.AddHeader("Cache-Control", "no-cache");
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Content-Type", "application/json");
        
        return client.Execute(request);
    }

    // Generic methods for strongly-typed responses
    public T Get<T>(string path)
    {
        var response = SendRequest(path, Method.GET);
        return ConvertToModelViewResponse<T>(response);
    }

    public T Put<T>(string path, string content)
    {
        var response = SendRequest(path, Method.PUT, content);
        return ConvertToModelViewResponse<T>(response);
    }

    public T Post<T>(string path, string content)
    {
        var response = SendRequest(path, Method.POST, content);
        return ConvertToModelViewResponse<T>(response);
    }

    public T Delete<T>(string path)
    {
        var response = SendRequest(path, Method.DELETE);
        return ConvertToModelViewResponse<T>(response);
    }

    private T ConvertToModelViewResponse<T>(IRestResponse restResponse)
    {
        _output.WriteLine(" Response Data for Uri {0}", restResponse.ResponseUri.ToString());
        _output.WriteLine(" Response Status: {0}", restResponse.StatusCode.ToString());
        _output.WriteLine(" Response Content: {0} ", 
            StringHelper.FormatJSON(restResponse.Content.ToString()));

        try
        {
            return JsonConvert.DeserializeObject<T>(restResponse.Content);
        }
        catch (Exception e)
        {
            _output.WriteLine("Error deserializing response from {0} : {1}", 
                restResponse.ResponseUri.ToString(), e.Message);
            _output.WriteLine("Error Response Status: {0}", restResponse.StatusCode.ToString());
            _output.WriteLine("Error Response Content: {0} ", 
                StringHelper.FormatJSON(restResponse.Content.ToString()));

            throw;
        }
    }
}
```

**Key Features:**

- **Synchronous Operations**: Simpler test code without async/await complexity
- **RestSharp Abstraction**: Leverages RestSharp's fluent API for request building
- **Dual Interface**: Supports both raw `IRestResponse` and strongly-typed responses
- **Automatic Caching Prevention**: Includes `Cache-Control: no-cache` header by default

**Usage Example:**

```csharp
var client = new ApiClient(_outputHelper);
var movie = client.Get<MovieResponse>("/api/v1/movies/123");
```

### Client Configuration

Both client implementations share common configuration patterns:

| Configuration | Value | Purpose |
|---------------|-------|---------|
| **Base URL** | `Environments.BaseUrl` | Loaded from `appsettings.test.json` |
| **Accept Header** | `application/json` | Ensures JSON responses |
| **Content-Type** | `application/json` | Specifies JSON request bodies |
| **Cache-Control** | `no-cache` | Prevents caching issues in tests |

### Choosing Between HttpClient and RestSharp

| Consideration | HttpClient | RestSharp |
|---------------|------------|-----------|
| **Async Support** | Native async/await | Synchronous (simpler for tests) |
| **Framework Dependency** | .NET built-in | External NuGet package |
| **Learning Curve** | Standard .NET patterns | RestSharp-specific API |
| **Flexibility** | Full control over HTTP | Higher-level abstractions |
| **Test Complexity** | Requires async test methods | Simpler synchronous tests |

**Recommendation**: Use **HttpClient** for new projects to align with modern .NET patterns and avoid external dependencies. Use **RestSharp** if your team prefers synchronous test code or already has RestSharp expertise.

## Test Helpers

### Environment Setup

The `Environments` static class provides centralized configuration management:

```csharp
public static class Environments
{
    public static string BaseUrl { get; set; }
    public static string DBConnection { get; set; }
}
```

This class is populated during the `[BeforeTestRun]` phase and provides global access to configuration values throughout the test suite.

### String Helpers

The `StringHelper` class provides utility methods for formatting and manipulating test data:

```csharp
public static class StringHelper
{
    public static string FormatJSON(string json)
    {
        try
        {
            var parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }
        catch
        {
            return json; // Return original if formatting fails
        }
    }
}
```

**Usage**: Primarily used in logging to make JSON responses more readable in test output.

### Constants Management

The `Constants` class centralizes API route definitions:

```csharp
public static class Constants
{
    public const string BaseRoute = "/api/v1";
    public const string Movies = "/movies";
}
```

**Usage Example:**

```csharp
var fullPath = Constants.BaseRoute + Constants.Movies + "/123";
var movie = await client.Get(fullPath);
```

**Benefits:**

- **Single Source of Truth**: Route changes only need to be updated in one location
- **Type Safety**: Compile-time checking of route strings
- **Discoverability**: IntelliSense support for available routes
- **Consistency**: Ensures all tests use the same route patterns

## Running Integration Tests

### Prerequisites

Before running integration tests, ensure the following:

1. **Database Setup**
   - SQL Server instance is running
   - Test database exists (e.g., `BlackSlopeMovies_Test`)
   - Connection string in `appsettings.test.json` is correct
   - Database schema is up-to-date (run migrations if needed)

2. **API Configuration**
   - `appsettings.test.json` exists in the test project root
   - `BlackSlopeHost` points to the running API instance
   - `DBConnectionString` points to the test database

3. **SpecFlow Setup**
   - SpecFlow extension installed in your IDE:
     - Visual Studio: [SpecFlow for Visual Studio](https://marketplace.visualstudio.com/items?itemName=TechTalkSpecFlowTeam.SpecFlowForVisualStudio)
     - Rider: Built-in support
     - VS Code: [Cucumber (Gherkin) extension](https://marketplace.visualstudio.com/items?itemName=alexkrechik.cucumberautocomplete)

### Starting the API

**Option 1: Separate IDE Instance**

1. Open a second instance of your IDE
2. Open the BlackSlope.NET solution
3. Run the `BlackSlope.Api` project
4. Verify the API is accessible at the configured URL (default: `http://localhost:51385`)
5. Check Swagger UI: `http://localhost:51385/swagger`

**Option 2: Command Line**

```bash
cd src/BlackSlope.Api
dotnet run
```

**Option 3: Docker Container**

```bash
cd src
docker build -t blackslope.api -f Dockerfile .
docker run -p 51385:80 blackslope.api
```

### Test Execution

**Visual Studio Test Explorer:**

1. Build the solution
2. Open Test Explorer (Test → Test Explorer)
3. Click "Run All" or select specific tests
4. View results and output in the Test Explorer window

**Command Line:**

```bash
# Run all tests in the solution
dotnet test ./src/

# Run with detailed output
dotnet test ./src/ --logger "console;verbosity=detailed"
```

**Rider:**

1. Right-click on the test project
2. Select "Run Unit Tests"
3. View results in the Unit Tests window

### Test Output and Logging

Both client implementations provide comprehensive logging through `ITestOutputHelper`:

```
 client Method: GET, RequestUri: 'http://localhost:51385/api/v1/movies/123', Version: 1.1, Content: <null>, Headers: { Accept: application/json Content-Type: application/json }
 Response Data for Uri http://localhost:51385/api/v1/movies/123
 Response Status: OK
 Response Content: {
  "id": 123,
  "title": "The Matrix",
  "releaseYear": 1999,
  "genre": "Science Fiction"
}
```

**Accessing Test Output:**

- **Visual Studio**: Click on a test in Test Explorer, then view the "Output" link
- **Command Line**: Use `--logger "console;verbosity=detailed"`
- **Rider**: Click on a test result to view output in the bottom panel

### Cleanup Procedures

Integration tests should clean up after themselves to maintain test isolation. Implement cleanup in your test services:

```csharp
[AfterScenario]
public void CleanupTestData()
{
    // Delete test data created during the scenario
    using (var connection = new SqlConnection(Environments.DBConnection))
    {
        connection.Open();
        var command = new SqlCommand("DELETE FROM Movies WHERE Title LIKE '%_TEST_%'", connection);
        command.ExecuteNonQuery();
    }
}
```

**Cleanup Strategies:**

1. **Transaction Rollback**: Wrap each test in a transaction and roll back at the end
2. **Test Data Markers**: Use special naming conventions (e.g., `_TEST_` prefix) for easy identification
3. **Separate Test Database**: Use a dedicated test database that can be reset between test runs
4. **Database Snapshots**: Create a snapshot before tests and restore after (SQL Server Enterprise)

## Best Practices

### Test Isolation

Each test should be completely independent and not rely on the state left by previous tests.

**Anti-Pattern:**

```csharp
// Test 1 creates a movie
[Scenario]
public void CreateMovie()
{
    var movie = client.Post<Movie>("/api/v1/movies", movieJson);
    // Movie ID 123 is created
}

// Test 2 assumes movie 123 exists (BAD!)
[Scenario]
public void UpdateMovie()
{
    var movie = client.Put<Movie>("/api/v1/movies/123", updatedJson);
}
```

**Best Practice:**

```csharp
[Scenario]
public void UpdateMovie()
{
    // Create the movie as part of this test
    var movie = client.Post<Movie>("/api/v1/movies", movieJson);
    
    // Now update it
    var updatedMovie = client.Put<Movie>($"/api/v1/movies/{movie.Id}", updatedJson);
    
    // Clean up
    client.Delete($"/api/v1/movies/{movie.Id}");
}
```

### Database State Management

**Strategy 1: Test Database Reset**

```csharp
[BeforeTestRun]
public static void ResetDatabase()
{
    using (var connection = new SqlConnection(Environments.DBConnection))
    {
        connection.Open();
        var command = new SqlCommand(@"
            DELETE FROM Movies;
            DBCC CHECKIDENT ('Movies', RESEED, 0);
        ", connection);
        command.ExecuteNonQuery();
    }
}
```

**Strategy 2: Unique Test Data**

```csharp
public class TestDataFactory
{
    private static int _counter = 0;
    
    public static Movie CreateUniqueMovie()
    {
        var id = Interlocked.Increment(ref _counter);
        return new Movie
        {
            Title = $"Test Movie {id}_{Guid.NewGuid()}",
            ReleaseYear = 2000 + id,
            Genre = "Test"
        };
    }
}
```

**Strategy 3: Database Transactions**

```csharp
[BeforeScenario]
public void BeginTransaction()
{
    _transaction = _dbContext.Database.BeginTransaction();
}

[AfterScenario]
public void RollbackTransaction()
{
    _transaction?.Rollback();
    _transaction?.Dispose();
}
```

### Test Data Cleanup

Implement a robust cleanup strategy to prevent test data accumulation:

```csharp
public class TestDataTracker
{
    private readonly List<string> _createdResourceUrls = new List<string>();
    private readonly ApiClient _client;

    public TestDataTracker(ApiClient client)
    {
        _client = client;
    }

    public T Create<T>(string path, string content)
    {
        var result = _client.Post<T>(path, content);
        _createdResourceUrls.Add($"{path}/{GetId(result)}");
        return result;
    }

    public void CleanupAll()
    {
        foreach (var url in _createdResourceUrls.Reverse<string>())
        {
            try
            {
                _client.Delete(url);
            }
            catch (Exception ex)
            {
                // Log but don't fail cleanup
                Console.WriteLine($"Failed to cleanup {url}: {ex.Message}");
            }
        }
        _createdResourceUrls.Clear();
    }

    private int GetId(object obj)
    {
        return (int)obj.GetType().GetProperty("Id").GetValue(obj);
    }
}
```

**Usage:**

```csharp
[BeforeScenario]
public void Setup()
{
    _dataTracker = new TestDataTracker(_client);
}

[Scenario]
public void TestMovieCreation()
{
    var movie = _dataTracker.Create<Movie>("/api/v1/movies", movieJson);
    // Test logic...
}

[AfterScenario]
public void Cleanup()
{
    _dataTracker.CleanupAll();
}
```

### Handling Asynchronous Operations

When using the HttpClient implementation, ensure proper async/await patterns:

```csharp
[Scenario]
public async Task CreateAndVerifyMovie()
{
    // Create
    var createdMovie = await _client.CreateAsStringAsync(movieJson, "/api/v1/movies");
    
    // Verify
    var retrievedMovie = await _client.Get($"/api/v1/movies/{createdMovie.Id}");
    
    Assert.Equal(createdMovie.Title, retrievedMovie.Title);
    
    // Cleanup
    await _client.Delete($"/api/v1/movies/{createdMovie.Id}");
}
```

### Error Handling and Assertions

Implement comprehensive error handling and meaningful assertions:

```csharp
[Scenario]
public async Task CreateMovie_WithInvalidData_ReturnsValidationError()
{
    var invalidMovieJson = "{ \"title\": \"\", \"releaseYear\": -1 }";
    
    try
    {
        var result = await _client.CreateAsStringAsync(invalidMovieJson, "/api/v1/movies");
        Assert.Fail("Expected validation exception was not thrown");
    }
    catch (Exception ex)
    {
        // Verify the error response contains expected validation messages
        Assert.Contains("Title is required", ex.Message);
        Assert.Contains("Release year must be positive", ex.Message);
    }
}
```

### Performance Considerations

**Parallel Test Execution:**

Be cautious with parallel test execution when tests share database state:

```csharp
// In your test project file (.csproj)
<PropertyGroup>
    <ParallelizeTestCollections>false</ParallelizeTestCollections>
</PropertyGroup>
```

**Test Timeouts:**

Set appropriate timeouts for integration tests:

```csharp
[Scenario]
[Timeout(30000)] // 30 seconds
public async Task LongRunningOperation()
{
    // Test logic...
}
```

### Documentation and Maintainability

**Gherkin Feature Files:**

Write clear, business-readable scenarios:

```gherkin
Feature: Movie Management
    As a movie database administrator
    I want to manage movies through the API
    So that I can maintain an up-to-date movie catalog

Scenario: Create a new movie
    Given I have valid movie data
    When I send a POST request to create the movie
    Then the movie should be created successfully
    And the response should contain the movie details
    And I should be able to retrieve the movie by ID

Scenario: Update an existing movie
    Given a movie exists in the database
    When I send a PUT request with updated movie data
    Then the movie should be updated successfully
    And the response should reflect the changes
```

## Related Documentation

For more information on testing and API usage, refer to:

- [Testing Overview](/testing/overview.md) - Comprehensive testing strategy and guidelines
- [BDD Tests](/testing/bdd_tests.md) - Behavior-Driven Development test patterns
- [Movies API Reference](/api_reference/movies_api.md) - Complete API endpoint documentation

## Troubleshooting

### Common Issues

**Issue: Tests fail with "Connection refused"**

- **Cause**: API is not running or running on a different port
- **Solution**: Verify the API is running and `appsettings.test.json` has the correct URL

**Issue: Database connection errors**

- **Cause**: Incorrect connection string or database doesn't exist
- **Solution**: Verify SQL Server is running and the test database exists

**Issue: SpecFlow scenarios not discovered**

- **Cause**: SpecFlow extension not installed or feature files not set to "SpecFlowSingleFileGenerator"
- **Solution**: Install SpecFlow extension and regenerate feature file code-behind

**Issue: Deserialization errors**

- **Cause**: API response format doesn't match expected model
- **Solution**: Check test output logs for actual response content and update models accordingly

**Issue: Tests pass individually but fail when run together**

- **Cause**: Tests are not properly isolated and share state
- **Solution**: Implement proper cleanup and ensure each test creates its own test data