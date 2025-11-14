# Authorization

## Authorization Overview

BlackSlope.Api implements a comprehensive authorization framework built on ASP.NET Core's authentication and authorization middleware. The application is designed to support multiple authorization strategies to provide flexible and granular access control across API endpoints.

### Role-Based Authorization

Role-based authorization (RBAC) allows access control based on user roles assigned within Azure Active Directory. This approach is suitable for scenarios where permissions are grouped by organizational roles (e.g., Administrator, User, Manager).

**Key Characteristics:**
- Users are assigned one or more roles in Azure AD
- Roles are included in the JWT token as claims
- Controllers or actions can require specific roles for access
- Simple to implement and understand for straightforward permission models

### Policy-Based Authorization

Policy-based authorization provides a more flexible and powerful approach than role-based authorization. Policies are defined in code and can evaluate multiple requirements, including roles, claims, and custom logic.

**Key Characteristics:**
- Policies are registered in the dependency injection container during startup
- Multiple requirements can be combined within a single policy
- Custom authorization handlers can implement complex business logic
- Policies can be reused across multiple controllers and actions
- Supports both synchronous and asynchronous evaluation

### Claims-Based Authorization

Claims-based authorization makes access decisions based on specific claims present in the user's identity token. Claims represent facts about the user (e.g., email, department, subscription level) and are issued by the identity provider (Azure AD).

**Key Characteristics:**
- Fine-grained access control based on user attributes
- Claims are included in JWT tokens issued by Azure AD
- Can be combined with policies for complex authorization scenarios
- Supports custom claim types specific to your application domain

## Controller Authorization

### [Authorize] Attribute

The `[Authorize]` attribute is the primary mechanism for protecting API endpoints in BlackSlope.Api. This attribute can be applied at both the controller and action levels to enforce authentication and authorization requirements.

**Current Implementation Status:**

```csharp
// TODO: enable this once authentication middleware has been configured
// [Authorize]
public class MoviesController : BaseController
{
    // Controller implementation
}
```

As shown in the `MoviesController.cs`, authorization is currently **disabled** throughout the application. The `[Authorize]` attribute is commented out pending proper authentication middleware configuration.

### Role Requirements

Role-based authorization can be implemented by specifying required roles in the `[Authorize]` attribute:

```csharp
// Require a single role
[Authorize(Roles = "Administrator")]
public class AdminController : BaseController
{
    // Only users with the Administrator role can access these endpoints
}

// Require multiple roles (user must have ALL specified roles)
[Authorize(Roles = "Administrator,Manager")]
public IActionResult SensitiveOperation()
{
    // Implementation
}

// Require any of multiple roles (user must have AT LEAST ONE role)
[Authorize(Roles = "Administrator")]
[Authorize(Roles = "Manager")]
public IActionResult ModerateOperation()
{
    // Implementation
}
```

**Implementation Considerations:**
- Roles must be configured in Azure AD and included in the JWT token
- Role claims are typically included as `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`
- Multiple `[Authorize]` attributes with different roles create an OR condition
- Comma-separated roles within a single attribute create an AND condition

### Policy Requirements

Policy-based authorization provides more flexibility than simple role checks. Policies are defined during application startup and referenced by name in the `[Authorize]` attribute.

**Example Policy Configuration (to be added to Startup.cs):**

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Existing service configuration...
    
    services.AddAuthorization(options =>
    {
        // Simple policy requiring a specific claim
        options.AddPolicy("RequireEmployeeId", policy =>
            policy.RequireClaim("employee_id"));
        
        // Policy requiring specific role
        options.AddPolicy("RequireAdministrator", policy =>
            policy.RequireRole("Administrator"));
        
        // Policy with multiple requirements
        options.AddPolicy("RequireManagerInFinance", policy =>
            policy.RequireRole("Manager")
                  .RequireClaim("department", "Finance"));
        
        // Policy with custom requirement
        options.AddPolicy("RequireActiveSubscription", policy =>
            policy.Requirements.Add(new ActiveSubscriptionRequirement()));
    });
    
    // Register custom authorization handlers
    services.AddSingleton<IAuthorizationHandler, ActiveSubscriptionHandler>();
}
```

**Using Policies in Controllers:**

```csharp
[Authorize(Policy = "RequireEmployeeId")]
public class EmployeeController : BaseController
{
    // All actions require the employee_id claim
}

[Authorize(Policy = "RequireManagerInFinance")]
public IActionResult ApproveExpense()
{
    // Only Finance managers can access this endpoint
}
```

**Custom Policy Handler Example:**

```csharp
public class ActiveSubscriptionRequirement : IAuthorizationRequirement
{
    // Marker class for the requirement
}

public class ActiveSubscriptionHandler : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    private readonly ISubscriptionService _subscriptionService;
    
    public ActiveSubscriptionHandler(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSubscriptionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (userId == null)
        {
            return;
        }
        
        var hasActiveSubscription = await _subscriptionService.IsSubscriptionActive(userId);
        
        if (hasActiveSubscription)
        {
            context.Succeed(requirement);
        }
    }
}
```

## Current Implementation

### Authentication Currently Disabled

**Critical Security Notice:** The BlackSlope.Api application currently has authentication and authorization **disabled** in production code. This is evident from multiple indicators in the codebase:

1. **Commented Authorization Attributes:**
   ```csharp
   // From MoviesController.cs
   // TODO: enable this once authentication middleware has been configured
   // [Authorize]
   public class MoviesController : BaseController
   ```

2. **Permissive CORS Configuration:**
   ```csharp
   // From Startup.cs
   services.AddCors(options =>
   {
       options.AddPolicy(
           "AllowSpecificOrigin",
           builder => builder.AllowAnyOrigin() // TODO: Replace with FE Service Host
               .AllowAnyHeader()
               .WithMethods("PUT", "POST", "OPTIONS", "GET", "DELETE"));
   });
   ```

3. **Authentication Middleware Configured but Not Enforced:**
   ```csharp
   // From Startup.cs Configure method
   app.UseAuthentication(); // Middleware is registered but no endpoints require it
   ```

**Current State:**
- All API endpoints are publicly accessible without authentication
- No authorization checks are performed on any operations
- CORS allows requests from any origin
- JWT token validation is configured but not enforced

### Planned Authorization Strategy

Based on the technology stack and existing configuration, the planned authorization strategy includes:

1. **Azure AD Integration:**
   - The application includes `Azure.Identity` (1.14.2) for modern Azure AD authentication
   - `Microsoft.IdentityModel.Clients.ActiveDirectory` (5.2.9) for legacy scenarios
   - JWT token handling via `System.IdentityModel.Tokens.Jwt` (7.7.1)

2. **Configuration Structure:**
   ```csharp
   // From AzureAdConfig.cs
   public class AzureAdConfig
   {
       public string AadInstance { get; set; }
       public string Tenant { get; set; }
       public string Audience { get; set; }
   }
   ```

3. **Service Registration:**
   ```csharp
   // From Startup.cs
   services.AddAzureAd(HostConfig.AzureAd);
   ```

**Implementation Roadmap:**

1. Configure Azure AD application registration
2. Update `appsettings.json` with Azure AD tenant and client information
3. Implement JWT bearer token validation
4. Uncomment `[Authorize]` attributes on controllers
5. Define authorization policies based on application requirements
6. Restrict CORS to specific frontend origins
7. Implement role and claim mappings from Azure AD
8. Add authorization unit and integration tests

### Security Considerations

**Immediate Risks:**
- **Data Exposure:** All endpoints are currently accessible without authentication, exposing potentially sensitive data
- **Unauthorized Modifications:** Create, Update, and Delete operations can be performed by anyone
- **No Audit Trail:** Without authentication, user actions cannot be tracked or audited
- **CORS Vulnerability:** `AllowAnyOrigin()` permits requests from any domain, enabling potential CSRF attacks

**Pre-Production Checklist:**
- [ ] Enable authentication middleware enforcement
- [ ] Uncomment and configure `[Authorize]` attributes on all controllers
- [ ] Configure Azure AD application registration
- [ ] Update CORS policy to whitelist specific frontend origins
- [ ] Implement proper error handling for 401 (Unauthorized) and 403 (Forbidden) responses
- [ ] Add authorization logging and monitoring
- [ ] Conduct security testing and penetration testing
- [ ] Review and document authorization policies
- [ ] Implement rate limiting to prevent abuse
- [ ] Configure HTTPS enforcement (already present via `app.UseHttpsRedirection()`)

**Defense in Depth:**
Even with authorization enabled, implement additional security layers:
- Input validation (already implemented via FluentValidation)
- SQL injection protection (provided by Entity Framework Core parameterized queries)
- Exception handling middleware (already implemented)
- Health checks for monitoring (already configured)
- Correlation IDs for request tracking (already implemented)

## Best Practices

### Principle of Least Privilege

The principle of least privilege dictates that users should have only the minimum permissions necessary to perform their job functions. This minimizes the potential damage from compromised accounts or insider threats.

**Implementation Guidelines:**

1. **Default Deny:**
   ```csharp
   // Apply [Authorize] at the controller level by default
   [Authorize]
   public class SecureController : BaseController
   {
       // All actions require authentication by default
       
       // Explicitly allow anonymous access only where necessary
       [AllowAnonymous]
       public IActionResult PublicEndpoint()
       {
           // Implementation
       }
   }
   ```

2. **Granular Permissions:**
   ```csharp
   [Authorize(Policy = "CanReadMovies")]
   [HttpGet]
   [Route("api/v1/movies")]
   public async Task<ActionResult<List<MovieViewModel>>> Get()
   {
       // Read-only operation requires read permission
   }
   
   [Authorize(Policy = "CanModifyMovies")]
   [HttpPost]
   [Route("api/v1/movies")]
   public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
   {
       // Write operation requires modify permission
   }
   
   [Authorize(Policy = "CanDeleteMovies")]
   [HttpDelete]
   [Route("api/v1/movies/{id}")]
   public async Task<ActionResult<DeletedMovieResponse>> Delete(int id)
   {
       // Delete operation requires explicit delete permission
   }
   ```

3. **Resource-Based Authorization:**
   For scenarios where authorization depends on the specific resource being accessed:
   
   ```csharp
   [Authorize]
   [HttpPut]
   [Route("api/v1/movies/{id}")]
   public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
   {
       var movie = await _movieService.GetMovieAsync(id.Value);
       
       // Check if user has permission to modify this specific movie
       var authorizationResult = await _authorizationService.AuthorizeAsync(
           User, movie, "CanModifyMovie");
       
       if (!authorizationResult.Succeeded)
       {
           return Forbid();
       }
       
       // Proceed with update
       var updatedMovie = await _movieService.UpdateMovieAsync(movie);
       return HandleSuccessResponse(_mapper.Map<MovieViewModel>(updatedMovie));
   }
   ```

### Authorization Policies

**Policy Design Principles:**

1. **Descriptive Naming:**
   Use clear, intention-revealing names for policies:
   ```csharp
   // Good
   options.AddPolicy("CanApproveExpenses", policy => /* ... */);
   
   // Avoid
   options.AddPolicy("Policy1", policy => /* ... */);
   ```

2. **Composable Policies:**
   Build complex policies from simpler requirements:
   ```csharp
   services.AddAuthorization(options =>
   {
       // Base policies
       options.AddPolicy("IsEmployee", policy =>
           policy.RequireClaim("employee_id"));
       
       options.AddPolicy("IsManager", policy =>
           policy.RequireRole("Manager"));
       
       // Composed policy
       options.AddPolicy("CanApproveExpenses", policy =>
       {
           policy.RequireRole("Manager");
           policy.RequireClaim("department");
           policy.Requirements.Add(new BudgetAuthorityRequirement(10000));
       });
   });
   ```

3. **Centralized Policy Configuration:**
   Create a dedicated configuration class for authorization policies:
   ```csharp
   public static class AuthorizationPolicies
   {
       public const string CanReadMovies = "CanReadMovies";
       public const string CanModifyMovies = "CanModifyMovies";
       public const string CanDeleteMovies = "CanDeleteMovies";
       public const string RequireAdministrator = "RequireAdministrator";
       
       public static void ConfigurePolicies(AuthorizationOptions options)
       {
           options.AddPolicy(CanReadMovies, policy =>
               policy.RequireAuthenticatedUser());
           
           options.AddPolicy(CanModifyMovies, policy =>
               policy.RequireRole("Editor", "Administrator"));
           
           options.AddPolicy(CanDeleteMovies, policy =>
               policy.RequireRole("Administrator"));
           
           options.AddPolicy(RequireAdministrator, policy =>
               policy.RequireRole("Administrator"));
       }
   }
   
   // In Startup.cs
   services.AddAuthorization(AuthorizationPolicies.ConfigurePolicies);
   ```

### Testing Authorization

**Unit Testing Authorization Logic:**

```csharp
[TestClass]
public class MovieControllerAuthorizationTests
{
    [TestMethod]
    public async Task Get_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var controller = CreateControllerWithoutAuthentication();
        
        // Act
        var result = await controller.Get();
        
        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
    }
    
    [TestMethod]
    public async Task Delete_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var controller = CreateControllerWithUserRole();
        
        // Act
        var result = await controller.Delete(1);
        
        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(ForbidResult));
    }
    
    [TestMethod]
    public async Task Delete_WithAdminRole_ReturnsNoContent()
    {
        // Arrange
        var controller = CreateControllerWithAdminRole();
        
        // Act
        var result = await controller.Delete(1);
        
        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(NoContentResult));
    }
}
```

**Integration Testing with Authentication:**

```csharp
[TestClass]
public class MovieControllerIntegrationTests
{
    private HttpClient _client;
    private string _validToken;
    
    [TestInitialize]
    public void Setup()
    {
        _client = CreateTestClient();
        _validToken = GetTestJwtToken();
    }
    
    [TestMethod]
    public async Task GetMovies_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _validToken);
        
        // Act
        var response = await _client.GetAsync("/api/v1/movies");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
    
    [TestMethod]
    public async Task GetMovies_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/movies");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

**Testing Custom Authorization Handlers:**

```csharp
[TestClass]
public class ActiveSubscriptionHandlerTests
{
    [TestMethod]
    public async Task HandleRequirement_WithActiveSubscription_Succeeds()
    {
        // Arrange
        var mockSubscriptionService = new Mock<ISubscriptionService>();
        mockSubscriptionService
            .Setup(s => s.IsSubscriptionActive(It.IsAny<string>()))
            .ReturnsAsync(true);
        
        var handler = new ActiveSubscriptionHandler(mockSubscriptionService.Object);
        var requirement = new ActiveSubscriptionRequirement();
        var user = CreateUserWithClaims("user123");
        var context = new AuthorizationHandlerContext(
            new[] { requirement }, user, null);
        
        // Act
        await handler.HandleAsync(context);
        
        // Assert
        Assert.IsTrue(context.HasSucceeded);
    }
}
```

**Authorization Testing Checklist:**
- [ ] Test unauthenticated access returns 401
- [ ] Test authenticated but unauthorized access returns 403
- [ ] Test each role has appropriate access
- [ ] Test policy requirements are enforced
- [ ] Test custom authorization handlers
- [ ] Test resource-based authorization
- [ ] Test authorization with expired tokens
- [ ] Test authorization with invalid tokens
- [ ] Test authorization with missing claims
- [ ] Test authorization edge cases and boundary conditions

## Related Documentation

For additional information on related security and implementation topics, refer to:

- [Authentication](/security/authentication.md) - Detailed authentication configuration and JWT token handling
- [Controllers](/features/controllers.md) - Controller implementation patterns and best practices
- [Production Best Practices](/deployment/production_best_practices.md) - Security hardening and deployment guidelines

## Migration Path

To enable authorization in the BlackSlope.Api application, follow this migration path:

1. **Configure Azure AD** (see [Authentication](/security/authentication.md))
2. **Define Authorization Policies** in `Startup.cs`
3. **Uncomment `[Authorize]` Attributes** on controllers
4. **Add Policy-Specific Attributes** where needed
5. **Update CORS Configuration** to restrict origins
6. **Test Authorization** using the testing strategies above
7. **Monitor and Audit** authorization decisions in production