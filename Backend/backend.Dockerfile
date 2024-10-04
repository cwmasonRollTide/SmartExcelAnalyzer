# Dockerfile for backend 
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build 
WORKDIR /app 
COPY . . 
RUN dotnet restore 
RUN dotnet publish -c Release -o out 
RUN mkdir /app/publish 
RUN cp -r /app/out /app/publish 
FROM mcr.microsoft.com/dotnet/aspnet:8.0 
WORKDIR /app 
COPY --from=build /app/out . 
COPY --from=build /app/publish /app/publish 
ENTRYPOINT ["dotnet", "API.dll"] 
