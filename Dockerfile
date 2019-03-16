# Build the application
FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ./src .
WORKDIR /src/BlackSlope.Hosts.Api
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app

FROM microsoft/dotnet:2.2-runtime

WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "BlackSlope.Hosts.Api.dll"]
