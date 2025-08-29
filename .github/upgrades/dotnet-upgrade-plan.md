# .NET 8 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8 upgrade.
3. Upgrade BlackSlope.Hosts.Console\BlackSlope.Hosts.ConsoleApp.csproj
4. Upgrade RenameUtility\RenameUtility\RenameUtility.csproj  
5. Upgrade BlackSlope.Api\BlackSlope.Api.csproj
6. Upgrade BlackSlope.Api.Common\BlackSlope.Api.Common.csproj
7. Upgrade BlackSlope.Api.Tests\BlackSlope.Api.Tests.csproj
8. Upgrade BlackSlope.Api.Common.Tests\BlackSlope.Api.Common.Tests.csproj
9. Run unit tests to validate upgrade in the projects listed below:
   - BlackSlope.Api.Tests\BlackSlope.Api.Tests.csproj
   - BlackSlope.Api.Common.Tests\BlackSlope.Api.Common.Tests.csproj

## Settings

This section contains settings and data used by execution steps.

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### BlackSlope.Hosts.ConsoleApp.csproj modifications

Project properties changes:
- Verify target framework is set to `net8.0`

#### RenameUtility.csproj modifications  

Project properties changes:
- Verify target framework is set to `net8.0`

#### BlackSlope.Api.csproj modifications

Project properties changes:
- Verify target framework is set to `net8.0`

#### BlackSlope.Api.Common.csproj modifications

Project properties changes:  
- Verify target framework is set to `net8.0`

#### BlackSlope.Api.Tests.csproj modifications

Project properties changes:
- Verify target framework is set to `net8.0`

#### BlackSlope.Api.Common.Tests.csproj modifications

Project properties changes:
- Verify target framework is set to `net8.0`