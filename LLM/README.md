# LLM Service 🤖💬

The LLM (Language Model) Service is a key component of the Smart Excel Analyzer project. It leverages advanced natural language processing techniques to analyze Excel data and generate insightful answers to user queries.

## Table of Contents

- [Overview](#overview)
- [Setup](#setup)
  - [Windows (PowerShell)](#windows-powershell)
  - [Linux](#linux)
  - [macOS](#macos)
- [Local Setup](#local-setup)
- [Dependencies](#dependencies-)
- [Usage](#usage-)

## Overview 📝

The LLM Service is built using Python and utilizes state-of-the-art language models to understand and process natural language queries. It integrates with the Smart Excel Analyzer backend API to receive user queries and Excel data, and returns relevant answers and summaries.

## Setup ⚙️

To set up the LLM Service, follow the instructions for your operating system:

### Windows (PowerShell)

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

### Linux

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

### macOS

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

## Local Setup

But really the easiest way is to run the whole project

   1. Go to the root directory

   2. Run the command

   ```powershell
   docker compose up -d
   ```

## Dependencies 📦

The LLM Service relies on the following key dependencies:

- `openai`: Provides access to OpenAI's powerful language models.
- `azure-functions`: Enables running the service as an Azure Function.
- `requests`: Allows making HTTP requests to interact with the backend API.

For a complete list of dependencies, refer to the `requirements.txt` file.

## Usage 🚀

The LLM Service exposes an API endpoint that accepts user queries and Excel data as input. It processes the input using advanced NLP techniques and returns relevant answers and summaries.

To use the LLM Service:

1. Ensure the service is running and accessible.
2. Send a POST request to the `/analyze` endpoint with the following payload:

   ```http
   {
     "query": "User's natural language query",
     "data": "Excel data as JSON"
   }
   ```

3. The service will process the request and return the generated answer and summary.
