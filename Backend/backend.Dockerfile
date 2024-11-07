FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SmartExcelAnalyzerBackend.sln .
COPY API/API.csproj API/
COPY Domain/Domain.csproj Domain/
COPY Persistence/Persistence.csproj Persistence/
COPY Application/Application.csproj Application/
COPY SmartExcelAnalyzer.Tests/SmartExcelAnalyzer.Tests.csproj SmartExcelAnalyzer.Tests/

COPY . .
RUN dotnet restore SmartExcelAnalyzerBackend.sln
RUN dotnet build SmartExcelAnalyzerBackend.sln -c Release --no-restore
RUN dotnet publish API/API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5001;https://+:44359;http://+:5000
EXPOSE 5001
EXPOSE 5000
EXPOSE 44359

ENTRYPOINT ["dotnet", "API.dll"]