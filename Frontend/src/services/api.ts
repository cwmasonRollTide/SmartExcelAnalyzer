const API_URL = import.meta.env.DEV ? 'http://localhost:5001' : '';

export const uploadFile = async (file: File): Promise<string> => {
  const formData = new FormData();
  formData.append('file', file);
  const response = await fetch(`${API_URL}/analysis/upload`, {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    throw new Error('File upload failed');
  }
  return response.json();
};

type Query = {
  question: string;
  documentId: string;
};

export interface QueryResponse extends Query {
  answer: string;
  relevantRows: any[];
}

export const submitQuery = async (query: Query): Promise<QueryResponse> => {
  const response = await fetch(`${API_URL}/analysis/query`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ query }),
  });

  if (!response.ok) {
    throw new Error('Query submission failed');
  }
  return response.json();
};