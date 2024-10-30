import axios from 'axios';
import { SubmitQueryResponse } from './SubmitQueryResponse';

export const uploadFile = async (file: File): Promise<string> => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('filename', file.name);
  const response = await axios.post('/api/analysis/upload', formData, {
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
  const response = await axios.post<SubmitQueryResponse>('/api/analysis/query', {
    query,
    documentId,
  });
  return response.data;
};
