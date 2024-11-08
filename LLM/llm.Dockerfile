FROM python:3.9-slim

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir --upgrade pip setuptools wheel
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

RUN python -c "from transformers import AutoTokenizer, AutoModel, pipeline; \
    AutoTokenizer.from_pretrained('facebook/bart-large-cnn'); \
    AutoModel.from_pretrained('facebook/bart-large-cnn'); \
    AutoTokenizer.from_pretrained('sentence-transformers/all-MiniLM-L6-v2'); \
    AutoModel.from_pretrained('sentence-transformers/all-MiniLM-L6-v2'); \
    pipeline('text2text-generation', model='facebook/bart-large-cnn')"

COPY . .

EXPOSE 8000
EXPOSE 8001
EXPOSE 8002

ARG LLM_PORT=8000
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
