# BlackSlope.NET

## What is it?

You can read more in the following blog posts:

* https://medium.com/slalom-engineering/black-slope-a-net-core-reference-architecture-5a7bf8695fc8
* https://medium.com/slalom-engineering/black-slope-setup-and-insights-e05ab58e2960

## Installation Instructions

### Install .NET Core
Install the latest verison of .NET Core for Windows/Linux or Mac.
* https://dotnet.microsoft.com/download

### Build (Application)

	dotnet build BlackSlope.NET.sln

### Build (Database)

1. Install SQL Server Developer 2019
    > https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Update connection string server name and credentials in [appsettings.json](./src/BlackSlope.Api/appsettings.json)
    ```
    MoviesConnectionString
    ```
3. Open PowerShell to your repository root directory and run the following command:
    ```
    dotnet ef database update --project src/BlackSlope.Hosts.Api/BlackSlope.Hosts.Api.csproj
    ```
4. If successful, the result of the above command will be similar to the following example:
    ```
    Build started...
    Build succeeded.
    Applying migration '20190814225754_initialized'.
    Applying migration '20190814225910_seeded'.
    Done.
    ```

### Run

	dotnet run --project src/BlackSlope.Hosts.Api/BlackSlope.Hosts.Api.csproj
	
### Swagger
Open your browser and navigate to ```http://localhost:51385/swagger``` to view the API documentation

### StyleCop
The following rules are currently ignored.

| Rule Id | Rule Title |
| --- | --- |
| SA1101 | Prefix local calls with this |
| SA1309 | Field names should not begin with an underscore |
| SA1629 | Documentation text should end with a period |
| SA1633 | File should have header |
| SA1600 | Elements should be documented |
| SA1614 | Element parameter documentation must have text |
| SA1616 | Element return value documentation must have text |