# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy the .csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the application code
COPY . ./

# Build the application
RUN dotnet publish -c Release -o /out

# Use the official .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copy the build output from the previous stage
COPY --from=build /out .

# Set environment variable for ASP.NET Core URL
ENV ASPNETCORE_URLS=http://*:8080

# Expose the port for the API
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "ControlPlaneAPI.dll"]
