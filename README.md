# BlackSlope.NET

## What is it?

You can read more in the following blog posts:

* https://medium.com/slalom-engineering/black-slope-a-net-core-reference-architecture-5a7bf8695fc8
* https://medium.com/slalom-engineering/black-slope-setup-and-insights-e05ab58e2960

## Installtion Instructions

### Install .NET Core
Install the latest verison of .NET Core for Windows/Linux or Mac.
* https://dotnet.microsoft.com/download

### Build

	cd src
	dotnet build BlackSlope.NET.sln

### Run

	dotnet run --project BlackSlope.Hosts.Api/BlackSlope.Hosts.Api.csproj
	
### Swagger
Open your browser and navigate to ```http://localhost:51385/swagger``` to view the API documentation.

### Authentication
The API endpoints are authenticated using an Azure Active Directory application. You will need to provide an ```Authorization``` header with a bearer JWT. The console app is configured to generate a token. You will need to reach out to the team to get the client secret which is stored in Azure Key Vault.
Once you have the secret you can run the console app.

	dotnet run --project BlackSlope.Hosts.Console/BlackSlope.Hosts.ConsoleApp.csproj

Paste in the secret and a JWT will be generated. eg.

	Bearer <--token-->

Paste this into the Authorize swagger header to request resources via Swagger.

### AAD Configuration
//todo: add details here

### Entity Framework Migrations
//todo: add details here

### Serilog Configuration
//todo: add details here

### Application Insights Configuration
//todo: add details here

