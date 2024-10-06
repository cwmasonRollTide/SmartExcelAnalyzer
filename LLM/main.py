# LLM/main.py
import os
import json
import torch
import psycopg2
from pydantic import BaseModel
from transformers import pipeline
from fastapi import FastAPI, HTTPException
from psycopg2.extras import RealDictCursor
from LLM.Config import EnvironmentVariables
from transformers import AutoTokenizer, AutoModel

default_text_generation_model = "facebook/bart-large-cnn"
default_embedding_model = "sentence-transformers/all-MiniLM-L6-v2"
default_db_connection_string = "dbname=smartexcelanalyzer user=admin password=password host=localhost sslmode=disable"

EMBEDDING_MODEL = os.getenv(EnvironmentVariables.EMBEDDING_MODEL, default_embedding_model)
DB_CONNECTION_STRING = os.getenv(EnvironmentVariables.DB_CONNECTION_STRING, default_db_connection_string)
TEXT_GENERATION_MODEL = os.getenv(EnvironmentVariables.TEXT_GENERATION_MODEL, default_text_generation_model)

app = FastAPI()

# Load the model
model = pipeline("text2text-generation", model=TEXT_GENERATION_MODEL)

class Query(BaseModel):
    document_id: str
    question: str

@app.post("/query")
async def process_query(query: Query):
    try:
        # Connect to the vector database where we store the documents and summaries (excel rows and summary of excel sheets)
        conn = psycopg2.connect(DB_CONNECTION_STRING)
        cur = conn.cursor(cursor_factory=RealDictCursor)

        # Fetch relevant rows and summary
        cur.execute("""
            SELECT content FROM documents 
            WHERE id = %s 
            ORDER BY embedding <-> %s 
            LIMIT 10
        """, (query.document_id, await compute_embedding(query.question)))
        relevant_rows = cur.fetchall()

        cur.execute("SELECT content FROM summaries WHERE id = %s", (query.document_id,))
        summary = cur.fetchone()

        # Prepare context for the LLM
        context = json.dumps(relevant_rows)
        summary_text = json.dumps(summary)

        prompt = f"""Given the following Excel data summary: {summary_text} And these relevant rows: {context} Question: {query.question} Answer:"""
        result = model(prompt, max_length=250, do_sample=False)[0]['generated_text']
        print(result)
        return {"answer": result}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/compute_embedding")
async def compute_embedding(text: str):
    tokenizer = AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
    embedding_model = AutoModel.from_pretrained(EMBEDDING_MODEL)
    inputs = tokenizer(text, return_tensors="pt", truncation=True, padding=True)
    with torch.no_grad():
        embeddings = embedding_model(**inputs).last_hidden_state.mean(dim=1)
    return embeddings.numpy().tolist()[0]

@app.get("/health")
async def health():
    try:
        print("Performing health check...")
        conn = psycopg2.connect()
        curr = conn.cursor()
        curr.execute("SELECT 1")
        conn.close()
        print("Database connection successful.")
        # Check if the pretrained models can load
        AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
        AutoModel.from_pretrained(EMBEDDING_MODEL)
        pipeline("text2text-generation", model="facebook/bart-large-cnn")
        print("Pretrained models loaded successfully.")
        # Log the health check
        print("Health check passed: Database and models are accessible.")
        return {"status": "ok"}
    except Exception as e:
        print("Health check failed.")
        print(e)
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn

uvicorn.run(app, host="0.0.0.0", port=8000)

# app.run()
