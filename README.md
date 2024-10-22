# Smart Excel Analyzer

## Table of Contents
1. [Introduction](#introduction)
2. [Features](#features)
3. [Technology Stack](#technology-stack)
4. [Architecture](#architecture)
5. [Setup and Installation](#setup-and-installation)
6. [Usage](#usage)
7. [API Documentation](#api-documentation)
8. [Development](#development)
9. [Docker](#docker)
10. [Testing](#testing)
11. [License](#license)
12. [Contact](#contact)

## Introduction

Smart Excel Analyzer is an advanced tool designed to process, analyze, and query Excel files using state-of-the-art Language Model (LLM) technology and vector database capabilities.

This project aims to transform the way users interact with spreadsheet data by providing intelligent, natural language querying and deep data insights.

## Features

| Feature                           | Description                                                                |
|-----------------------------------|----------------------------------------------------------------------------|
| **Excel File Upload**             | Seamlessly upload Excel files for processing and analysis                  |
| **Natural Language Querying**     | Ask questions about your data in plain English                             |
| **Intelligent Data Analysis**     | Leverage LLM technology to extract insights from your Excel data           |
| **Local Data Analysis**           | This does not require outside internet access or third party intervention  |
| **Vector Database Integration**   | Utilize Qdrant for efficient similarity search and data retrieval          |
| **Scalable Architecture**         | Designed to handle large datasets with distributed LLM services            |
| **Real-time Processing**          | Get quick responses to your queries with optimized data processing         |

## Technology Stack

- **Backend**: 
  - [C# 12](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)
  - [ASP.NET Core 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- **Frontend**: 
  - [React with TypeScript](https://react.dev/learn/typescript)
  - [Vite for build tooling](https://vite.dev/)
- **Database**: 
  - [Qdrant Vector Database](https://try.qdrant.tech/high-performance-vector-search?utm_source=google&utm_medium=cpc&utm_campaign=21518712216&utm_content=163351119817&utm_term=qdrant%20vector%20database&hsa_acc=6907203950&hsa_cam=21518712216&hsa_grp=163351119817&hsa_ad=707722911577&hsa_src=g&hsa_tgt=kwd-2240456171437&hsa_kw=qdrant%20vector%20database&hsa_mt=e&hsa_net=adwords&hsa_ver=3&gad_source=1&gclid=CjwKCAjw1NK4BhAwEiwAVUHPUDZxy5-yERXvEGdq-Q58x1xzDrpdB3rj1norGq_P6JDlU9vI3yXnyhoCTIoQAvD_BwE)
- **Machine Learning**:
  - [PyTorch](https://pytorch.org/)
  - [Hugging Face Transformers](https://huggingface.co/docs/transformers/en/index)
- **Containerization**: 
  - [Docker](https://app.docker.com/)
  - [Docker Compose](https://docs.docker.com/compose/)
- **API Documentation**: 
  - [Swagger / OpenAPI](https://swagger.io/docs/)
- **Testing**: 
  - [xUnit](https://xunit.net/)
  - [Moq](https://github.com/devlooped/moq)
  - [FluentAssertions](https://github.com/fluentassertions/fluentassertions)

- **Other Libraries**:
  - [FluentValidation for request validation](https://github.com/FluentValidation/FluentValidation)
  - [ExcelDataReader for Fast Excel Data Loading](https://github.com/ExcelDataReader/ExcelDataReader)
  - [MediatR for CQRS pattern](https://github.com/jbogard/MediatR) **Only choice in my opinion**

## Architecture

The Smart Excel Analyzer follows a microservices architecture:

1. **Frontend Service**: React application for user interaction

2. **Backend Service**: ASP.NET Core API handling business logic and orchestration

3. **LLM Services**: Multiple instances of Python-based services for natural language processing and embedding generation

4. **Vector Database**: Qdrant for storing and querying vector embeddings

The application uses a CQRS (Command Query Responsibility Segregation) pattern with MediatR for efficient request handling

## Setup and Installation

1. Clone the repository:
      
    ```bash
      git clone https://github.com/cwmasonRollTide/SmartExcelAnalyzer
    ```
     
    ```bash
      cd SmartExcelAnalyzer
    ```

2. Ensure Docker and Docker Compose are installed on your system
    ```powershell
    # Download Docker Desktop Installer
    Invoke-WebRequest -Uri "https://desktop.docker.com/win/stable/Docker%20Desktop%20Installer.exe" -OutFile "DockerDesktopInstaller.exe"

    # Run the installer
    Start-Process -FilePath ".\DockerDesktopInstaller.exe" -Wait
    ```

3. Create a `.env` file in the root directory and populate it with necessary environment variables (refer to `.env.example`)

4. Build and run the Docker containers:
    ```bash
      docker compose up --build -d
    ```

5. Access the frontend application at `http://localhost:3000`

6. Access the backend API directly at `http://localhost:5001/swagger`

## Usage

1. Upload an Excel file through the web interface

2. Wait for the file to be processed and indexed

3. Use the query interface to ask questions about your data in natural language

4. View the results and insights provided by the system

## API Documentation

API documentation is available via Swagger UI. After starting the backend service, navigate to:
http://localhost:5001/swagger

## Development

To set up the development environment:

1. Backend:
   - Install .NET 8 SDK
   - Open the solution in Visual Studio or VS Code
   - Run `dotnet restore` to install dependencies

2. Frontend:
   - Install Node.js and npm
   - Navigate to the `Frontend` directory
   - Run `npm install` to install dependencies
   - Use `npm run dev` for development server

3. LLM Services:
   - Install Python 3.9+
   - Navigate to the `LLM` directory
   - Create a virtual environment: `python -m venv venv`
   - Activate the virtual environment and install dependencies: `pip install -r requirements.txt`

## Docker

- All of our docker images will be published to this docker hub container registry:
    * https://hub.docker.com/repository/docker/fivemowner/smartexcelanalyzer

- This process is kicked off in the file [in this Github Action](.github/workflows/docker-build-push.yml)
  ```bash
    ~/.github/workflows/docker-build-push.yml
  ```
  
## Testing

- Backend tests: Run `dotnet test` in the ```~./Backend/``` directory.

- Frontend tests: Run `npm test` in the ```~./Frontend/``` directory.

## Contact

If you are trying to fork this or having trouble using the tool itself, feel free to contact me!

Connor Mason - connor.mason@fivemconsulting.com

Git Link: https://github.com/cwmasonRollTide/SmartExcelAnalyzer
