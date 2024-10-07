import os
import json
import torch
import psycopg2
from pydantic import BaseModel
from transformers import pipeline
from fastapi import FastAPI, HTTPException
from psycopg2.extras import RealDictCursor
from LLM.config import EnvironmentVariables
from transformers import AutoTokenizer, AutoModel

default_text_generation_model = "facebook/bart-large-cnn"
default_embedding_model = "sentence-transformers/all-MiniLM-L6-v2"
default_db_connection_string = "dbname=smartexcelanalyzer user=admin password=password host=localhost sslmode=disable"

EMBEDDING_MODEL = os.getenv(EnvironmentVariables.EMBEDDING_MODEL, default_embedding_model)
DB_CONNECTION_STRING = os.getenv(EnvironmentVariables.DB_CONNECTION_STRING, default_db_connection_string)
TEXT_GENERATION_MODEL = os.getenv(EnvironmentVariables.TEXT_GENERATION_MODEL, default_text_generation_model)

app = FastAPI()
model = pipeline("text2text-generation", model=TEXT_GENERATION_MODEL)

class Query(BaseModel):
    document_id: str
    question: str

class QueryResponse(Query): 
    answer: str
    relevantRows: list

class ComputeEmbedding(BaseModel):
    text: str

@app.get("/health", response_model=dict)
async def health():
    """
    Perform a health check by testing the connection to the database and loading the pretrained models.

    Returns:
        dict: A dictionary containing the status of the health check.
    """
    try:
        print("Performing health check...")
        conn = psycopg2.connect()
        curr = conn.cursor()
        curr.execute("SELECT 1")
        conn.close()
        print("Database connection successful.")
        AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
        AutoModel.from_pretrained(EMBEDDING_MODEL)
        pipeline("text2text-generation", model=TEXT_GENERATION_MODEL)
        print("Pretrained models loaded successfully.")
        print("Health check passed: LLM Service Endpoint, Vector Database and LLM Models are all accessible.")
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
        context = json.dumps(relevant_rows)
        summary_text = json.dumps(summary)
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
        tokenizer = AutoTokenizer.from_pretrained(EMBEDDING_MODEL)
        embedding_model = AutoModel.from_pretrained(EMBEDDING_MODEL)
        inputs = tokenizer(compute_embedding.text, return_tensors="pt", truncation=True, padding=True)
        with torch.no_grad():
            embeddings = embedding_model(**inputs).last_hidden_state.mean(dim=1)
        return embeddings.numpy().tolist()[0]
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn

uvicorn.run(app, host="0.0.0.0", port=8000)
