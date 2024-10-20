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
9. [Testing](#testing)
10. [Deployment](#deployment)
11. [License](#license)
12. [Contact](#contact)

## Introduction

Smart Excel Analyzer is an advanced tool designed to process, analyze, and query Excel files using state-of-the-art Language Model (LLM) technology and vector database capabilities. This project aims to transform the way users interact with spreadsheet data by providing intelligent, natural language querying and deep data insights.

## Features

- **Excel File Upload**: Seamlessly upload Excel files for processing and analysis.
- **Natural Language Querying**: Ask questions about your data in plain English.
- **Intelligent Data Analysis**: Leverage LLM technology to extract insights from your Excel data.
- **Vector Database Integration**: Utilize Qdrant for efficient similarity search and data retrieval.
- **Scalable Architecture**: Designed to handle large datasets with distributed LLM services.
- **Real-time Processing**: Get quick responses to your queries with optimized data processing.

## Technology Stack

- **Backend**: 
  - ASP.NET Core 8.0
  - C# 12
- **Frontend**: 
  - React with TypeScript
  - Vite for build tooling
- **Database**: 
  - Qdrant Vector Database
- **Machine Learning**:
  - Hugging Face Transformers
  - PyTorch
- **Containerization**: 
  - Docker
  - Docker Compose
- **API Documentation**: 
  - Swagger / OpenAPI
- **Testing**: 
  - xUnit
  - FluentAssertions
  - Moq
- **Other Libraries**:
  - MediatR for CQRS pattern
  - FluentValidation for request validation
  - ClosedXML for Excel file processing
  - ExcelDataReader for Fast Excel Data Loading

## Architecture

The Smart Excel Analyzer follows a microservices architecture:

1. **Frontend Service**: React application for user interaction.
2. **Backend Service**: ASP.NET Core API handling business logic and orchestration.
3. **LLM Services**: Multiple instances of Python-based services for natural language processing and embedding generation.
4. **Vector Database**: Qdrant for storing and querying vector embeddings.

The application uses a CQRS (Command Query Responsibility Segregation) pattern with MediatR for efficient request handling.

## Setup and Installation

1. Clone the repository:
    * git clone https://github.com/cwmasonRollTide/SmartExcelAnalyzer
cd smart-excel-analyzer

2. Ensure Docker and Docker Compose are installed on your system.

3. Create a `.env` file in the root directory and populate it with necessary environment variables (refer to `.env.example`).

4. Build and run the Docker containers:
    * ```docker compose up --build -d```

5. Access the application at `http://localhost:3000`.

6. Access the backend API directly at `http://localhost:5001/swagger`

## Usage

1. Upload an Excel file through the web interface.
2. Wait for the file to be processed and indexed.
3. Use the query interface to ask questions about your data in natural language.
4. View the results and insights provided by the system.

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

## Testing

- Backend tests: Run `dotnet test` in the root directory.
- Frontend tests: Run `npm test` in the `Frontend` directory.

## Deployment

The application is containerized and can be deployed to any environment that supports Docker:

1. Ensure all environment variables are properly set for the target environment.
2. Build the Docker images: `docker-compose build`
3. Push the images to your container registry.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Contact

If you are trying to fork this or having trouble using the tool itself, feel free to contact me!

Connor Mason - connor.mason@fivemconsulting.com

Git Link: https://github.com/cwmasonRollTide/SmartExcelAnalyzer