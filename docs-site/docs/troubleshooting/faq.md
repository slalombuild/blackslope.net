# Frequently Asked Questions

This document provides answers to common questions about the BlackSlope.NET reference architecture. For additional troubleshooting, see [Common Issues](/troubleshooting/common_issues.md).

## General Questions

### What is BlackSlope.NET?

BlackSlope.NET is a comprehensive .NET 6.0 reference architecture designed to provide a production-ready foundation for building enterprise-grade RESTful APIs. It demonstrates best practices for:

- **Clean Architecture**: Separation of concerns with distinct layers for API, Services, and Data Access
- **Modern Authentication**: Azure AD integration with JWT token support
- **Resilience Patterns**: Polly-based retry policies and circuit breakers
- **API Documentation**: Swagger/OpenAPI integration for interactive API exploration
- **Health Monitoring**: Built-in health checks for database connectivity and application status
- **Code Quality**: StyleCop and .NET Analyzers for consistent code standards
- **Containerization**: Docker support with Windows containers

The architecture includes a sample Movies API demonstrating CRUD operations, validation, mapping, and error handling patterns that can be adapted for your specific domain.

For more information, see the [Introduction](/getting_started/introduction.md) guide.

### Who should use this reference architecture?

BlackSlope.NET is designed for:

- **Development Teams** building new .NET 6.0 web APIs who want to start with proven patterns
- **Enterprise Organizations** requiring standardized architecture across multiple projects
- **Technical Leads** establishing architectural guidelines for their teams
- **Developers** learning modern .NET development practices and patterns

The architecture is particularly well-suited for teams working with:
- Microsoft Azure cloud infrastructure
- SQL Server databases
- Microservices architectures
- RESTful API development

### What are the system requirements?

**Development Environment:**
- **.NET 6.0 SDK** or later
- **SQL Server 2019 Developer Edition** (or SQL Server Express/Azure SQL Database)
- **Visual Studio 2022** or **Visual Studio Code** with C# extensions
- **Docker Desktop** (optional, for containerization)
- **Git** for version control

**Runtime Environment:**
- **.NET 6.0 Runtime** (ASP.NET Core)
- **SQL Server** (2016 or later)
- **Windows Server 2019+** or **Linux** (for Docker containers)

**Recommended Tools:**
- **Postman** or similar API testing tool
- **SQL Server Management Studio (SSMS)** or **Azure Data Studio**
- **PowerShell 7+** for running scripts

## Configuration Questions

### How do I configure Azure AD?

BlackSlope.NET supports Azure Active Directory authentication using both modern (`Azure.Identity`) and legacy (`Microsoft.IdentityModel.Clients.ActiveDirectory`) authentication libraries.

**Step 1: Register Application in Azure AD**

1. Navigate to Azure Portal → Azure Active Directory → App Registrations
2. Click "New registration"
3. Configure:
   - **Name**: Your application name (e.g., "BlackSlope API")
   - **Supported account types**: Choose based on your requirements
   - **Redirect URI**: `https://localhost:5001/signin-oidc` (for development)

**Step 2: Configure Application Settings**

In `appsettings.json`, add the Azure AD configuration:

```json
{
  "BlackSlope.Api": {
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "Domain": "yourdomain.onmicrosoft.com",
      "TenantId": "your-tenant-id",
      "ClientId": "your-client-id",
      "ClientSecret": "your-client-secret",
      "Audience": "api://your-client-id"
    }
  }
}
```

**Step 3: Enable Authentication in Controllers**

Currently, authentication is disabled by default. To enable it, uncomment the `[Authorize]` attribute in controllers:

```csharp
namespace BlackSlope.Api.Operations.Movies
{
    // TODO: enable this once authentication middleware has been configured
    [Authorize]  // <-- Uncomment this line
    public class MoviesController : BaseController
    {
        // Controller implementation
    }
}
```

**Step 4: Configure Startup**

The `Startup.cs` file includes the Azure AD configuration via the `AddAzureAd` extension method:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Other configurations...
    services.AddAzureAd(HostConfig.AzureAd);
    // Other configurations...
}
```

**Security Best Practices:**
- Never commit `ClientSecret` to source control
- Use **Azure Key Vault** or **User Secrets** for sensitive configuration
- For local development, use User Secrets:
  ```bash
  dotnet user-secrets set "BlackSlope.Api:AzureAd:ClientSecret" "your-secret"
  ```

### How do I change the database connection?

**Step 1: Update Connection String**

Modify the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=your-server;Database=your-database;User Id=your-user;Password=your-password;TrustServerCertificate=True;"
  }
}
```

**Connection String Formats:**

**Windows Authentication:**
```json
"MoviesConnectionString": "Server=localhost;Database=Movies;Integrated Security=True;TrustServerCertificate=True;"
```

**SQL Authentication:**
```json
"MoviesConnectionString": "Server=localhost,1433;Database=Movies;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;"
```

**Azure SQL Database:**
```json
"MoviesConnectionString": "Server=tcp:yourserver.database.windows.net,1433;Database=Movies;User ID=youradmin;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

**Step 2: Apply Database Migrations**

After changing the connection string, apply Entity Framework migrations:

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Navigate to the API project directory
cd src/BlackSlope.Api

# Apply migrations
dotnet ef database update
```

**Step 3: Verify Database Connection**

The application includes health checks for database connectivity. After starting the application, verify the connection:

```bash
curl http://localhost:51385/health
```

**Environment-Specific Configuration:**

For different environments, use environment-specific configuration files:

- `appsettings.Development.json` - Local development
- `appsettings.Staging.json` - Staging environment
- `appsettings.Production.json` - Production environment

**Docker Configuration:**

When running in Docker, override the connection string using environment variables:

```bash
docker run -e ConnectionStrings__MoviesConnectionString="Server=host.docker.internal;Database=Movies;..." blackslope.api
```

### How do I add new configuration settings?

BlackSlope.NET uses a strongly-typed configuration pattern with the `HostConfig` class.

**Step 1: Define Configuration Model**

Create a configuration class in `BlackSlope.Api.Common.Configuration`:

```csharp
namespace BlackSlope.Api.Common.Configuration
{
    public class EmailConfig
    {
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string FromAddress { get; set; }
        public bool EnableSsl { get; set; }
    }
}
```

**Step 2: Add to HostConfig**

Update the `HostConfig` class to include your new configuration:

```csharp
public class HostConfig
{
    public SwaggerConfig Swagger { get; set; }
    public AzureAdConfig AzureAd { get; set; }
    public EmailConfig Email { get; set; }  // Add your configuration
}
```

**Step 3: Add to appsettings.json**

Add the configuration section to `appsettings.json`:

```json
{
  "BlackSlope.Api": {
    "Swagger": { /* ... */ },
    "AzureAd": { /* ... */ },
    "Email": {
      "SmtpServer": "smtp.example.com",
      "Port": 587,
      "FromAddress": "noreply@example.com",
      "EnableSsl": true
    }
  }
}
```

**Step 4: Register and Inject Configuration**

The configuration is automatically registered in `Startup.cs`:

```csharp
private void ApplicationConfiguration(IServiceCollection services)
{
    services.AddSingleton(_ => _configuration);
    services.AddSingleton(_configuration
        .GetSection(Assembly.GetExecutingAssembly().GetName().Name)
        .Get<HostConfig>());
    
    var serviceProvider = services.BuildServiceProvider();
    HostConfig = serviceProvider.GetService<HostConfig>();
}
```

**Step 5: Use Configuration in Services**

Inject `HostConfig` into your services:

```csharp
public class EmailService : IEmailService
{
    private readonly EmailConfig _emailConfig;
    
    public EmailService(HostConfig hostConfig)
    {
        _emailConfig = hostConfig.Email;
    }
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Use _emailConfig.SmtpServer, _emailConfig.Port, etc.
    }
}
```

**Configuration Validation:**

Add validation in `Startup.cs` to ensure required configuration is present:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ApplicationConfiguration(services);
    
    // Validate configuration
    if (string.IsNullOrEmpty(HostConfig.Email?.SmtpServer))
    {
        throw new InvalidOperationException("Email configuration is missing or invalid");
    }
}
```

## Development Questions

### How do I add a new endpoint?

Follow the established pattern demonstrated in `MoviesController.cs`:

**Step 1: Create View Models**

Create request and response view models in `BlackSlope.Api.Operations.YourFeature.ViewModels`:

```csharp
namespace BlackSlope.Api.Operations.Products.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
    }
    
    public class CreateProductViewModel
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
    }
}
```

**Step 2: Create Request/Response Models**

Create request models in `BlackSlope.Api.Operations.YourFeature.Requests`:

```csharp
namespace BlackSlope.Api.Operations.Products.Requests
{
    public class CreateProductRequest
    {
        public CreateProductViewModel Product { get; set; }
    }
}
```

**Step 3: Create Validators**

Create FluentValidation validators:

```csharp
using FluentValidation;

namespace BlackSlope.Api.Operations.Products.Validators
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            RuleFor(x => x.Product).NotNull();
            RuleFor(x => x.Product.Name)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(x => x.Product.Price)
                .GreaterThan(0);
        }
    }
}
```

**Step 4: Create Controller**

Create a controller inheriting from `BaseController`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace BlackSlope.Api.Operations.Products
{
    [ApiController]
    public class ProductsController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly IProductService _productService;
        private readonly IBlackSlopeValidator _validator;
        
        public ProductsController(
            IProductService productService,
            IMapper mapper,
            IBlackSlopeValidator validator)
        {
            _productService = productService;
            _mapper = mapper;
            _validator = validator;
        }
        
        /// <summary>
        /// Create a new product
        /// </summary>
        /// <remarks>
        /// Use this operation to create a new product in the catalog
        /// </remarks>
        /// <response code="201">Product successfully created, will return the new product</response>
        /// <response code="400">Bad Request - validation failed</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        [Route("api/v1/products")]
        public async Task<ActionResult<ProductViewModel>> CreateProduct(
            [FromBody] CreateProductViewModel viewModel)
        {
            var request = new CreateProductRequest { Product = viewModel };
            
            // Validate request
            await _validator.AssertValidAsync(request);
            
            // Map to domain model
            var product = _mapper.Map<ProductDomainModel>(viewModel);
            
            // Create product
            var createdProduct = await _productService.CreateProductAsync(product);
            
            // Map to response
            var response = _mapper.Map<ProductViewModel>(createdProduct);
            
            // Return 201 Created
            return HandleCreatedResponse(response);
        }
    }
}
```

**Step 5: Register Validators**

Register your validators in the DI container (typically in an extension method):

```csharp
public static class ValidatorExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddTransient<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
        // Register other validators...
        return services;
    }
}
```

**Key Patterns:**
- Use `BaseController` for consistent response handling
- Always validate requests using `IBlackSlopeValidator`
- Use AutoMapper for object mapping
- Follow RESTful conventions for route naming
- Include comprehensive XML documentation for Swagger

### How do I create a new repository?

Follow the repository pattern established in the codebase:

**Step 1: Define Repository Interface**

```csharp
namespace BlackSlope.Repositories.Products
{
    public interface IProductRepository
    {
        Task<List<ProductEntity>> GetAllAsync();
        Task<ProductEntity> GetByIdAsync(int id);
        Task<ProductEntity> CreateAsync(ProductEntity product);
        Task<ProductEntity> UpdateAsync(ProductEntity product);
        Task DeleteAsync(int id);
    }
}
```

**Step 2: Create Entity Model**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlackSlope.Repositories.Products.Entities
{
    [Table("Products")]
    public class ProductEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
```

**Step 3: Implement Repository**

```csharp
using Microsoft.EntityFrameworkCore;

namespace BlackSlope.Repositories.Products
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        
        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<ProductEntity>> GetAllAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<ProductEntity> GetByIdAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        
        public async Task<ProductEntity> CreateAsync(ProductEntity product)
        {
            product.CreatedDate = DateTime.UtcNow;
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }
        
        public async Task<ProductEntity> UpdateAsync(ProductEntity product)
        {
            product.ModifiedDate = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return product;
        }
        
        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
```

**Step 4: Update DbContext**

Add the DbSet to your `ApplicationDbContext`:

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<MovieEntity> Movies { get; set; }
    public DbSet<ProductEntity> Products { get; set; }  // Add new DbSet
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entity relationships and constraints
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }
}
```

**Step 5: Register Repository**

Create an extension method for DI registration:

```csharp
public static class RepositoryExtensions
{
    public static IServiceCollection AddProductRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("MoviesConnectionString")));
        
        services.AddScoped<IProductRepository, ProductRepository>();
        
        return services;
    }
}
```

Register in `Startup.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Other configurations...
    services.AddProductRepository(_configuration);
}
```

**Step 6: Create Migration**

```bash
dotnet ef migrations add AddProductsTable --project src/BlackSlope.Api
dotnet ef database update --project src/BlackSlope.Api
```

**Best Practices:**
- Use `AsNoTracking()` for read-only queries to improve performance
- Always use async methods for database operations
- Handle null cases appropriately
- Use transactions for complex operations
- Consider implementing a generic repository base class for common operations

### How do I add custom middleware?

BlackSlope.NET includes examples of custom middleware for correlation IDs and exception handling.

**Step 1: Create Middleware Class**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlackSlope.Api.Common.Middleware.RequestLogging
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        
        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            
            _logger.LogInformation(
                "Request {Method} {Path} started at {StartTime}",
                context.Request.Method,
                context.Request.Path,
                startTime);
            
            try
            {
                await _next(context);
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;
                
                _logger.LogInformation(
                    "Request {Method} {Path} completed with {StatusCode} in {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds);
            }
        }
    }
}
```

**Step 2: Create Extension Method**

```csharp
using Microsoft.AspNetCore.Builder;

namespace BlackSlope.Api.Common.Middleware.RequestLogging
{
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
```

**Step 3: Register Middleware in Startup**

Add the middleware to the pipeline in `Startup.cs`:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }
    
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("AllowSpecificOrigin");
    
    // Add custom middleware
    app.UseRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    app.UseAuthentication();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

**Middleware Order Matters:**

The order of middleware registration is critical. Follow this general pattern:

1. Exception handling (should be first to catch all errors)
2. HTTPS redirection
3. Static files (if applicable)
4. Routing
5. CORS
6. Authentication
7. Authorization
8. Custom middleware (logging, correlation, etc.)
9. Endpoint routing

**Example: Correlation ID Middleware**

The existing `CorrelationIdMiddleware` demonstrates a complete implementation:

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);
        
        await _next(context);
    }
}
```

**Testing Middleware:**

Create integration tests for your middleware:

```csharp
[Fact]
public async Task RequestLoggingMiddleware_LogsRequestDetails()
{
    // Arrange
    var loggerMock = new Mock<ILogger<RequestLoggingMiddleware>>();
    var middleware = new RequestLoggingMiddleware(
        next: (innerHttpContext) => Task.CompletedTask,
        logger: loggerMock.Object);
    
    var context = new DefaultHttpContext();
    context.Request.Method = "GET";
    context.Request.Path = "/api/v1/products";
    
    // Act
    await middleware.InvokeAsync(context);
    
    // Assert
    loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("GET")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        Times.AtLeastOnce);
}
```

### How do I add a new validator?

BlackSlope.NET uses FluentValidation for request validation.

**Step 1: Create Validator Class**

```csharp
using FluentValidation;
using BlackSlope.Api.Operations.Products.Requests;

namespace BlackSlope.Api.Operations.Products.Validators
{
    public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
    {
        public CreateProductRequestValidator()
        {
            // Validate the Product property is not null
            RuleFor(x => x.Product)
                .NotNull()
                .WithMessage("Product information is required");
            
            // Validate Name
            RuleFor(x => x.Product.Name)
                .NotEmpty()
                .WithMessage("Product name is required")
                .MaximumLength(100)
                .WithMessage("Product name cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9\s\-]+$")
                .WithMessage("Product name contains invalid characters");
            
            // Validate Price
            RuleFor(x => x.Product.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero")
                .LessThan(1000000)
                .WithMessage("Price cannot exceed 1,000,000");
            
            // Validate Description (optional field)
            RuleFor(x => x.Product.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Product?.Description));
        }
    }
}
```

**Step 2: Complex Validation Rules**

For more complex scenarios:

```csharp
public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    private readonly IProductRepository _productRepository;
    
    public UpdateProductRequestValidator(IProductRepository productRepository)
    {
        _productRepository = productRepository;
        
        // Validate ID consistency
        RuleFor(x => x)
            .Must(HaveConsistentId)
            .WithMessage("Product ID in URL must match ID in request body");
        
        // Async validation - check if product exists
        RuleFor(x => x.Id)
            .MustAsync(ProductExists)
            .WithMessage("Product with specified ID does not exist");
        
        // Conditional validation
        RuleFor(x => x.Product.Price)
            .GreaterThan(x => x.Product.OriginalPrice * 0.5m)
            .When(x => x.Product.IsOnSale)
            .WithMessage("Sale price cannot be less than 50% of original price");
        
        // Custom validation
        RuleFor(x => x.Product)
            .Custom((product, context) =>
            {
                if (product.Price > 10000 && string.IsNullOrEmpty(product.ApprovalCode))
                {
                    context.AddFailure("ApprovalCode", 
                        "Approval code is required for products over $10,000");
                }
            });
    }
    
    private bool HaveConsistentId(UpdateProductRequest request)
    {
        return !request.Id.HasValue 
            || !request.Product.Id.HasValue 
            || request.Id == request.Product.Id;
    }
    
    private async Task<bool> ProductExists(int? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return false;
        var product = await _productRepository.GetByIdAsync(id.Value);
        return product != null;
    }
}
```

**Step 3: Register Validators**

Register validators in the DI container:

```csharp
public static class ValidatorExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        // Register FluentValidation
        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();
        
        // Or register individually
        services.AddTransient<IValidator<CreateProductRequest>, CreateProductRequestValidator>();
        services.AddTransient<IValidator<UpdateProductRequest>, UpdateProductRequestValidator>();
        
        return services;
    }
}
```

**Step 4: Use Validator in Controller**

```csharp
[HttpPost]
[Route("api/v1/products")]
public async Task<ActionResult<ProductViewModel>> CreateProduct(
    [FromBody] CreateProductViewModel viewModel)
{
    var request = new CreateProductRequest { Product = viewModel };
    
    // Validate using BlackSlope validator
    await _blackSlopeValidator.AssertValidAsync(request);
    
    // Continue with business logic...
}
```

**Validation Patterns:**

| Pattern | Use Case | Example |
|---------|----------|---------|
| `NotNull()` / `NotEmpty()` | Required fields | `RuleFor(x => x.Name).NotEmpty()` |
| `Length()` / `MaximumLength()` | String length constraints | `RuleFor(x => x.Name).MaximumLength(100)` |
| `GreaterThan()` / `LessThan()` | Numeric ranges | `RuleFor(x => x.Price).GreaterThan(0)` |
| `Matches()` | Regex validation | `RuleFor(x => x.Email).Matches(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$")` |
| `Must()` | Custom synchronous validation | `RuleFor(x => x).Must(BeValid)` |
| `MustAsync()` | Custom asynchronous validation | `RuleFor(x => x.Id).MustAsync(ExistsInDatabase)` |
| `When()` | Conditional validation | `RuleFor(x => x.Price).GreaterThan(0).When(x => x.IsActive)` |
| `Custom()` | Complex multi-field validation | `RuleFor(x => x).Custom((obj, context) => { /* logic */ })` |

**Testing Validators:**

```csharp
[Fact]
public void CreateProductRequestValidator_ValidProduct_PassesValidation()
{
    // Arrange
    var validator = new CreateProductRequestValidator();
    var request = new CreateProductRequest
    {
        Product = new CreateProductViewModel
        {
            Name = "Test Product",
            Price = 99.99m,
            Description = "A test product"
        }
    };
    
    // Act
    var result = validator.Validate(request);
    
    // Assert
    Assert.True(result.IsValid);
}

[Fact]
public void CreateProductRequestValidator_InvalidPrice_FailsValidation()
{
    // Arrange
    var validator = new CreateProductRequestValidator();
    var request = new CreateProductRequest
    {
        Product = new CreateProductViewModel
        {
            Name = "Test Product",
            Price = -10m  // Invalid price
        }
    };
    
    // Act
    var result = validator.Validate(request);
    
    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "Product.Price");
}
```

## Deployment Questions

### How do I deploy to production?

**Prerequisites:**
- SQL Server database provisioned
- Azure resources configured (if using Azure)
- SSL certificate for HTTPS
- Environment-specific configuration files

**Step 1: Prepare Configuration**

Create `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "MoviesConnectionString": "Server=prod-server;Database=Movies;User Id=app_user;Password=***;Encrypt=True;"
  },
  "BlackSlope.Api": {
    "AzureAd": {
      "Instance": "https://login.microsoftonline.com/",
      "TenantId": "production-tenant-id",
      "ClientId": "production-client-id"
    },
    "Swagger": {
      "Enabled": false  // Disable Swagger in production
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

**Step 2: Build for Production**

```bash
# Clean previous builds
dotnet clean src/BlackSlope.Api/BlackSlope.Api.csproj

# Build in Release mode
dotnet build src/BlackSlope.Api/BlackSlope.Api.csproj -c Release

# Publish application
dotnet publish src/BlackSlope.Api/BlackSlope.Api.csproj \
    -c Release \
    -o ./publish \
    --no-restore
```

**Step 3: Database Migration**

Apply migrations to production database:

```bash
# Generate SQL script for review
dotnet ef migrations script \
    --project src/BlackSlope.Api \
    --output migrations.sql \
    --idempotent

# Review the script, then apply to production database
# Option 1: Using dotnet ef (requires connection from deployment machine)
dotnet ef database update --project src/BlackSlope.Api

# Option 2: Execute SQL script manually using SSMS or Azure Data Studio
```

**Step 4: Deploy to IIS (Windows Server)**

1. Install prerequisites:
   - .NET 6.0 Hosting Bundle
   - IIS with ASP.NET Core Module

2. Create IIS Application Pool:
   - .NET CLR Version: No Managed Code
   - Managed Pipeline Mode: Integrated
   - Identity: ApplicationPoolIdentity (or custom service account)

3. Create IIS Website:
   - Physical path: Point to publish folder
   - Binding: Configure HTTPS with SSL certificate
   - Application Pool: Select created pool

4. Configure `web.config` (auto-generated during publish):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\BlackSlope.Api.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

**Step 5: Deploy to Azure App Service**

```bash
# Login to Azure
az login

# Create resource group (if not exists)
az group create --name blackslope-rg --location eastus

# Create App Service Plan
az appservice plan create \
    --name blackslope-plan \
    --resource-group blackslope-rg \
    --sku B1 \
    --is-linux

# Create Web App
az webapp create \
    --name blackslope-api \
    --resource-group blackslope-rg \
    --plan blackslope-plan \
    --runtime "DOTNET|6.0"

# Configure connection string
az webapp config connection-string set \
    --name blackslope-api \
    --resource-group blackslope-rg \
    --connection-string-type SQLAzure \
    --settings MoviesConnectionString="Server=..."

# Deploy application
az webapp deployment source config-zip \
    --name blackslope-api \
    --resource-group blackslope-rg \
    --src ./publish.zip
```

**Step 6: Verify Deployment**

```bash
# Check health endpoint
curl https://your-domain.com/health

# Expected response:
# {
#   "status": "Healthy",
#   "results": {
#     "database": "Healthy"
#   }
# }
```

**Production Checklist:**

- [ ] Environment variables configured
- [ ] Connection strings secured (use Azure Key Vault or similar)
- [ ] HTTPS enforced
- [ ] Swagger disabled
- [ ] Logging configured (Application Insights, Serilog, etc.)
- [ ] Health checks enabled
- [ ] Database migrations applied
- [ ] Authentication configured
- [ ] CORS policies restricted
- [ ] Rate limiting configured
- [ ] Monitoring and alerting set up

### How do I configure Docker?

BlackSlope.NET includes Docker support with a multi-stage Dockerfile.

**Understanding the Dockerfile:**

```dockerfile
# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY . .
WORKDIR /BlackSlope.Api
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app

# Stage 2: Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
```

**Build Docker Image:**

```bash
# Navigate to src directory
cd src

# Build image
docker build -t blackslope.api:latest -f Dockerfile .

# Verify image was created
docker images | grep blackslope
```

**Run Container Locally:**

```bash
# Run with environment variables
docker run -d \
    --name blackslope-container \
    -p 8080:80 \
    -p 8443:443 \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -e ConnectionStrings__MoviesConnectionString="Server=host.docker.internal;Database=Movies;..." \
    blackslope.api:latest

# View logs
docker logs blackslope-container

# Stop container
docker stop blackslope-container

# Remove container
docker rm blackslope-container
```

**Docker Compose Configuration:**

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  api:
    image: blackslope.api:latest
    build:
      context: ./src
      dockerfile: Dockerfile
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80;https://+:443
      - ConnectionStrings__MoviesConnectionString=Server=sqlserver;Database=Movies;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;
    depends_on:
      - sqlserver
    networks:
      - blackslope-network

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql
    networks:
      - blackslope-network

networks:
  blackslope-network:
    driver: bridge

volumes:
  sqlserver-data:
```

**Run with Docker Compose:**

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f api

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

**Optimize Docker Image:**

Create `.dockerignore`:

```
**/bin
**/obj
**/out
**/.vs
**/.vscode
**/*.user
**/.git
**/node_modules
**/TestResults
**/*.md
```

**Multi-Architecture Build:**

```bash
# Build for multiple platforms
docker buildx build \
    --platform linux/amd64,linux/arm64 \
    -t blackslope.api:latest \
    -f src/Dockerfile \
    src/
```

**Push to Container Registry:**

```bash
# Azure Container Registry
az acr login --name myregistry
docker tag blackslope.api:latest myregistry.azurecr.io/blackslope.api:latest
docker push myregistry.azurecr.io/blackslope.api:latest

# Docker Hub
docker login
docker tag blackslope.api:latest username/blackslope.api:latest
docker push username/blackslope.api:latest
```

### How do I set up Kubernetes?

**Prerequisites:**
- Kubernetes cluster (AKS, EKS, GKE, or local with Minikube/Docker Desktop)
- kubectl CLI installed
- Docker image pushed to container registry

**Step 1: Create Kubernetes Manifests**

Create `k8s/deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: blackslope-api
  labels:
    app: blackslope-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: blackslope-api
  template:
    metadata:
      labels:
        app: blackslope-api
    spec:
      containers:
      - name: api
        image: myregistry.azurecr.io/blackslope.api:latest
        ports:
        - containerPort: 80
          name: http
        - containerPort: 443
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__MoviesConnectionString
          valueFrom:
            secretKeyRef:
              name: blackslope-secrets
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
```

Create `k8s/service.yaml`:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: blackslope-api-service
spec:
  type: LoadBalancer
  selector:
    app: blackslope-api
  ports:
  - name: http
    protocol: TCP
    port: 80
    targetPort: 80
  - name: https
    protocol: TCP
    port: 443
    targetPort: 443
```

Create `k8s/secrets.yaml`:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: blackslope-secrets
type: Opaque
stringData:
  connection-string: "Server=sql-server;Database=Movies;User Id=sa;Password=YourPassword123;"
```

Create `k8s/configmap.yaml`:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: blackslope-config
data:
  appsettings.Production.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information"
        }
      }
    }
```

**Step 2: Deploy to Kubernetes**

```bash
# Create namespace
kubectl create namespace blackslope

# Apply secrets (do this first)
kubectl apply -f k8s/secrets.yaml -n blackslope

# Apply ConfigMap
kubectl apply -f k8s/configmap.yaml -n blackslope

# Deploy application
kubectl apply -f k8s/deployment.yaml -n blackslope

# Create service
kubectl apply -f k8s/service.yaml -n blackslope

# Verify deployment
kubectl get pods -n blackslope
kubectl get services -n blackslope
```

**Step 3: Configure Ingress (Optional)**

Create `k8s/ingress.yaml`:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: blackslope-ingress
  annotations:
    kubernetes.io/ingress.class: nginx
    cert-manager.io/cluster-issuer: letsencrypt-prod
spec:
  tls:
  - hosts:
    - api.yourdomain.com
    secretName: blackslope-tls
  rules:
  - host: api.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: blackslope-api-service
            port:
              number: 80
```

**Step 4: Set Up Horizontal Pod Autoscaling**

Create `k8s/hpa.yaml`:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: blackslope-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: blackslope-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

**Step 5: Monitor Deployment**

```bash
# Watch pod status
kubectl get pods -n blackslope -w

# View logs
kubectl logs -f deployment/blackslope-api -n blackslope

# Describe pod for troubleshooting
kubectl describe pod <pod-name> -n blackslope

# Check service endpoints
kubectl get endpoints -n blackslope

# Port forward for local testing
kubectl port-forward service/blackslope-api-service 8080:80 -n blackslope
```

**Azure Kubernetes Service (AKS) Specific:**

```bash
# Create AKS cluster
az aks create \
    --resource-group blackslope-rg \
    --name blackslope-aks \
    --node-count 3 \
    --enable-addons monitoring \
    --generate-ssh-keys

# Get credentials
az aks get-credentials \
    --resource-group blackslope-rg \
    --name blackslope-aks

# Attach ACR to AKS
az aks update \
    --resource-group blackslope-rg \
    --name blackslope-aks \
    --attach-acr myregistry
```

**Helm Chart (Advanced):**

Create `helm/blackslope/Chart.yaml`:

```yaml
apiVersion: v2
name: blackslope-api
description: BlackSlope.NET API Helm Chart
version: 1.0.0
appVersion: "1.0.0"
```

Create `helm/blackslope/values.yaml`:

```yaml
replicaCount: 3

image:
  repository: myregistry.azurecr.io/blackslope.api
  tag: latest
  pullPolicy: IfNotPresent

service:
  type: LoadBalancer
  port: 80

ingress:
  enabled: true
  className: nginx
  hosts:
    - host: api.yourdomain.com
      paths:
        - path: /
          pathType: Prefix

resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 250m
    memory: 256Mi

autoscaling:
  enabled: true
  minReplicas: 2
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
```

Deploy with Helm:

```bash
# Install chart
helm install blackslope-api ./helm/blackslope -n blackslope

# Upgrade chart
helm upgrade blackslope-api ./helm/blackslope -n blackslope

# Uninstall
helm uninstall blackslope-api -n blackslope
```

## Testing Questions

### How do I run tests?

BlackSlope.NET supports multiple testing approaches.

**Run All Tests:**

```bash
# From solution root
dotnet test ./src/

# With detailed output
dotnet test ./src/ --verbosity detailed

# Generate code coverage
dotnet test ./src/ --collect:"XPlat Code Coverage"
```

**Run Specific Test Project:**

```bash
# Unit tests only
dotnet test ./src/BlackSlope.Api.Tests/BlackSlope.Api.Tests.csproj

# Integration tests
dotnet test ./src/BlackSlope.Api.IntegrationTests/BlackSlope.Api.IntegrationTests.csproj
```

**Run Tests by Category:**

```csharp
// Mark tests with categories
[Fact]
[Trait("Category", "Unit")]
public void MovieService_GetAllMovies_ReturnsMovies()
{
    // Test implementation
}

[Fact]
[Trait("Category", "Integration")]
public async Task MovieRepository_GetAllAsync_ReturnsFromDatabase()
{
    // Test implementation
}
```

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Exclude integration tests
dotnet test --filter "Category!=Integration"
```

**Visual Studio Test Explorer:**

1. Open Test Explorer: `Test` → `Test Explorer`
2. Build solution to discover tests
3. Run all tests or select specific tests
4. View test results and output

**VS Code:**

1. Install `.NET Core Test Explorer` extension
2. Tests appear in Test Explorer sidebar
3. Click play button to run tests

**Continuous Integration:**

Example GitHub Actions workflow:

```yaml
name: .NET Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 6.0.x
    
    - name: Restore dependencies
      run: dotnet restore ./src/
    
    - name: Build
      run: dotnet build ./src/ --no-restore
    
    - name: Test
      run: dotnet test ./src/ --no-build --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage
      uses: codecov/codecov-action@v2
```

### How do I write integration tests?

**Note:** As mentioned in the README, SpecFlow integration test projects have been removed from .NET 6 until SpecFlow adds full support. However, you can still write integration tests using xUnit and WebApplicationFactory.

**Step 1: Create Test Project**

```bash
dotnet new xunit -n BlackSlope.Api.IntegrationTests
cd BlackSlope.Api.IntegrationTests
dotnet add reference ../BlackSlope.Api/BlackSlope.Api.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

**Step 2: Create Test Fixture**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlackSlope.Api.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                // Add DbContext using in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });
                
                // Build service provider
                var sp = services.BuildServiceProvider();
                
                // Create scope and seed database
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    
                    db.Database.EnsureCreated();
                    SeedDatabase(db);
                }
            });
        }
        
        private void SeedDatabase(ApplicationDbContext context)
        {
            context.Movies.AddRange(
                new MovieEntity { Id = 1, Title = "Test Movie 1", Genre = "Action" },
                new MovieEntity { Id = 2, Title = "Test Movie 2", Genre = "Comedy" }
            );
            context.SaveChanges();
        }
    }
}
```

**Step 3: Write Integration Tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BlackSlope.Api.IntegrationTests
{
    public class MoviesControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        
        public MoviesControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }
        
        [Fact]
        public async Task GetAllMovies_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/movies");
            
            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        
        [Fact]
        public async Task GetAllMovies_ReturnsMoviesList()
        {
            // Act
            var movies = await _client.GetFromJsonAsync<List<MovieViewModel>>("/api/v1/movies");
            
            // Assert
            Assert.NotNull(movies);
            Assert.Equal(2, movies.Count);
        }
        
        [Fact]
        public async Task CreateMovie_WithValidData_ReturnsCreated()
        {
            // Arrange
            var newMovie = new CreateMovieViewModel
            {
                Title = "New Movie",
                Genre = "Drama",
                ReleaseYear = 2023
            };
            
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/movies", newMovie);
            
            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            
            var createdMovie = await response.Content.ReadFromJsonAsync<MovieViewModel>();
            Assert.NotNull(createdMovie);
            Assert.Equal("New Movie", createdMovie.Title);
        }
        
        [Fact]
        public async Task CreateMovie_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidMovie = new CreateMovieViewModel
            {
                Title = "", // Invalid: empty title
                Genre = "Drama"
            };
            
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/movies", invalidMovie);
            
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        
        [Fact]
        public async Task UpdateMovie_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var updatedMovie = new MovieViewModel
            {
                Id = 1,
                Title = "Updated Movie",
                Genre = "Thriller"
            };
            
            // Act
            var response = await _client.PutAsJsonAsync("/api/v1/movies/1", updatedMovie);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            var movie = await response.Content.ReadFromJsonAsync<MovieViewModel>();
            Assert.Equal("Updated Movie", movie.Title);
        }
        
        [Fact]
        public async Task DeleteMovie_ExistingMovie_ReturnsNoContent()
        {
            // Act
            var response = await _client.DeleteAsync("/api/v1/movies/1");
            
            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        
        [Fact]
        public async Task HealthCheck_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Healthy", content);
        }
    }
}
```

**Step 4: Test with Real Database (Optional)**

```csharp
public class DatabaseIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    
    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=BlackSlopeTest;Trusted_Connection=True;")
            .Options;
        
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }
    
    [Fact]
    public async Task MovieRepository_CreateAndRetrieve_Success()
    {
        // Arrange
        var repository = new MovieRepository(_context);
        var movie = new MovieEntity
        {
            Title = "Integration Test Movie",
            Genre = "Test"
        };
        
        // Act
        var created = await repository.CreateAsync(movie);
        var retrieved = await repository.GetByIdAsync(created.Id);
        
        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Integration Test Movie", retrieved.Title);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### How do I work with SpecFlow tests?

**Current Status:**

The project includes SpecFlow-based integration tests using **SpecFlow 3.9.40** with **xUnit** as the test runner. The integration test project (`BlackSlope.Api.IntegrationTests`) is configured for .NET 6.0 and includes:

- **SpecFlow** (3.9.40): BDD test framework
- **SpecFlow.xUnit** (3.9.40): xUnit integration for SpecFlow
- **xUnit** (2.4.1): Test runner
- **ReportPortal.SpecFlow** (3.2.1): Test reporting integration
- **AutoFixture** (4.17.0): Test data generation

**Running SpecFlow Tests:**

```bash
# Run all integration tests
dotnet test src/BlackSlope.Api.IntegrationTests/

# Run specific feature
dotnet test --filter "FullyQualifiedName~FeatureName"

# Generate SpecFlow reports
dotnet test --logger "ReportPortal"
```

**Creating New SpecFlow Tests:**

1. **Create Feature File** (`.feature`):

```gherkin
Feature: Movie Management
    As an API consumer
    I want to manage movies
    So that I can maintain a movie catalog

Scenario: Create a new movie
    Given I have a valid movie request
    When I post the movie to the API
    Then the response status should be 201
    And the movie should be created in the database
```

2. **Generate Step Definitions**:

Run the SpecFlow generator to create step definition stubs, then implement the steps in a step definition class.

**Alternative Testing Approach:**

For simpler integration tests without BDD syntax, use xUnit with descriptive test names:

```csharp
public class MovieCreationFeature
{
    [Fact]
    public async Task Given_ValidMovieData_When_CreatingMovie_Then_MovieIsCreated()
    {
        // Given
        var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        var movie = new CreateMovieViewModel
        {
            Title = "Test Movie",
            Genre = "Action"
        };
        
        // When
        var response = await client.PostAsJsonAsync("/api/v1/movies", movie);
        
        // Then
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

**Test Configuration:**

The integration test project includes `appsettings.test.json` for test-specific configuration. This file is automatically copied to the output directory during build.

For additional help, see [Common Issues](/troubleshooting/common_issues.md) or [Contributing Guide](/development/contributing.md).