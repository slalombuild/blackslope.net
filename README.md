# BlackSlope.NET

## What is it?

You can read more in the following blog posts:

* https://medium.com/slalom-engineering/black-slope-a-net-core-reference-architecture-5a7bf8695fc8
* https://medium.com/slalom-engineering/black-slope-setup-and-insights-e05ab58e2960

## Installation Instructions

### Install .NET Core
Install the latest verison of .NET Core for Windows/Linux or Mac.
* https://dotnet.microsoft.com/download

### Build

	dotnet build BlackSlope.NET.sln

### Run

	dotnet run --project BlackSlope.Hosts.Api/BlackSlope.Hosts.Api.csproj
	
### Swagger
Open your browser and navigate to ```http://localhost:51385/swagger``` to view the API documentation
