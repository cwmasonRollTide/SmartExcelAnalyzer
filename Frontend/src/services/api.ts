import axios from 'axios';
import { SubmitQueryResponse } from './SubmitQueryResponse';

const API_URL = import.meta.env.VITE_BASE_API_URL || 'http://localhost:5001/api';

export const uploadFile = async (file: File): Promise<string> => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('filename', file.name);
  const response = await axios.post(`${API_URL}/analysis/upload`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

export const submitQuery = async (
  query: string, 
  documentId: string
): Promise<SubmitQueryResponse> => {
  const response = await axios.post<SubmitQueryResponse>(`${API_URL}/analysis/query`, {
    query,
    document_id: documentId,
  });
  return response.data;
};
