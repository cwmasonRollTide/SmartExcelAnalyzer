FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY */*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore

COPY . .

RUN PROJECT_TEST_PATH=$(find . -name 'SmartExcelAnalyzer.Tests.csproj') && \
    dotnet test $PROJECT_TEST_PATH --collect:"XPlat Code Coverage"

RUN dotnet publish SmartExcelAnalyzerBackend.sln -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5001,https://+:44359,http://+:5000
EXPOSE 5001
EXPOSE 5000
EXPOSE 44359

ENTRYPOINT ["dotnet", "SmartExcelAnalyzerBackend.dll"]
