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