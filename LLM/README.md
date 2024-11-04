# 🤖💬 LLM Service

- [![Smart Excel Analyzer LLM CI/CD Workflow](https://github.com/cwmasonRollTide/SmartExcelAnalyzer/actions/workflows/llm-workflow.yml/badge.svg?branch=main)](https://github.com/cwmasonRollTide/SmartExcelAnalyzer/actions/workflows/llm-workflow.yml)

The LLM (Language Model) Service is a key component of the Smart Excel Analyzer project. It leverages advanced natural language processing techniques to analyze Excel data and generate insightful answers to user queries.

## Table of Contents

- [🤖💬 LLM Service](#-llm-service)
  - [Table of Contents](#table-of-contents)
  - [📝 Overview](#-overview)
  - [⚙️ Setup](#️-setup)
    - [Windows (PowerShell)](#windows-powershell)
    - [Linux](#linux)
    - [macOS](#macos)
  - [🐳 Using Docker](#-using-docker)
  - [📦 Dependencies](#-dependencies)

## 📝 Overview

The LLM Service is built using Python and utilizes state-of-the-art language models to understand and process natural language queries. It integrates with the Smart Excel Analyzer backend API to receive user queries and Excel data, and returns relevant answers and summaries.

## ⚙️ Setup  

To set up the LLM Service, follow the instructions for your operating system:

### 🖥️ Windows (PowerShell)

1. Install Python 3.x:
   - Download the Python installer from the official website: <https://www.python.org/downloads/>
   - Run the installer and follow the installation wizard.
   - Make sure to check the option to add Python to the PATH environment variable.

2. Open PowerShell and navigate to the LLM service directory.

3. Create a virtual environment:

   ```python
   python -m venv venv
   ```

4. Activate the virtual environment:

   ```python
   .\venv\Scripts\Activate.ps1
   ```

5. Install the required dependencies:

   ```python
   pip install -r requirements.txt
   ```

6. Configure the necessary environment variables, such as API endpoints and authentication keys.

7. Run the service:

   ```python
   python main.py
   ```

### 🐧 Linux

1. Install Python 3.x using your distribution's package manager. For example, on Ubuntu:

   ```python
   sudo apt update
   sudo apt install python3 python3-pip python3-venv
   ```

2. Open a terminal and navigate to the LLM service directory.

3. Create a virtual environment:

   ```python
   python3 -m venv venv
   ```

4. Activate the virtual environment:

   ```python
   source venv/bin/activate
   ```

5. Install the required dependencies:

   ```python
   pip install -r requirements.txt
   ```

6. Configure the necessary environment variables, such as API endpoints and authentication keys.

7. Run the service:

   ```python
   python main.py
   ```

### 🍎 macOS

1. Install Python 3.x using Homebrew:

   ```python
   brew install python
   ```

2. Open a terminal and navigate to the LLM service directory.

3. Create a virtual environment:

   ```python
   python3 -m venv venv
   ```

4. Activate the virtual environment:

   ```python
   source venv/bin/activate
   ```

5. Install the required dependencies:

   ```python
   pip install -r requirements.txt
   ```

6. Configure the necessary environment variables, such as API endpoints and authentication keys.

7. Run the service:

   ```python
   python main.py
   ```

## 🐳 Using Docker

   1. Build the Docker Image:

      ```powershell
      docker buildx build -f llm.Dockerfile -t llm .
      ```

   2. Run the Docker Container:

      ```powershell
      docker run -p 8000:8000 llm
      ```

   3. Make a request to the LLM Service:

      ```http
      POST "http://localhost:8000/query" 
      "Content-Type: application/json" 
      {
         question: "What is the total revenue for the year 2023?", 
         documentId: "123456789012" 
      }
      ```

   ***Note:** The qdrant database must be running and accessible for the LLM service to query the data. Also must have a document with this id uploaded previously*

## 📦 Dependencies

The LLM Service relies on the following key dependencies:

- `hugging face`: Provides access to Hugging Face's powerful open source models.
- `azure-functions`: Enables running the service as an Azure Function.
- `requests`: Allows making HTTP requests to interact with the backend API.
- `qdrant`: Vector Database used for storing and retrieving high-dimensional vectors quickly to compare against computed LLM embeddings of data.

For a complete list of dependencies, refer to the `requirements.txt` file.
