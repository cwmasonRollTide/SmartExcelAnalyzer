@echo off
setlocal

:: Create the project
set /p "projectName=Enter the project name: "
set /p "solutionName=Enter the solution name: "

mkdir %projectName%
cd %projectName%

echo Creating the solution and projects...
mkdir Backend
cd Backend
dotnet new sln -n %solutionName%

set /p "frontendType=Choose the frontend type (1 - Vite, 2 - Create React App, 3 - Next.js): "
set /p "frontendLang=Choose the frontend language (1 - TypeScript, 2 - JavaScript): "

:: Create the frontend project
if "%frontendType%"=="1" (
    if "%frontendLang%"=="1" (
        call npm init vite@latest ../Client -- --template react-ts
    ) else (
        call npm init vite@latest ../Client -- --template react
    )
) else if "%frontendType%"=="2" (
    if "%frontendLang%"=="1" (
        call npx create-react-app ../Client --template typescript
    ) else (
        call npx create-react-app ../Client
    )
) else if "%frontendType%"=="3" (
    if "%frontendLang%"=="1" (
        call npx create-next-app@latest ../Client --typescript
    ) else (
        call npx create-next-app@latest ../Client
    )
)

:: Create the backend project
dotnet new webapi -n API
dotnet new classlib -n Application
dotnet new classlib -n Domain
dotnet new classlib -n Persistence

echo Adding projects to the solution...
dotnet sln add API/API.csproj
dotnet sln add Application/Application.csproj
dotnet sln add Domain/Domain.csproj
dotnet sln add Persistence/Persistence.csproj

echo Adding project references...
cd API
dotnet add reference ../Application/Application.csproj
cd ../Application
dotnet add reference ../Domain/Domain.csproj
dotnet add reference ../Persistence/Persistence.csproj
cd ../Persistence
dotnet add reference ../Domain/Domain.csproj
cd ..

echo Creating Docker and Docker Compose files...
cd ..
echo # Dockerfile for backend > Backend/backend.Dockerfile
echo FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build >> Backend/backend.Dockerfile
echo WORKDIR /app >> Backend/backend.Dockerfile
echo COPY . . >> Backend/backend.Dockerfile
echo RUN dotnet restore >> Backend/backend.Dockerfile
echo RUN dotnet publish -c Release -o out >> Backend/backend.Dockerfile
echo RUN mkdir /app/publish >> Backend/backend.Dockerfile
echo RUN cp -r /app/out /app/publish >> Backend/backend.Dockerfile
echo FROM mcr.microsoft.com/dotnet/aspnet:8.0 >> Backend/backend.Dockerfile
echo WORKDIR /app >> Backend/backend.Dockerfile
echo COPY --from=build /app/out . >> Backend/backend.Dockerfile
echo COPY --from=build /app/publish /app/publish >> Backend/backend.Dockerfile
echo ENTRYPOINT ["dotnet", "API.dll"] >> Backend/backend.Dockerfile

echo # Dockerfile for frontend > Client/frontend.Dockerfile
echo FROM node:18 AS build >> Client/frontend.Dockerfile
echo WORKDIR /app >> Client/frontend.Dockerfile
echo COPY . . >> Client/frontend.Dockerfile
echo RUN npm install >> Client/frontend.Dockerfile
echo RUN npm run build >> Client/frontend.Dockerfile
echo FROM nginx:stable-alpine >> Client/frontend.Dockerfile
echo COPY --from=build /app/dist /usr/share/nginx/html >> Client/frontend.Dockerfile
echo EXPOSE 80 >> Client/frontend.Dockerfile
echo CMD ["nginx", "-g", "daemon off;"] >> Client/frontend.Dockerfile

echo version: '3' > docker-compose.yml
echo services: >> docker-compose.yml
echo   backend: >> docker-compose.yml
echo     build: >> docker-compose.yml
echo       context: ./Backend >> docker-compose.yml
echo       dockerfile: backend.Dockerfile >> docker-compose.yml
echo     ports: >> docker-compose.yml
echo       - "5000:80" >> docker-compose.yml

echo   frontend: >> docker-compose.yml
echo     build: >> docker-compose.yml
echo       context: ./Client >> docker-compose.yml
echo       dockerfile: frontend.Dockerfile >> docker-compose.yml
echo     ports: >> docker-compose.yml
echo       - "3000:80" >> docker-compose.yml
echo     depends_on: >> docker-compose.yml
echo       - backend >> docker-compose.yml

echo node_modules/ > .gitignore
echo bin/ >> .gitignore
echo obj/ >> .gitignore

echo Executing dotnet restore...
cd Backend
dotnet restore

echo Running npm install...
cd ../Client
npm install

echo Finished!
endlocal