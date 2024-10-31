FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /app

COPY . .

RUN dotnet restore

RUN PROJECT_TEST_PATH=$(find . -name 'SmartExcelAnalyzer.Tests.csproj') && \
dotnet test $PROJECT_TEST_PATH --collect:"XPlat Code Coverage" --settings ./coverlet.runsettings

FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

COPY . .

RUN dotnet restore

EXPOSE 80
EXPOSE 443  
EXPOSE 5000
EXPOSE 5001

ENTRYPOINT ["/bin/sh", "-c", "\
    PROJECT_PATH=$(find . -name 'API.csproj') && \
    dotnet restore $PROJECT_PATH && \
    dotnet watch run --project $PROJECT_PATH --no-restore --urls http://+:80,http://+:5000,http://+:5001\
"]
