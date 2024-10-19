import os
import json
import torch
import logging
from enum import Enum
from typing import List
from pydantic import BaseModel
from transformers import pipeline
from qdrant_client.http import models
from qdrant_client import QdrantClient
from fastapi import FastAPI, HTTPException
from fastapi.responses import JSONResponse
from transformers import AutoTokenizer, AutoModel

class EnvironmentVariables(str, Enum):
    QDRANT_HOST = "QDRANT_HOST"
    QDRANT_PORT = "QDRANT_PORT"
    EMBEDDING_MODEL = "EMBEDDING_MODEL"
    TEXT_GENERATION_MODEL = "TEXT_GENERATION_MODEL"

class Query(BaseModel):
    document_id: str
    question: str

class QueryResponse(Query): 
    answer: str
    relevantRows: list

class ComputeEmbedding(BaseModel):
    text: str

class ComputeBatchEmbeddings(BaseModel):
    texts: list[str]

default_qdrant_port = 6333
default_qdrant_host = "localhost"
default_text_generation_model = "facebook/bart-large-cnn"
default_embedding_model = "sentence-transformers/all-MiniLM-L6-v2"

QDRANT_HOST = os.getenv(EnvironmentVariables.QDRANT_HOST.value, default_qdrant_host)
QDRANT_PORT = int(os.getenv(EnvironmentVariables.QDRANT_PORT.value, default_qdrant_port))
EMBEDDING_MODEL = os.getenv(EnvironmentVariables.EMBEDDING_MODEL.value, default_embedding_model)
TEXT_GENERATION_MODEL = os.getenv(EnvironmentVariables.TEXT_GENERATION_MODEL.value, default_text_generation_model)

app = FastAPI()
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)
tokenizer = AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
embedding_model = AutoModel.from_pretrained(EMBEDDING_MODEL)
model = pipeline("text2text-generation", model=TEXT_GENERATION_MODEL)
qdrant_client = QdrantClient(host=QDRANT_HOST, port=QDRANT_PORT)

@app.get("/health", response_model=dict)
async def health():
    try:
        print("Performing health check...")
        AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
        AutoModel.from_pretrained(EMBEDDING_MODEL)
        pipeline("text2text-generation", model=TEXT_GENERATION_MODEL)
        qdrant_client.get_collections()  # Check Qdrant connection
        print("Health check passed: LLM Service Endpoint, Qdrant, and LLM Models are all accessible.")
        return {"status": "ok"}
    except Exception as e:
        print("Health check failed.")
        print(e)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/query", response_model=QueryResponse)
async def process_query(query: Query) -> QueryResponse:
    try:
        inputs = tokenizer(query.question, return_tensors="pt", truncation=True, padding=True)
        with torch.no_grad():
            question_embedding = embedding_model(**inputs).last_hidden_state.mean(dim=1).numpy().tolist()[0]

        logger.info(f"Question Embedding: {question_embedding}")

        search_result = qdrant_client.search(
            collection_name="documents",
            query_vector=question_embedding,
            query_filter=models.Filter(
                must=[
                    models.FieldCondition(
                        key="document_id",
                        match=models.MatchValue(value=query.document_id)
                    )
                ]
            ),
            limit=10
        )
        relevant_rows = [json.loads(hit.payload["content"]) for hit in search_result]

        logger.info(f"Relevant Rows: {relevant_rows}")

        context = " ".join([row["content"] for row in relevant_rows])
        prompt = f"Given the following context: {context} Question: {query.question} Answer:"
        result = model(prompt, max_length=250, do_sample=False)[0]['generated_text']
        return QueryResponse(
            answer=result,
            question=query.question,
            document_id=query.document_id,
            relevant_rows=relevant_rows
        )
    except Exception as e:
        logger.error(f"Error processing query: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/compute_embedding", response_model=list[float])
async def compute_embedding(compute_embedding: ComputeEmbedding):
    try:
        inputs = tokenizer(compute_embedding.text, return_tensors="pt", truncation=True, padding=True)
        with torch.no_grad():
            embeddings = embedding_model(**inputs).last_hidden_state.mean(dim=1)
        return embeddings.numpy().tolist()[0]
    except Exception as e:
        print(e)
        raise HTTPException(status_code=500, detail=str(e))
    
@app.post("/compute_batch_embedding", response_model=List[List[float]])
async def compute_batch_embedding(compute_embedding: ComputeBatchEmbeddings):
    try:
        inputs = tokenizer(compute_embedding.texts, padding=True, truncation=True, return_tensors="pt")
        with torch.no_grad():
            outputs = embedding_model(**inputs)
        embeddings = outputs.last_hidden_state.mean(dim=1)
        return embeddings.tolist()
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    
async def general_exception_handler(request, exc):
    logger.error(f"An error occurred: {str(exc)}", exc_info=True)
    return JSONResponse(
        status_code=500,
        content={"message": "An internal error occurred", "detail": str(exc)}
    )