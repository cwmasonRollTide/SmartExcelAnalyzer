import axios from 'axios';

const API_URL = import.meta.env.VITE_BASE_API_URL || 'http://localhost:5001/api';

export const uploadFile = async (file: File): Promise<string> => {
  const formData = new FormData();
  formData.append('file', file);
  const response = await axios.post(`${API_URL}/analysis/upload`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

export interface QueryResponse {
  answer: string;
  question: string;
  documentId: string;
  relevantRows: Record<string, unknown>[];
}

export const submitQuery = async (
  query: string, 
  documentId: string
): Promise<QueryResponse> => {
  const response = await axios.post<QueryResponse>(`${API_URL}/analysis/query`, {
    query,
    documentId,
  });
  return response.data;
};
