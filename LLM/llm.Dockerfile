FROM python:3.9-slim

WORKDIR /app
RUN pip install --no-cache-dir --upgrade pip setuptools wheel
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

RUN python -c "from transformers import AutoTokenizer, AutoModel, pipeline; AutoTokenizer.from_pretrained('facebook/bart-large-cnn'); AutoModel.from_pretrained('facebook/bart-large-cnn'); AutoTokenizer.from_pretrained('sentence-transformers/all-MiniLM-L6-v2'); AutoModel.from_pretrained('sentence-transformers/all-MiniLM-L6-v2'); pipeline('text2text-generation', model='facebook/bart-large-cnn')"
RUN pip list
COPY . .

EXPOSE 8000
EXPOSE 8001
EXPOSE 8002
EXPOSE 8003
EXPOSE 8004
EXPOSE 8005