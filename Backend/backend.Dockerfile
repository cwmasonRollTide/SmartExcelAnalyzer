# Testing stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /app

# Copy everything
COPY . .

# Restore dependencies
RUN dotnet restore

# Find and run tests
RUN PROJECT_TEST_PATH=$(find . -name 'SmartExcelAnalyzer.Tests.csproj') && \
    dotnet test $PROJECT_TEST_PATH

# Final application stage
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Copy everything
COPY . .

# Restore dependencies 
RUN dotnet restore

# Expose ports
EXPOSE 80
EXPOSE 443  
EXPOSE 5000
EXPOSE 5001

# Set the entrypoint to find the project, restore, and run
ENTRYPOINT ["/bin/sh", "-c", "\
    PROJECT_PATH=$(find . -name 'API.csproj') && \
    dotnet restore $PROJECT_PATH && \
    dotnet watch run --project $PROJECT_PATH --no-restore --urls http://+:80 \
"]
