# Unit Testing

## Unit Test Structure

The BlackSlope.NET project follows industry-standard unit testing practices using **xUnit** as the testing framework, **Moq** for mocking dependencies, and **AutoFixture** for test data generation. The test project is organized to mirror the structure of the main application, making it easy to locate tests for specific components.

### Test Project Organization

The test project (`BlackSlope.Api.Tests`) is structured as follows:

```
BlackSlope.Api.Tests/
├── OperationsTests/
│   └── MoviesTests/
│       └── ValidatorsTests/
│           └── CreateMovieViewModelValidatorShould.cs
├── ServicesTests/
│   └── MoviesTests/
│       ├── MovieServiceTestsBase.cs
│       └── MovieService_CreateMovieShould.cs
└── BlackSlope.Api.Tests.csproj
```

**Key organizational principles:**

- **Feature-based grouping**: Tests are organized by feature area (e.g., `MoviesTests`) rather than by technical layer
- **Layer separation**: Tests are further subdivided by architectural layer (`OperationsTests`, `ServicesTests`)
- **Test base classes**: Shared test infrastructure is extracted into base classes (e.g., `MovieServiceTestsBase`)
- **Descriptive folder names**: Folder names clearly indicate what is being tested (e.g., `ValidatorsTests`)

### Naming Conventions

The project follows a consistent naming convention that makes test intent immediately clear:

**Test Class Naming Pattern:**
```
{ClassUnderTest}_{MethodUnderTest}Should
```

**Examples:**
```csharp
public class MovieService_CreateMovieShould : MovieServiceTestsBase
public class CreateMovieViewModelValidatorShould
```

**Test Method Naming Pattern:**
```
{Action}_{ExpectedOutcome}
```

**Examples:**
```csharp
[Fact]
public async Task CreateMovie_successfully()

[Fact]
public void Fail_when_title_is_null()

[Fact]
public void Pass_when_description_is_between_2_and_50_characters()
```

This naming convention provides several benefits:
- **Self-documenting**: Test names describe what is being tested and the expected behavior
- **Readable test output**: Failed tests clearly indicate what functionality is broken
- **Searchability**: Easy to locate tests for specific methods or scenarios

### Arrange-Act-Assert Pattern

All tests in the project follow the **Arrange-Act-Assert (AAA)** pattern, which provides a clear structure for test logic:

```csharp
[Fact]
public async Task CreateMovie_successfully()
{
    // Arrange - Set up test data and configure mocks
    var newMovieDomainModelRequest = _fixture.Create<MovieDomainModel>();
    _movieRepository.Setup(x => x.Create(It.IsAny<MovieDtoModel>()))
        .ReturnsAsync(_newMovieDto);

    // Act - Execute the method under test
    var result = await _service.CreateMovieAsync(newMovieDomainModelRequest);

    // Assert - Verify the expected outcome
    Assert.NotNull(result);
    Assert.IsType<MovieDomainModel>(result);
    Assert.Equal(_newMovieDomainModel, result);
}
```

**Pattern breakdown:**

1. **Arrange**: Configure test dependencies, create test data, and set up mock behaviors
2. **Act**: Execute the single method or operation being tested
3. **Assert**: Verify that the actual result matches expectations

## Testing Services

Service layer tests focus on business logic validation and ensure that services correctly orchestrate interactions between repositories, mappers, and other dependencies.

### MovieService Tests

The `MovieService` tests demonstrate comprehensive service layer testing with proper dependency isolation:

```csharp
public class MovieService_CreateMovieShould : MovieServiceTestsBase
{
    private readonly MovieDtoModel _newMovieDto;
    private readonly MovieDomainModel _newMovieDomainModel;

    public MovieService_CreateMovieShould()
    {
        // Arrange test data in constructor for reuse across test methods
        _newMovieDto = _fixture.Create<MovieDtoModel>();
        _newMovieDomainModel = _fixture.Create<MovieDomainModel>();

        // Configure mapper behavior for domain-to-DTO conversion
        _mapper.Setup(_ => _.Map<MovieDtoModel>(It.IsAny<MovieDomainModel>()))
            .Returns(_newMovieDto);

        // Configure mapper behavior for DTO-to-domain conversion
        _mapper.Setup(_ => _.Map<MovieDomainModel>(_newMovieDto))
            .Returns(_newMovieDomainModel);
    }

    [Fact]
    public async Task CreateMovie_successfully()
    {
        // Arrange
        var newMovieDomainModelRequest = _fixture.Create<MovieDomainModel>();
        _movieRepository.Setup(x => x.Create(It.IsAny<MovieDtoModel>()))
            .ReturnsAsync(_newMovieDto);

        // Act
        var result = await _service.CreateMovieAsync(newMovieDomainModelRequest);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MovieDomainModel>(result);
        Assert.Equal(_newMovieDomainModel, result);
    }
}
```

**Key testing considerations:**

- **Async/await support**: Tests properly handle asynchronous service methods using `async Task`
- **Type verification**: Tests verify both the type and content of returned objects
- **Mock verification**: Implicit verification that mocked methods are called with expected parameters

### Mocking Dependencies

The project uses **Moq** (version 4.16.1) for creating test doubles. Mocking is essential for isolating the unit under test from its dependencies:

```csharp
// Create mock instances
protected readonly Mock<IMovieRepository> _movieRepository;
protected readonly Mock<IFakeApiRepository> _fakeApiRepository;
protected Mock<IMapper> _mapper;

// Configure mock behavior
_movieRepository.Setup(x => x.Create(It.IsAny<MovieDtoModel>()))
    .ReturnsAsync(_newMovieDto);

// Use It.IsAny<T>() for flexible parameter matching
_mapper.Setup(_ => _.Map<MovieDtoModel>(It.IsAny<MovieDomainModel>()))
    .Returns(_newMovieDto);
```

**Mocking best practices demonstrated:**

1. **Interface-based mocking**: All dependencies are mocked via interfaces (`IMovieRepository`, `IMapper`)
2. **Flexible parameter matching**: `It.IsAny<T>()` allows tests to focus on behavior rather than exact parameter values
3. **Return value configuration**: `.Returns()` and `.ReturnsAsync()` specify mock return values
4. **Dependency injection**: Mocks are injected into the service constructor, mimicking production DI behavior

**Common mocking patterns:**

```csharp
// Return a specific value
_repository.Setup(x => x.GetById(123)).Returns(expectedEntity);

// Return different values based on input
_repository.Setup(x => x.GetById(It.Is<int>(id => id > 0)))
    .Returns(validEntity);

// Throw an exception
_repository.Setup(x => x.Delete(It.IsAny<int>()))
    .ThrowsAsync(new InvalidOperationException());

// Verify method was called
_repository.Verify(x => x.Create(It.IsAny<MovieDtoModel>()), Times.Once);
```

### Test Base Classes

Test base classes eliminate code duplication and provide consistent test infrastructure across related test classes:

```csharp
public class MovieServiceTestsBase
{
    protected readonly Fixture _fixture = new Fixture();
    protected Mock<IMapper> _mapper;
    protected readonly Mock<IMovieRepository> _movieRepository;
    protected readonly Mock<IFakeApiRepository> _fakeApiRepository;
    protected readonly IMovieService _service;

    public MovieServiceTestsBase()
    {
        // Initialize all mocks
        _movieRepository = new Mock<IMovieRepository>();
        _fakeApiRepository = new Mock<IFakeApiRepository>();
        _mapper = new Mock<IMapper>();
        
        // Create the service under test with mocked dependencies
        _service = new MovieService(
            _movieRepository.Object, 
            _fakeApiRepository.Object,
            _mapper.Object
        );
    }
}
```

**Benefits of test base classes:**

- **DRY principle**: Common setup code is written once and inherited by all test classes
- **Consistent initialization**: All tests start with the same baseline configuration
- **Easy maintenance**: Changes to service dependencies only require updates in one location
- **Protected members**: Test infrastructure is accessible to derived classes via `protected` access modifiers

**Usage pattern:**

```csharp
// Inherit from base class to get all setup automatically
public class MovieService_CreateMovieShould : MovieServiceTestsBase
{
    // Additional setup specific to this test class
    public MovieService_CreateMovieShould()
    {
        // Configure mocks for this specific test scenario
    }
    
    // Test methods have access to all protected members from base class
    [Fact]
    public async Task CreateMovie_successfully()
    {
        // _service, _movieRepository, _mapper, etc. are all available
    }
}
```

## Testing Validators

Validator tests ensure that input validation rules are correctly implemented and provide appropriate error messages. The project uses **FluentValidation** (version 10.3.6) with its built-in test helpers.

### Validator Unit Tests

The `CreateMovieViewModelValidatorShould` class demonstrates comprehensive validator testing:

```csharp
public class CreateMovieViewModelValidatorShould
{
    private readonly Fixture _fixture = new Fixture();
    private readonly CreateMovieViewModel _modelViewModel;
    private readonly CreateMovieViewModelValidatorCollection _movieViewModelValidator;
    private readonly Mock<IMovieService> _movieService = new Mock<IMovieService>();

    public CreateMovieViewModelValidatorShould()
    {
        // Create a valid model using AutoFixture
        _modelViewModel = _fixture.Create<CreateMovieViewModel>();
        
        // Initialize the validator with mocked dependencies
        _movieViewModelValidator = new CreateMovieViewModelValidatorCollection(
            _movieService.Object
        );
    }

    [Fact]
    public void Fail_when_title_is_null()
    {
        // Arrange - Set property to invalid state
        _modelViewModel.Title = null;
        
        // Act - Execute validation
        var result = _movieViewModelValidator.TestValidate(_modelViewModel);
        
        // Assert - Verify specific validation error
        var failures = result
            .ShouldHaveValidationErrorFor(x => x.Title)
            .When(failure => MovieErrorCode.EmptyOrNullMovieTitle.Equals(failure.CustomState));
    }

    [Fact]
    public void Pass_when_title_is_between_2_and_50_characters()
    {
        // Arrange
        _modelViewModel.Title = "the post";
        
        // Act
        var result = _movieViewModelValidator.TestValidate(_modelViewModel);
        
        // Assert - Verify no validation errors
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }
}
```

### Testing Validation Rules

FluentValidation provides specialized test helpers that make validation testing more expressive and maintainable:

**Testing for validation failures:**

```csharp
[Fact]
public void Fail_when_description_is_null()
{
    _modelViewModel.Description = null;
    var result = _movieViewModelValidator.TestValidate(_modelViewModel);
    
    // Verify that a validation error exists for the Description property
    result
        .ShouldHaveValidationErrorFor(x => x.Description)
        .When(failure => MovieErrorCode.EmptyOrNullMovieDescription.Equals(failure.CustomState));
}
```

**Testing with multiple input values using Theory:**

```csharp
[Theory]
[InlineData("d")]  // Too short
[InlineData("A great movie title that is very thrilling but sadly too long.")]  // Too long
public void Fail_when_title_is_not_between_2_and_50_characters(string title)
{
    _modelViewModel.Title = title;
    var result = _movieViewModelValidator.TestValidate(_modelViewModel);
    
    result
        .ShouldHaveValidationErrorFor(x => x.Title.Length)
        .When(failure => MovieErrorCode.TitleNotBetween2and50Characters.Equals(failure.CustomState));
}
```

**Testing for validation success:**

```csharp
[Fact]
public void Pass_when_description_is_between_2_and_50_characters()
{
    _modelViewModel.Description = "Great movie";
    var result = _movieViewModelValidator.TestValidate(_modelViewModel);
    
    // Verify that NO validation errors exist for the Description property
    result.ShouldNotHaveValidationErrorFor(x => x.Description);
}
```

### FluentValidation Test Helpers

The project leverages FluentValidation's test extension methods for more readable and maintainable tests:

| Method | Purpose | Example |
|--------|---------|---------|
| `TestValidate()` | Executes validation and returns a result object | `var result = validator.TestValidate(model);` |
| `ShouldHaveValidationErrorFor()` | Asserts that a validation error exists for a specific property | `result.ShouldHaveValidationErrorFor(x => x.Title);` |
| `ShouldNotHaveValidationErrorFor()` | Asserts that no validation error exists for a specific property | `result.ShouldNotHaveValidationErrorFor(x => x.Title);` |
| `When()` | Filters validation errors based on a predicate | `.When(failure => errorCode.Equals(failure.CustomState))` |

**Custom error codes:**

The project uses custom error codes (via `CustomState`) to distinguish between different validation failures:

```csharp
result
    .ShouldHaveValidationErrorFor(x => x.Title)
    .When(failure => MovieErrorCode.EmptyOrNullMovieTitle.Equals(failure.CustomState));
```

This approach provides:
- **Precise error identification**: Tests can verify specific validation rules rather than just checking for any error
- **Better error messages**: Custom error codes can be mapped to user-friendly messages
- **API contract testing**: Ensures that API consumers receive consistent error codes

## Best Practices

### Test Isolation

Each test should be completely independent and not rely on the state or execution order of other tests:

**Achieved through:**

1. **Fresh instances per test**: xUnit creates a new instance of the test class for each test method
2. **Mock reset**: Each test configures its own mock behaviors
3. **AutoFixture data generation**: Each test gets unique test data

```csharp
public class MovieService_CreateMovieShould : MovieServiceTestsBase
{
    // Constructor runs before EACH test method
    public MovieService_CreateMovieShould()
    {
        // Fresh test data for each test
        _newMovieDto = _fixture.Create<MovieDtoModel>();
        _newMovieDomainModel = _fixture.Create<MovieDomainModel>();
        
        // Configure mocks specific to this test class
        _mapper.Setup(_ => _.Map<MovieDtoModel>(It.IsAny<MovieDomainModel>()))
            .Returns(_newMovieDto);
    }
}
```

**Anti-pattern to avoid:**

```csharp
// DON'T: Share mutable state between tests
private static MovieDomainModel _sharedModel;  // Static field shared across tests

[Fact]
public void Test1()
{
    _sharedModel.Title = "Test1";  // Modifies shared state
}

[Fact]
public void Test2()
{
    // This test's behavior depends on whether Test1 ran first
    Assert.Equal("Test1", _sharedModel.Title);  // Brittle!
}
```

### One Assertion Per Test

While not strictly enforced, tests should focus on verifying a single behavior or outcome:

**Good example - focused assertion:**

```csharp
[Fact]
public void Fail_when_title_is_null()
{
    _modelViewModel.Title = null;
    var result = _movieViewModelValidator.TestValidate(_modelViewModel);
    
    // Single logical assertion: title validation should fail
    result.ShouldHaveValidationErrorFor(x => x.Title)
        .When(failure => MovieErrorCode.EmptyOrNullMovieTitle.Equals(failure.CustomState));
}
```

**Acceptable - multiple assertions for the same logical outcome:**

```csharp
[Fact]
public async Task CreateMovie_successfully()
{
    var result = await _service.CreateMovieAsync(newMovieDomainModelRequest);
    
    // Multiple assertions verifying the same outcome (successful creation)
    Assert.NotNull(result);
    Assert.IsType<MovieDomainModel>(result);
    Assert.Equal(_newMovieDomainModel, result);
}
```

**Anti-pattern - testing multiple behaviors:**

```csharp
// DON'T: Test multiple unrelated behaviors in one test
[Fact]
public void Test_everything_about_movies()
{
    // Tests creation
    var created = _service.CreateMovieAsync(movie);
    Assert.NotNull(created);
    
    // Tests retrieval
    var retrieved = _service.GetMovieAsync(1);
    Assert.NotNull(retrieved);
    
    // Tests deletion
    _service.DeleteMovieAsync(1);
    // ... this should be 3 separate tests
}
```

### Descriptive Test Names

Test names should clearly communicate what is being tested and under what conditions:

**Naming formula:**
```
{Action}_{Condition}_{ExpectedResult}
```

**Examples from the codebase:**

```csharp
// Clear: What fails, why it fails
public void Fail_when_title_is_null()

// Clear: What passes, under what condition
public void Pass_when_title_is_between_2_and_50_characters()

// Clear: What succeeds, what operation
public async Task CreateMovie_successfully()
```

**Benefits:**

- **Self-documenting**: No need to read test code to understand intent
- **Test reports**: Failed tests immediately indicate what functionality is broken
- **Refactoring confidence**: Clear names help identify which tests need updating

### Test Data Builders

The project uses **AutoFixture** (version 4.17.0) to automatically generate test data, eliminating boilerplate and improving test maintainability:

```csharp
private readonly Fixture _fixture = new Fixture();

// Generate a complete object with all properties populated
var movie = _fixture.Create<MovieDomainModel>();

// Generate a collection
var movies = _fixture.CreateMany<MovieDomainModel>(5);

// Customize specific properties
var customMovie = _fixture.Build<MovieDomainModel>()
    .With(x => x.Title, "Specific Title")
    .Without(x => x.Description)  // Leave null
    .Create();
```

**Advantages of AutoFixture:**

1. **Reduced boilerplate**: No need to manually set every property
2. **Realistic data**: Generated values are varied and realistic
3. **Maintainability**: Tests don't break when new properties are added to models
4. **Focus on intent**: Only specify properties relevant to the test

**When to use AutoFixture vs. manual data:**

```csharp
// Use AutoFixture when property values don't matter
var anyMovie = _fixture.Create<MovieDomainModel>();

// Use manual data when specific values are important for the test
_modelViewModel.Title = null;  // Testing null validation
_modelViewModel.Title = "d";   // Testing minimum length validation
```

## Additional Testing Considerations

### Test Project Dependencies

The test project includes the following key packages:

```xml
<PackageReference Include="AutoFixture" Version="4.17.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
<PackageReference Include="Moq" Version="4.16.1" />
<PackageReference Include="xunit" Version="2.4.1" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.4.3" />
```

### Running Tests

Tests can be executed using multiple methods:

**Command line:**
```bash
# Run all tests in the solution
dotnet test ./src/

# Run tests in a specific project
dotnet test ./src/BlackSlope.Api.Tests/BlackSlope.Api.Tests.csproj

# Run with verbose output
dotnet test ./src/ --verbosity detailed
```

**Visual Studio:**
- Use Test Explorer to run individual tests or test groups
- Right-click on test methods or classes to run specific tests
- Use keyboard shortcuts (Ctrl+R, A for all tests)

### Code Coverage

While not explicitly configured in the provided files, consider adding code coverage tools:

```bash
# Install coverage tool
dotnet tool install --global dotnet-coverage

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Related Documentation

For more information on related topics, see:

- [Testing Overview](/testing/overview.md) - Comprehensive testing strategy and guidelines
- [Services](/features/services.md) - Service layer architecture and implementation details
- [Validation](/features/validation.md) - FluentValidation configuration and custom validators

### Common Pitfalls and Solutions

**Pitfall: Async void test methods**
```csharp
// DON'T: async void is not supported by xUnit
[Fact]
public async void CreateMovie_successfully()  // Wrong!

// DO: Use async Task
[Fact]
public async Task CreateMovie_successfully()  // Correct!
```

**Pitfall: Not verifying mock interactions**
```csharp
// Consider verifying that expected methods were called
_movieRepository.Verify(
    x => x.Create(It.IsAny<MovieDtoModel>()), 
    Times.Once
);
```

**Pitfall: Testing implementation details**
```csharp
// DON'T: Test internal implementation
Assert.Equal(3, service.InternalCounter);  // Brittle!

// DO: Test observable behavior
var result = await service.CreateMovieAsync(movie);
Assert.NotNull(result);  // Tests the contract
```