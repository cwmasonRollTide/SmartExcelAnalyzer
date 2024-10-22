FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy project files first
COPY ["Backend/API/API.csproj", "API/"]
COPY ["Backend/Application/Application.csproj", "Application/"]
COPY ["Backend/Domain/Domain.csproj", "Domain/"]
COPY ["Backend/Persistence/Persistence.csproj", "Persistence/"]

# Restore all packages
RUN dotnet restore "API/API.csproj"

# Copy the rest of the code
COPY ["Backend/API/", "API/"]
COPY ["Backend/Application/", "Application/"]
COPY ["Backend/Domain/", "Domain/"]
COPY ["Backend/Persistence/", "Persistence/"]

# Build and publish
RUN dotnet publish "API/API.csproj" -c Release -o /app --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

# Update the entrypoint to explicitly use Program
ENTRYPOINT ["dotnet", "API.dll", "--server.urls", "http://+:8080"]