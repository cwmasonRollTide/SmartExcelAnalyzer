# 📈 Smart Excel Analyzer Frontend

- [![Smart Excel Analyzer Frontend CI/CD Workflow](https://github.com/cwmasonRollTide/SmartExcelAnalyzer/actions/workflows/frontend-workflow.yml/badge.svg?branch=main)](https://github.com/cwmasonRollTide/SmartExcelAnalyzer/actions/workflows/frontend-workflow.yml)

*This is the frontend for the Smart Excel Analyzer application. It provides a web-based user interface for uploading Excel files, querying the data using natural language, and viewing the results.*

## Features

- 📁 File upload component for sending Excel files to the backend
- 🔍 Query form for entering natural language questions about the data
- 📊 Document list showing uploaded files
- 💬 Query result component displaying answers from the AI
- 🌙 Light and dark theme UI

## 📚 Tech Stack

- React
- TypeScript
- Vite
- Jest

## Getting Started

### Prerequisites

- Node.js and npm installed
- Backend API running (see main README for instructions)

### 🏃 Running Locally

- Install dependencies:

```powershell
  npm install
```

- Start the dev server:

```powershell
  npm run dev
```

### 🐳 Running with Docker

- Build the nginx Image:

  ```powershell
  docker build -t nginx -f nginx.Dockerfile .
  ```

- Run the nginx Container:

  ```powershell
  docker run -p 8080:80 ngingx
  ```

- Build the Frontend Docker Image:
  
  ```powershell
  docker build -t smart-excel-analyzer-frontend -f frontend.Dockerfile .
  ```
  
- Run the Frontend Container:
  
  ```powershell
  docker run -p 3000:3000 smart-excel-analyzer-frontend
  ```
  
- Open <http://localhost:3000> in your browser

## Other Commands

- 🧪 `npm test` - Run the test suite
- 🔧 `npm run build` - Build the production version of the app
- 🔍 `npm run lint` - Lint the code using ESLint

### 🔩 Project Components

| Component                         | Description                                                                        |
|-----------------------------------|------------------------------------------------------------------------------------|
| **App.tsx**                       | Main application component. Manages state for uploaded documents and query results.|
| **FileUpload**                    | Allows user to select an Excel file and upload it to the backend.                  |
| **QueryForm**                     | Accepts natural language questions from the user and submits them to the backend.  |
| **DocumentList**                  | Displays a list of uploaded Excel files                                            |
| **QueryResult**                   | Shows the result of the most recent query.                                         |
| **ThemeSwitch**                   | Allows toggling between light and dark UI themes.                                  |

*The `services` directory contains modules for interacting with the backend API.*
