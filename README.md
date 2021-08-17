# BlackSlope.NET

## What is it?

You can read more in the following blog posts:

* [Introducing BlackSlope: A DotNet Core Reference Architecture from Slalom Build](https://medium.com/slalom-build/introducing-black-slope-a-dotnet-core-reference-architecture-from-slalom-build-3f1452eb62ef)
* [BlackSlope: A Deeper Look at the Components of our DotNet Reference Architecture](https://medium.com/slalom-build/blackslope-a-deeper-look-at-the-components-of-our-dotnet-reference-architecture-b7b3a9d6e43b)
* [BlackSlope in Action: A Guide to Using our DotNet Reference Architecture](https://medium.com/slalom-build/blackslope-in-action-a-guide-to-using-our-dotnet-reference-architecture-d1e41eea8024)

## Installation Instructions

### Install .NET Core
Install the latest verison of .NET Core for Windows/Linux or Mac.
* https://dotnet.microsoft.com/download

### Build (Application)

	dotnet build src/BlackSlope.NET.sln

### Build (Database)

1. Install SQL Server Developer 2019
    > https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Update connection string server name and credentials in [appsettings.json](./src/BlackSlope.Api/appsettings.json)
    ```
    MoviesConnectionString
    ```
3. Create a SQL Database for "movies" either manually through a GUI of your choice or CLI tool
   - [SSMS](https://docs.microsoft.com/en-us/sql/relational-databases/databases/create-a-database?view=sql-server-ver15) 
   - [mssql-cli](https://github.com/dbcli/mssql-cli)
5. Open PowerShell and install the `dotnet-ef` tool using:
    ```
    dotnet tool install --global dotnet-ef
    ```
6. Navigate Powershell to your repository root directory and run the following command:
    ```
    dotnet ef database update --project=.\src\BlackSlope.Api\BlackSlope.Api.csproj
    ```
7. If successful, the result of the above command will be similar to the following example:
    ```
    Build started...
    Build succeeded.
    Applying migration '20190814225754_initialized'.
    Applying migration '20190814225910_seeded'.
    Done.
    ```

### Run

	dotnet run --project src/BlackSlope.Api/BlackSlope.Api.csproj

### Docker Setup
1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop)
2. Build the application under the `/src` directory
3. Build the docker image in the CLI under the `/src` directory:
    ```
    docker build -t blackslope.api -f Dockerfile .
    ```
4. Verify the new `blackslope.api` image exists in local docker repo with
    ```
    docker images
    ```
5. Create a container for the `blackslope.api` image
    ```
    docker create --name blackslope-container blackslope.api
    ```
6. Spin the container for the blackslope image up
    ```
    docker start blackslope-container
    ```
7. Visual inspection of the Docker Desktop app should show your image running in the container locally

### Test

    dotnet test ./src/

### Integration Tests
Intended for use by Quality Engineers (QE), Blackslope provides two SpecFlow driven Integration Test projects for consumption:
- `BlackSlope.Api.Tests.IntegrationTests`
  - using a `System.Net.Http.HttpClient` implementation
- `BlackSlope.Api.Tests.RestSharpIntegrationTests`
  - using a RestSharp HttpClient implementation

These can be executed in Test Explorer much like regular Unit Tests, and it is up to your team to choose which implementation best suits your project.

To Setup:
1. Ensure you've successfully run the [Build DB](#build-database) and [Build Application](#build-application) steps above.
2. Update the `appsettings.test.json` file in your Integration Test project with the proper DB connection string and Host URL for the BlackSlope API
    - NOTE: The Blackslope API can be run on a localhost configuration if desired, but needs to be done so in a separate instance of your IDE to allow tests to run
3. Download the appropriate [SpecFlow plugins](https://docs.specflow.org/projects/specflow/en/latest/Installation/Installation.html) for your IDE

### Swagger
Open your browser and navigate to ```http://localhost:51385/swagger``` to view the API documentation

### StyleCop and NetAnalyzers
Blackslope makes use of two different analyzers to keep the codebase clean and formatted.
1. StyleCop - for style formatting and code cleanliness
   - Consumes `stylecop.json` files at the project level
   - May be set as part of `.editorconfig`, but documentation is sparse and not recommended at this time
2. Microsoft.CodeAnalysis.NetAnalyzers - Nuget package for the IDE level; covers style formatting and code analysis issues.
   - Consumes `.editorconfig` files set at the solution or project level

**NOTE:** SA and CA rules are globally suppressed at `BlackSlope.Api.Common.GlobalSuppressions`

* [CodeAnalysis FAQ](https://github.com/MicrosoftDocs/visualstudio-docs/issues/2382)
* [When to Use NetAnalyzers?](https://github.com/MicrosoftDocs/visualstudio-docs/issues/2382)
* [.editorconfig Configuration](https://github.com/dotnet/roslyn-analyzers/blob/main/docs/Analyzer%20Configuration.md)
* [stylecop.json Configuration](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/Configuration.md)

**StyleCop**  
The following rules are currently ignored:

| Rule Id | Rule Title |
| --- | --- |
| SA1101 | Prefix local calls with this |
| SA1309 | Field names should not begin with an underscore |
| SA1600 | Elements should be documented |
| SA1614 | Element parameter documentation must have text |
| SA1616 | Element return value documentation must have text |
| SA1629 | Documentation text should end with a period |
| SA1633 | File should have header |
  
**CodeAnalysis**  
The following rules are currently ignored:

| Rule Id | Rule Title | Scope |
| --- | --- | --- |
| CA1031 | Do not catch general exception types | `~M:BlackSlope.Api.Common.Middleware.ExceptionHandling.ExceptionHandlingMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")` |
| CA1710 | Identifiers should have correct suffix | ```~T:BlackSlope.Api.Common.Validators.CompositeValidator\`1``` |