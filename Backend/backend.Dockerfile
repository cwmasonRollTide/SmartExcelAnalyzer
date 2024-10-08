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
EXPOSE 80
EXPOSE 443
EXPOSE 5000
EXPOSE 5001
ENTRYPOINT ["dotnet", "API.dll"] 
