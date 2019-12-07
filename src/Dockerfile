# Build the application
FROM microsoft/dotnet:3.0-sdk AS build
COPY . .
WORKDIR /BlackSlope.Api
RUN dotnet restore
RUN dotnet publish --no-restore -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0
WORKDIR /app
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "BlackSlope.Api.dll"]
