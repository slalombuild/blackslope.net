# Quick Start Guide

This guide will help you get started with the BlackSlope API by walking through your first API calls, exploring the interactive documentation, and performing common operations. By the end of this guide, you'll be able to interact with the Movies API and understand the core functionality of the application.

## Prerequisites

Before proceeding with this guide, ensure you have completed the [installation and setup](/getting_started/installation.md) steps, including:

- .NET 6.0 SDK installed
- SQL Server configured with the Movies database
- Database migrations applied
- Application successfully built

## Running Your First API Call

### Starting the Application

1. **Navigate to the project root directory** in your terminal or PowerShell:

```bash
cd /path/to/BlackSlope.NET
```

2. **Start the API application** using the .NET CLI:

```bash
dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj
```

3. **Verify the application is running**. You should see output similar to:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:55644
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

> **Note**: The default port is configured as `55644` in the `appsettings.json` file. If you need to change this, update the `BaseUrl` property in the configuration.

### Making a GET Request to Retrieve Movies

Once the application is running, you can make your first API call to retrieve all movies.

#### Using cURL

```bash
curl -X GET "http://localhost:55644/api/v1/movies" -H "accept: application/json"
```

#### Using PowerShell

```powershell
Invoke-RestMethod -Uri "http://localhost:55644/api/v1/movies" -Method Get -ContentType "application/json"
```

#### Using a REST Client (e.g., Postman, Insomnia)

- **Method**: GET
- **URL**: `http://localhost:55644/api/v1/movies`
- **Headers**: `Accept: application/json`

### Understanding the Response Format

A successful request will return a **200 OK** status code with a JSON array of movie objects:

```json
[
  {
    "id": 1,
    "title": "The Shawshank Redemption",
    "director": "Frank Darabont",
    "releaseYear": 1994,
    "genre": "Drama"
  },
  {
    "id": 2,
    "title": "The Godfather",
    "director": "Francis Ford Coppola",
    "releaseYear": 1972,
    "genre": "Crime"
  }
]
```

#### Response Structure

Each movie object in the response contains the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `id` | integer | Unique identifier for the movie |
| `title` | string | The movie's title |
| `director` | string | The director's name |
| `releaseYear` | integer | Year the movie was released |
| `genre` | string | Movie genre/category |

#### HTTP Status Codes

The Movies API uses standard HTTP status codes to indicate the success or failure of requests:

| Status Code | Description |
|-------------|-------------|
| 200 OK | Request succeeded, data returned |
| 201 Created | Resource successfully created |
| 204 No Content | Resource successfully deleted |
| 400 Bad Request | Invalid request data or validation failure |
| 401 Unauthorized | Authentication required (when enabled) |
| 404 Not Found | Requested resource doesn't exist |
| 500 Internal Server Error | Server-side error occurred |

> **Authentication Note**: The `MoviesController` currently has authentication disabled (see the commented `[Authorize]` attribute). Once authentication middleware is configured, you'll need to include a valid JWT token in the `Authorization` header for all requests.

## Exploring the Swagger UI

BlackSlope API includes comprehensive interactive API documentation powered by Swagger/OpenAPI, making it easy to explore and test endpoints without writing code.

### Accessing Swagger

1. **Ensure the application is running** (see [Starting the Application](#starting-the-application))

2. **Open your web browser** and navigate to:

```
http://localhost:55644/swagger
```

3. **The Swagger UI interface will load**, displaying all available API endpoints organized by controller.

### Swagger UI Features

#### Interactive Endpoint Testing

The Swagger UI provides a "Try it out" feature for each endpoint:

1. **Expand an endpoint** by clicking on it (e.g., `GET /api/v1/movies`)
2. **Click the "Try it out" button** in the top-right corner
3. **Enter any required parameters** (for endpoints that need them)
4. **Click "Execute"** to send the request
5. **View the response** including status code, headers, and body

#### Request/Response Schema Documentation

Each endpoint displays:

- **Request parameters**: Path parameters, query strings, and request body schemas
- **Response schemas**: Expected response structure for each status code
- **Example values**: Sample request and response payloads
- **Data types**: Detailed type information for all properties

#### Code Generation

Swagger UI can generate client code in multiple languages:

1. **Click on an endpoint** to expand it
2. **Look for the "Download" or code generation options** (availability depends on Swagger configuration)
3. **Select your preferred language/framework** to generate client code

### Understanding API Documentation

The Swagger documentation is generated from XML comments in the source code. Here's how the `MoviesController` endpoints are documented:

```csharp
/// <summary>
/// Return a list of all movies
/// </summary>
/// <remarks>
/// Use this operation to return a list of all movies
/// </remarks>
/// <response code="200">Returns a list of all movies</response>
/// <response code="401">Unauthorized</response>
/// <response code="500">Internal Server Error</response>
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[HttpGet]
[Route("api/v1/movies")]
public async Task<ActionResult<List<MovieViewModel>>> Get()
```

The `ProducesResponseType` attributes inform Swagger about possible response codes, while XML comments provide human-readable descriptions.

### Swagger Configuration

The Swagger integration is configured in the application startup. Key configuration settings are stored in `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "Swagger": {
      "Version": "1",
      "ApplicationName": "BlackSlope",
      "XmlFile": "BlackSlope.Api.xml"
    }
  }
}
```

For more details on customizing Swagger, see the [Swagger Integration](/features/swagger_integration.md) documentation.

## Common First Tasks

### Creating a New Movie

To add a new movie to the database, send a POST request to the movies endpoint.

#### Request Details

- **Method**: POST
- **URL**: `http://localhost:55644/api/v1/movies`
- **Headers**: `Content-Type: application/json`
- **Body**:

```json
{
  "title": "Inception",
  "director": "Christopher Nolan",
  "releaseYear": 2010,
  "genre": "Science Fiction"
}
```

#### Using cURL

```bash
curl -X POST "http://localhost:55644/api/v1/movies" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Inception",
    "director": "Christopher Nolan",
    "releaseYear": 2010,
    "genre": "Science Fiction"
  }'
```

#### Using PowerShell

```powershell
$body = @{
    title = "Inception"
    director = "Christopher Nolan"
    releaseYear = 2010
    genre = "Science Fiction"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:55644/api/v1/movies" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body
```

#### Expected Response

A successful creation returns **200 OK** with the newly created movie including its assigned ID:

```json
{
  "id": 3,
  "title": "Inception",
  "director": "Christopher Nolan",
  "releaseYear": 2010,
  "genre": "Science Fiction"
}
```

#### Validation

The API uses **FluentValidation** to validate incoming requests. The `CreateMovieRequest` is validated before processing:

```csharp
var request = new CreateMovieRequest { Movie = viewModel };

// validate request model
await _blackSlopeValidator.AssertValidAsync(request);
```

If validation fails, you'll receive a **400 Bad Request** response with details about the validation errors.

**Common validation errors:**

- Missing required fields (title, director)
- Invalid data types
- Business rule violations (e.g., release year in the future)

### Updating an Existing Movie

To modify an existing movie, send a PUT request with the movie's ID and updated data.

#### Request Details

- **Method**: PUT
- **URL**: `http://localhost:55644/api/v1/movies/{id}`
- **Headers**: `Content-Type: application/json`
- **Body**:

```json
{
  "id": 3,
  "title": "Inception",
  "director": "Christopher Nolan",
  "releaseYear": 2010,
  "genre": "Sci-Fi Thriller"
}
```

> **Note**: The ID can be provided in the URL path, the request body, or both. If provided in both locations, they must match. The controller handles this flexibility:

```csharp
public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
{
    Contract.Requires(viewModel != null);
    var request = new UpdateMovieRequest { Movie = viewModel, Id = id };

    await _blackSlopeValidator.AssertValidAsync(request);

    // id can be in URL, body, or both
    viewModel.Id = id ?? viewModel.Id;
    
    // ... rest of update logic
}
```

#### Using cURL

```bash
curl -X PUT "http://localhost:55644/api/v1/movies/3" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 3,
    "title": "Inception",
    "director": "Christopher Nolan",
    "releaseYear": 2010,
    "genre": "Sci-Fi Thriller"
  }'
```

#### Expected Response

A successful update returns **200 OK** with the updated movie:

```json
{
  "id": 3,
  "title": "Inception",
  "director": "Christopher Nolan",
  "releaseYear": 2010,
  "genre": "Sci-Fi Thriller"
}
```

### Deleting a Movie

To remove a movie from the database, send a DELETE request with the movie's ID.

#### Request Details

- **Method**: DELETE
- **URL**: `http://localhost:55644/api/v1/movies/{id}`

#### Using cURL

```bash
curl -X DELETE "http://localhost:51385/api/v1/movies/3"
```

#### Using PowerShell

```powershell
Invoke-RestMethod -Uri "http://localhost:55644/api/v1/movies/3" -Method Delete
```

#### Expected Response

A successful deletion returns **204 No Content** with an empty response body:

```csharp
[ProducesResponseType(StatusCodes.Status204NoContent)]
[HttpDelete]
[Route("api/v1/movies/{id}")]
public async Task<ActionResult<DeletedMovieResponse>> Delete(int id)
{
    // delete existing movie
    await _movieService.DeleteMovieAsync(id);

    // 204 response
    return HandleDeletedResponse();
}
```

The `HandleDeletedResponse()` method from the `BaseController` returns a 204 status with no content, following REST best practices for DELETE operations.

### Retrieving a Single Movie

To fetch details for a specific movie, use the GET endpoint with an ID parameter.

#### Request Details

- **Method**: GET
- **URL**: `http://localhost:55644/api/v1/movies/{id}`

#### Using cURL

```bash
curl -X GET "http://localhost:51385/api/v1/movies/1" -H "accept: application/json"
```

#### Expected Response

Returns **200 OK** with the movie details:

```json
{
  "id": 1,
  "title": "The Shawshank Redemption",
  "director": "Frank Darabont",
  "releaseYear": 1994,
  "genre": "Drama"
}
```

If the movie doesn't exist, the service layer will handle the error appropriately (typically returning a 404 Not Found).

## Checking Health Endpoints

BlackSlope API includes comprehensive health check endpoints for monitoring application and database health. This is essential for production deployments and container orchestration.

### Available Health Check Endpoints

The application exposes several health check endpoints configured in `HealthCheckStartup.cs`:

| Endpoint | Description |
|----------|-------------|
| `/health` | Overall application health (all checks) |
| `/health/movies` | Movies-specific health checks |
| `/health/database` | Database connectivity checks |
| `/health/api` | API-level health checks |

### Accessing the Main Health Endpoint

```bash
curl -X GET "http://localhost:55644/health"
```

#### Response Format

Health check responses are returned in JSON format:

```json
{
  "status": "Healthy",
  "details": [
    {
      "key": "MOVIES.DB",
      "value": "Healthy",
      "description": null,
      "duration": "00:00:00.0234567",
      "exception": null
    },
    {
      "key": "MOVIES.API",
      "value": "Healthy",
      "description": "Movies API is operational",
      "duration": "00:00:00.0012345",
      "exception": null
    }
  ]
}
```

#### Health Status Values

| Status | Description |
|--------|-------------|
| `Healthy` | All checks passed successfully |
| `Degraded` | Some checks passed with warnings |
| `Unhealthy` | One or more critical checks failed |

### Database Health Check

The SQL Server health check verifies database connectivity:

```csharp
services.AddHealthChecks()
    .AddSqlServer(
        config.MoviesConnectionString, 
        name: "MOVIES.DB", 
        tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Database }
    )
```

To check only database health:

```bash
curl -X GET "http://localhost:55644/health/database"
```

### Custom Health Checks

The application includes a custom `MoviesHealthCheck` for API-specific validation:

```csharp
.AddCheck<MoviesHealthCheck>(
    "MOVIES.API", 
    tags: new[] { HealthCheckTag.Movies, HealthCheckTag.Api }
)
```

### Health Check Configuration

Health check endpoints are configured in `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "HealthChecks": {
      "Endpoint": "/health"
    }
  }
}
```

### Using Health Checks in Production

Health checks are particularly useful for:

- **Container orchestration**: Kubernetes liveness and readiness probes
- **Load balancers**: Determining if an instance should receive traffic
- **Monitoring systems**: Alerting on application health degradation
- **CI/CD pipelines**: Verifying deployment success

For more information on configuring and extending health checks, see the [Health Checks](/features/health_checks.md) documentation.

## Architecture Overview

Understanding the application's architecture will help you work more effectively with the API.

### Layered Architecture

BlackSlope follows a clean, layered architecture pattern:

```
┌─────────────────────────────────────┐
│   Controllers (API Layer)           │  ← HTTP endpoints, request/response handling
├─────────────────────────────────────┤
│   Services (Business Logic)         │  ← Domain logic, orchestration
├─────────────────────────────────────┤
│   Repositories (Data Access)        │  ← Database operations, EF Core
├─────────────────────────────────────┤
│   Database (SQL Server)             │  ← Data persistence
└─────────────────────────────────────┘
```

### Request Flow

A typical request follows this flow:

1. **HTTP Request** arrives at the controller
2. **Validation** occurs using FluentValidation
3. **Mapping** from ViewModel to DomainModel using AutoMapper
4. **Service Layer** executes business logic
5. **Repository Layer** performs database operations via Entity Framework Core
6. **Response Mapping** from DomainModel back to ViewModel
7. **HTTP Response** returned to client

Example from `MoviesController.Post`:

```csharp
public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
{
    var request = new CreateMovieRequest { Movie = viewModel };

    // 1. Validate request model
    await _blackSlopeValidator.AssertValidAsync(request);

    // 2. Map view model to domain model
    var movie = _mapper.Map<MovieDomainModel>(viewModel);

    // 3. Create new movie (service layer)
    var createdMovie = await _movieService.CreateMovieAsync(movie);

    // 4. Prepare response (map back to view model)
    var response = _mapper.Map<MovieViewModel>(createdMovie);

    // 5. Return 201 response
    return HandleCreatedResponse(response);
}
```

### Key Design Patterns

- **Repository Pattern**: Abstracts data access logic
- **Dependency Injection**: All dependencies injected via constructor
- **AutoMapper**: Separates internal domain models from API contracts
- **FluentValidation**: Declarative validation rules
- **Polly**: Resilience and transient fault handling for HTTP calls

## Troubleshooting Common Issues

### Connection String Issues

If you encounter database connection errors:

1. **Verify SQL Server is running**
2. **Check the connection string** in `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "MoviesConnectionString": "data source=.,1433;initial catalog=movies;Integrated Security=true;MultipleActiveResultSets=True;"
  }
}
```

3. **Update the server name** (`.` refers to localhost)
4. **Ensure the `movies` database exists**
5. **Verify authentication method** (Integrated Security vs SQL authentication)

### Port Already in Use

If port 55644 is already in use:

1. **Change the port** in `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "BaseUrl": "http://localhost:NEWPORT"
  }
}
```

2. **Update Swagger URL** accordingly when accessing the UI

### Validation Errors

If you receive 400 Bad Request responses:

1. **Check the response body** for validation error details
2. **Ensure all required fields** are provided
3. **Verify data types** match the expected schema
4. **Review validation rules** in the request validators

### Swagger Not Loading

If Swagger UI doesn't load:

1. **Verify the application is running**
2. **Check the Swagger configuration** in `appsettings.json`
3. **Ensure XML documentation file** is being generated (check project properties)
4. **Clear browser cache** and try again

## Next Steps

Now that you've completed the quick start guide, you can:

- **Explore the complete API reference** at [Movies API Documentation](/api_reference/movies_api.md)
- **Learn about advanced features** like Polly resilience patterns and health monitoring
- **Review the Swagger integration** for API documentation best practices at [Swagger Integration](/features/swagger_integration.md)
- **Understand health check implementation** at [Health Checks](/features/health_checks.md)
- **Set up authentication** by configuring the Azure AD middleware (currently disabled)

For installation and environment setup details, refer to the [Installation Guide](/getting_started/installation.md).