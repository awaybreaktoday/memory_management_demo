# Use the .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY DotNetMemoryApp.csproj .
RUN dotnet restore

# Copy the source code
COPY . .

# Build and publish the application
RUN dotnet publish -c Release -o out

# Use the .NET 8 runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /src/out .

# Expose the metrics port
EXPOSE 5000

# Set the entry point
ENTRYPOINT ["dotnet", "DotNetMemoryApp.dll"]
