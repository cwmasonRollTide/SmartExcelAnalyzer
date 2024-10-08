import os
import json
from typing import List
import torch
from enum import Enum
from pydantic import BaseModel
from pymongo import MongoClient
from transformers import pipeline
from fastapi import FastAPI, HTTPException
from transformers import AutoTokenizer, AutoModel

class EnvironmentVariables(str, Enum):
    TEXT_GENERATION_MODEL = "TEXT_GENERATION_MODEL"
    EMBEDDING_MODEL = "EMBEDDING_MODEL"
    DB_CONNECTION_STRING = "DB_CONNECTION_STRING"

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

default_text_generation_model = "facebook/bart-large-cnn"
default_embedding_model = "sentence-transformers/all-MiniLM-L6-v2"
default_db_connection_string = "mongodb://admin:password@localhost:27017/smartexcelanalyzer"

EMBEDDING_MODEL = os.getenv(EnvironmentVariables.EMBEDDING_MODEL.value, default_embedding_model)
DB_CONNECTION_STRING = os.getenv(EnvironmentVariables.DB_CONNECTION_STRING.value, default_db_connection_string)
TEXT_GENERATION_MODEL = os.getenv(EnvironmentVariables.TEXT_GENERATION_MODEL.value, default_text_generation_model)

app = FastAPI()
model = pipeline("text2text-generation", model=TEXT_GENERATION_MODEL)

client = MongoClient(DB_CONNECTION_STRING)
db = client.get_default_database()

tokenizer = AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
embedding_model = AutoModel.from_pretrained(EMBEDDING_MODEL)

@app.get("/health", response_model=dict)
async def health():
    """
    Perform a health check by loading the pretrained models and checking MongoDB connection.
    """
    try:
        print("Performing health check...")
        AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
        AutoModel.from_pretrained(EMBEDDING_MODEL)
        pipeline("text2text-generation", model=TEXT_GENERATION_MODEL)
        db.command('ping')  # Check MongoDB connection
        print("Health check passed: LLM Service Endpoint, MongoDB, and LLM Models are all accessible.")
        return {"status": "ok"}
    except Exception as e:
        print("Health check failed.")
        print(e)
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/query", response_model=QueryResponse)
async def process_query(query: Query) -> QueryResponse:
    """
    Process a query by fetching relevant rows and summaries from the database,
    then use a text generation model to generate an answer based on the provided context.

    Args:
        query (Query): The query containing the document ID and the question.

    Returns:
        QueryResponse: The generated answer along with the relevant rows.
    """
    try:
        question_embedding = await compute_embedding(ComputeEmbedding(text=query.question))
        relevant_docs = db.documents.aggregate([
            {"$match": {"_id": query.document_id}},
            {"$addFields": {
                "similarity": {
                    "$dotProduct": ["$embedding", question_embedding] ## Mongo is perfect for comparing Similarity of two vectors / embeddings across documents
                }
            }},
            {"$sort": {"similarity": -1}},
            {"$limit": 10},
            {"$project": {"content": 1, "_id": 0}}
        ])
        relevant_rows = list(relevant_docs)

        summary = db.summaries.find_one({"_id": query.document_id}, {"content": 1, "_id": 0})
        context = json.dumps(relevant_rows)
        summary_text = json.dumps(summary["content"] if summary else {})
        prompt = f"""Given the following Excel data summary: {summary_text} And these relevant rows: {context} Question: {query.question} Answer:"""
        result = model(prompt, max_length=250, do_sample=False)[0]['generated_text']
        print(result)
        return {
            "answer": result,
            "question": query.question,
            "documentId": query.document_id,
            "relevantRows": relevant_rows
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/compute_embedding", response_model=list[float])
async def compute_embedding(compute_embedding: ComputeEmbedding):
    """
    Compute the embedding of a given text using a pre-trained embedding model.

    Args:
        compute_embedding (ComputeEmbedding): The text to compute the embedding for.
        { "text": "This is a sample text." }

    Returns:
        list[ float ]: The computed embedding of the text. array of float values float[] in .NET
    """
    try:
        inputs = tokenizer(compute_embedding.text, return_tensors="pt", truncation=True, padding=True)
        with torch.no_grad():
            embeddings = embedding_model(**inputs).last_hidden_state.mean(dim=1)
        return embeddings.numpy().tolist()[0]
    except Exception as e:
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
    