import pytest
from fastapi.testclient import TestClient
from main import app

client = TestClient(app)

def test_health():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok"}

def test_query():
    response = client.post("/query", json={"document_id": "doc1", "question": "What is AI?"})
    assert response.status_code == 200
    assert "answer" in response.json()

def test_compute_embedding():
    response = client.post("/compute_embedding", json={"text": "sample text"})
    assert response.status_code == 200
    assert isinstance(response.json(), list)

def test_compute_batch_embedding():
    response = client.post("/compute_batch_embedding", json={"texts": ["text1", "text2"]})
    assert response.status_code == 200
    assert isinstance(response.json(), list)
    assert all(isinstance(item, list) for item in response.json())