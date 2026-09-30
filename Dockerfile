# Stage 1: Build stage using the .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files into the container
COPY . .

# Restore required NuGet packages
RUN dotnet restore

# Compile and publish the project in Release configuration to /app/publish
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime stage using the lightweight ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Inform Docker that the container listens on port 8080 at runtime
EXPOSE 8080

# Instruct ASP.NET Core to listen on port 8080 inside the container
ENV ASPNETCORE_HTTP_PORTS=8080

# Start the web application DLL
ENTRYPOINT ["dotnet", "H-st-2026-Gruppe-1.dll"]