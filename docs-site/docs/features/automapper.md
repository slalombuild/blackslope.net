# AutoMapper Integration

AutoMapper is integrated into the BlackSlope API to provide automatic object-to-object mapping between different layers of the application. This documentation covers the configuration, usage patterns, and best practices for working with AutoMapper in this codebase.

## Mapper Configuration

### Registration and Setup

AutoMapper is registered in the `Startup.cs` file during service configuration. The registration uses assembly scanning to automatically discover and register all mapping profiles within specified assemblies.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // ... other service registrations
    
    services.AddAutoMapper(GetAssembliesToScanForMapperProfiles());
    
    // ... additional service registrations
}

// Make a list of assemblies in the solution which must be scanned for mapper profiles
private static IEnumerable<Assembly> GetAssembliesToScanForMapperProfiles() =>
    new Assembly[] { Assembly.GetExecutingAssembly() };
```

**Key Configuration Details:**

- **Version**: AutoMapper 10.1.1
- **Registration Method**: Assembly scanning via `AddAutoMapper()`
- **Scanned Assemblies**: Currently scans only the executing assembly (`BlackSlope.Api`)
- **Profile Discovery**: Automatically discovers all classes inheriting from `Profile`

### Extending Assembly Scanning

To include additional assemblies for profile scanning (e.g., if you create profiles in separate class libraries), modify the `GetAssembliesToScanForMapperProfiles()` method:

```csharp
private static IEnumerable<Assembly> GetAssembliesToScanForMapperProfiles() =>
    new Assembly[] 
    { 
        Assembly.GetExecutingAssembly(),
        typeof(SomeClassInAnotherAssembly).Assembly,
        Assembly.Load("AnotherAssemblyName")
    };
```

### Dependency Injection

Once registered, `IMapper` is available for dependency injection throughout the application:

```csharp
public class MoviesController : BaseController
{
    private readonly IMapper _mapper;
    
    public MoviesController(IMovieService movieService, IMapper mapper, IBlackSlopeValidator blackSlopeValidator)
    {
        _mapper = mapper;
        // ... other dependencies
    }
}
```

## Mapping Profiles

The application uses a layered architecture with distinct mapping profiles for each layer boundary. This approach maintains separation of concerns and allows for independent evolution of each layer.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    API Layer (Controllers)                   │
│                      ViewModels (DTOs)                       │
└──────────────────────┬──────────────────────────────────────┘
                       │ MovieResponseProfile
                       │ CreateMovieRequestProfile
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Service Layer                             │
│                    DomainModels                              │
└──────────────────────┬──────────────────────────────────────┘
                       │ MovieProfile
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                  Repository Layer                            │
│                    DtoModels                                 │
└─────────────────────────────────────────────────────────────┘
```

### Movie Request/Response Profiles

#### MovieResponseProfile

Maps between service layer domain models and API layer view models for response scenarios.

```csharp
using AutoMapper;
using BlackSlope.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Api.Operations.Movies.MapperProfiles
{
    public class MovieResponseProfile : Profile
    {
        public MovieResponseProfile()
        {
            CreateMap<MovieDomainModel, MovieViewModel>().ReverseMap();
        }
    }
}
```

**Usage Context:**
- **Source**: `MovieDomainModel` (Service Layer)
- **Destination**: `MovieViewModel` (API Layer)
- **Direction**: Bidirectional (`.ReverseMap()`)
- **Purpose**: Transforms service responses into API responses

**Applied In:**
- `GET /api/v1/movies` - List all movies
- `GET /api/v1/movies/{id}` - Get single movie
- `POST /api/v1/movies` - Return created movie
- `PUT /api/v1/movies/{id}` - Return updated movie

#### CreateMovieRequestProfile

Maps between API layer create request view models and service layer domain models.

```csharp
using AutoMapper;
using BlackSlope.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Api.Operations.Movies.MapperProfiles
{
    public class CreateMovieRequestProfile : Profile
    {
        public CreateMovieRequestProfile()
        {
            CreateMap<CreateMovieViewModel, MovieDomainModel>().ReverseMap();
        }
    }
}
```

**Usage Context:**
- **Source**: `CreateMovieViewModel` (API Layer)
- **Destination**: `MovieDomainModel` (Service Layer)
- **Direction**: Bidirectional
- **Purpose**: Transforms API create requests into service domain models

**Applied In:**
- `POST /api/v1/movies` - Create new movie

### Domain Model Mappings

#### MovieProfile

Maps between service layer domain models and repository layer DTOs.

```csharp
using AutoMapper;
using BlackSlope.Repositories.Movies.DtoModels;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Services.Movies.MapperProfiles
{
    public class MovieProfile : Profile
    {
        public MovieProfile()
        {
            CreateMap<MovieDomainModel, MovieDtoModel>().ReverseMap();
        }
    }
}
```

**Usage Context:**
- **Source**: `MovieDomainModel` (Service Layer)
- **Destination**: `MovieDtoModel` (Repository Layer)
- **Direction**: Bidirectional
- **Purpose**: Transforms between service and data access layers

**Applied In:**
- Service layer operations that interact with repositories
- Data retrieval and persistence operations

### Profile Naming Conventions

The codebase follows these naming conventions for mapping profiles:

| Profile Type | Naming Pattern | Example |
|-------------|----------------|---------|
| Response Mapping | `{Entity}ResponseProfile` | `MovieResponseProfile` |
| Request Mapping | `Create{Entity}RequestProfile` | `CreateMovieRequestProfile` |
| Domain Mapping | `{Entity}Profile` | `MovieProfile` |

## Usage Patterns

### Controller Mapping

Controllers use `IMapper` to transform data between API view models and service domain models.

#### GET Operations - Collection

```csharp
[HttpGet]
[Route("api/v1/movies")]
public async Task<ActionResult<List<MovieViewModel>>> Get()
{
    // Get all movies from service (returns List<MovieDomainModel>)
    var movies = await _movieService.GetAllMoviesAsync();

    // Map domain models to view models
    var response = _mapper.Map<List<MovieViewModel>>(movies);

    // Return 200 response
    return HandleSuccessResponse(response);
}
```

**Mapping Flow:**
1. Service returns `List<MovieDomainModel>`
2. AutoMapper transforms to `List<MovieViewModel>`
3. Controller returns transformed collection

#### GET Operations - Single Entity

```csharp
[HttpGet]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Get(int id)
{
    // Get single movie from service (returns MovieDomainModel)
    var movie = await _movieService.GetMovieAsync(id);

    // Map domain model to view model
    var response = _mapper.Map<MovieViewModel>(movie);

    // Return 200 response
    return HandleSuccessResponse(response);
}
```

**Mapping Flow:**
1. Service returns `MovieDomainModel`
2. AutoMapper transforms to `MovieViewModel`
3. Controller returns transformed entity

#### POST Operations - Create

```csharp
[HttpPost]
[Route("api/v1/movies")]
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };

    // Validate request model
    await _blackSlopeValidator.AssertValidAsync(request);

    // Map view model to domain model
    var movie = _mapper.Map<MovieDomainModel>(viewModel);

    // Create new movie (returns MovieDomainModel)
    var createdMovie = await _movieService.CreateMovieAsync(movie);

    // Map created domain model back to view model
    var response = _mapper.Map<MovieViewModel>(createdMovie);

    // Return 201 response
    return HandleCreatedResponse(response);
}
```

**Mapping Flow:**
1. Request body deserialized to `CreateMovieViewModel`
2. AutoMapper transforms to `MovieDomainModel` for service layer
3. Service returns created `MovieDomainModel`
4. AutoMapper transforms to `MovieViewModel` for response
5. Controller returns 201 Created with transformed entity

#### PUT Operations - Update

```csharp
[HttpPut]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
{
    Contract.Requires(viewModel != null);
    var request = new UpdateMovieRequest { Movie = viewModel, Id = id };

    await _blackSlopeValidator.AssertValidAsync(request);

    // ID can be in URL, body, or both - URL takes precedence
    viewModel.Id = id ?? viewModel.Id;

    // Map view model to domain model
    var movie = _mapper.Map<MovieDomainModel>(viewModel);

    // Update existing movie (returns MovieDomainModel)
    var updatedMovie = await _movieService.UpdateMovieAsync(movie);

    // Map updated domain model back to view model
    var response = _mapper.Map<MovieViewModel>(updatedMovie);

    // Return 200 response
    return HandleSuccessResponse(response);
}
```

**Mapping Flow:**
1. Request body deserialized to `MovieViewModel`
2. ID reconciliation (URL parameter takes precedence)
3. AutoMapper transforms to `MovieDomainModel` for service layer
4. Service returns updated `MovieDomainModel`
5. AutoMapper transforms to `MovieViewModel` for response
6. Controller returns 200 OK with transformed entity

### Service Layer Mapping

While not shown in the provided source files, the service layer would use `IMapper` to transform between domain models and repository DTOs:

```csharp
public class MovieService : IMovieService
{
    private readonly IMapper _mapper;
    private readonly IMovieRepository _repository;
    
    public async Task<MovieDomainModel> GetMovieAsync(int id)
    {
        // Get DTO from repository
        var movieDto = await _repository.GetByIdAsync(id);
        
        // Map DTO to domain model
        return _mapper.Map<MovieDomainModel>(movieDto);
    }
    
    public async Task<MovieDomainModel> CreateMovieAsync(MovieDomainModel movie)
    {
        // Map domain model to DTO
        var movieDto = _mapper.Map<MovieDtoModel>(movie);
        
        // Persist via repository
        var createdDto = await _repository.CreateAsync(movieDto);
        
        // Map created DTO back to domain model
        return _mapper.Map<MovieDomainModel>(createdDto);
    }
}
```

### Projection Queries

For Entity Framework Core queries, AutoMapper can be used with projection to optimize database queries:

```csharp
// Instead of loading full entities and mapping in memory
var movies = await _context.Movies.ToListAsync();
var viewModels = _mapper.Map<List<MovieViewModel>>(movies);

// Use ProjectTo for database-level projection
var viewModels = await _context.Movies
    .ProjectTo<MovieViewModel>(_mapper.ConfigurationProvider)
    .ToListAsync();
```

**Benefits:**
- Reduces data transfer from database
- Only selects required columns
- Improves query performance
- Reduces memory footprint

**Note:** The current codebase doesn't show `ProjectTo` usage, but it's a recommended pattern for read-heavy operations.

## Best Practices

### Profile Organization

#### Location Strategy

Organize mapping profiles close to the models they transform:

```
src/BlackSlope.Api/
├── Operations/
│   └── Movies/
│       ├── MapperProfiles/
│       │   ├── CreateMovieRequestProfile.cs
│       │   └── MovieResponseProfile.cs
│       └── ViewModels/
│           ├── CreateMovieViewModel.cs
│           └── MovieViewModel.cs
└── Services/
    └── Movies/
        ├── MapperProfiles/
        │   └── MovieProfile.cs
        └── DomainModels/
            └── MovieDomainModel.cs
```

**Rationale:**
- Profiles are located near the models they map
- Easy to find and maintain related mappings
- Clear ownership and responsibility boundaries

#### Single Responsibility

Each profile should handle mappings for a specific context or layer boundary:

```csharp
// ✅ GOOD: Focused profile for a specific mapping context
public class CreateMovieRequestProfile : Profile
{
    public CreateMovieRequestProfile()
    {
        CreateMap<CreateMovieViewModel, MovieDomainModel>().ReverseMap();
    }
}

// ❌ AVOID: Mixing multiple unrelated mappings
public class AllMovieMappingsProfile : Profile
{
    public AllMovieMappingsProfile()
    {
        CreateMap<CreateMovieViewModel, MovieDomainModel>();
        CreateMap<MovieDomainModel, MovieViewModel>();
        CreateMap<MovieDomainModel, MovieDtoModel>();
        CreateMap<ActorViewModel, ActorDomainModel>();
        CreateMap<DirectorViewModel, DirectorDomainModel>();
    }
}
```

#### Bidirectional Mappings

Use `.ReverseMap()` judiciously:

```csharp
// ✅ GOOD: When bidirectional mapping makes sense
CreateMap<MovieDomainModel, MovieViewModel>().ReverseMap();

// ⚠️ CAUTION: Consider explicit mappings when transformation logic differs
CreateMap<CreateMovieViewModel, MovieDomainModel>()
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));

CreateMap<MovieDomainModel, CreateMovieViewModel>()
    .ForMember(dest => dest.SomeCalculatedField, opt => opt.MapFrom(src => CalculateValue(src)));
```

### Testing Mappings

#### Configuration Validation

Add configuration validation tests to ensure all mappings are valid:

```csharp
[TestClass]
public class AutoMapperConfigurationTests
{
    [TestMethod]
    public void AutoMapper_Configuration_IsValid()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
        });

        // Act & Assert
        configuration.AssertConfigurationIsValid();
    }
}
```

#### Mapping Behavior Tests

Test specific mapping behaviors:

```csharp
[TestClass]
public class MovieResponseProfileTests
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MovieResponseProfile>();
        });
        _mapper = configuration.CreateMapper();
    }

    [TestMethod]
    public void Map_MovieDomainModel_To_MovieViewModel_Success()
    {
        // Arrange
        var domainModel = new MovieDomainModel
        {
            Id = 1,
            Title = "Test Movie",
            ReleaseYear = 2023
        };

        // Act
        var viewModel = _mapper.Map<MovieViewModel>(domainModel);

        // Assert
        Assert.AreEqual(domainModel.Id, viewModel.Id);
        Assert.AreEqual(domainModel.Title, viewModel.Title);
        Assert.AreEqual(domainModel.ReleaseYear, viewModel.ReleaseYear);
    }

    [TestMethod]
    public void Map_MovieViewModel_To_MovieDomainModel_Success()
    {
        // Arrange
        var viewModel = new MovieViewModel
        {
            Id = 1,
            Title = "Test Movie",
            ReleaseYear = 2023
        };

        // Act
        var domainModel = _mapper.Map<MovieDomainModel>(viewModel);

        // Assert
        Assert.AreEqual(viewModel.Id, domainModel.Id);
        Assert.AreEqual(viewModel.Title, domainModel.Title);
        Assert.AreEqual(viewModel.ReleaseYear, domainModel.ReleaseYear);
    }
}
```

### Performance Considerations

#### Avoid Mapping in Loops

```csharp
// ❌ BAD: Mapping inside loop
foreach (var movie in movies)
{
    var viewModel = _mapper.Map<MovieViewModel>(movie);
    results.Add(viewModel);
}

// ✅ GOOD: Map entire collection
var results = _mapper.Map<List<MovieViewModel>>(movies);
```

#### Use Projection for Large Datasets

```csharp
// ❌ LESS EFFICIENT: Load all data then map
var movies = await _context.Movies
    .Include(m => m.Director)
    .Include(m => m.Actors)
    .ToListAsync();
var viewModels = _mapper.Map<List<MovieViewModel>>(movies);

// ✅ MORE EFFICIENT: Project at database level
var viewModels = await _context.Movies
    .ProjectTo<MovieViewModel>(_mapper.ConfigurationProvider)
    .ToListAsync();
```

#### Reuse Mapper Instance

```csharp
// ✅ GOOD: Inject IMapper (singleton)
public class MoviesController : BaseController
{
    private readonly IMapper _mapper;
    
    public MoviesController(IMapper mapper)
    {
        _mapper = mapper;
    }
}

// ❌ BAD: Creating new mapper instances
public void SomeMethod()
{
    var config = new MapperConfiguration(cfg => cfg.AddProfile<MovieProfile>());
    var mapper = config.CreateMapper(); // Don't do this in production code
}
```

### Custom Mapping Logic

When simple property-to-property mapping isn't sufficient, use custom value resolvers or member configuration:

```csharp
public class MovieWithCalculationsProfile : Profile
{
    public MovieWithCalculationsProfile()
    {
        CreateMap<MovieDomainModel, MovieViewModel>()
            .ForMember(dest => dest.AgeInYears, 
                opt => opt.MapFrom(src => DateTime.Now.Year - src.ReleaseYear))
            .ForMember(dest => dest.DisplayTitle, 
                opt => opt.MapFrom(src => $"{src.Title} ({src.ReleaseYear})"))
            .ForMember(dest => dest.IsClassic, 
                opt => opt.MapFrom(src => DateTime.Now.Year - src.ReleaseYear > 25));
    }
}
```

### Handling Null Values

Configure null value handling at the profile level:

```csharp
public class MovieProfile : Profile
{
    public MovieProfile()
    {
        CreateMap<MovieDomainModel, MovieViewModel>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
```

## Common Pitfalls and Solutions

### Missing Profile Registration

**Problem:** Profiles not discovered during assembly scanning.

**Solution:** Ensure profiles are in assemblies included in `GetAssembliesToScanForMapperProfiles()`:

```csharp
private static IEnumerable<Assembly> GetAssembliesToScanForMapperProfiles() =>
    new Assembly[] 
    { 
        Assembly.GetExecutingAssembly(),
        typeof(MovieProfile).Assembly // Add assemblies containing profiles
    };
```

### Circular References

**Problem:** Mapping objects with circular references causes stack overflow.

**Solution:** Configure maximum depth or use `PreserveReferences`:

```csharp
CreateMap<MovieDomainModel, MovieViewModel>()
    .MaxDepth(3);
```

### Property Name Mismatches

**Problem:** Properties with different names don't map automatically.

**Solution:** Use explicit member mapping:

```csharp
CreateMap<MovieDomainModel, MovieViewModel>()
    .ForMember(dest => dest.YearReleased, opt => opt.MapFrom(src => src.ReleaseYear));
```

## Related Documentation

- [Services Architecture](/features/services.md) - Understanding the service layer where domain models are used
- [Controllers](/features/controllers.md) - Controller implementation patterns and view model usage
- [Dependency Injection](/architecture/dependency_injection.md) - How AutoMapper is registered and resolved

## Additional Resources

- **AutoMapper Documentation**: https://docs.automapper.org/
- **Version**: 10.1.1 (see [Technology Versions](#) for compatibility matrix)
- **NuGet Package**: AutoMapper