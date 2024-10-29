import axios from 'axios';
import { SubmitQueryResponse } from '../interfaces/SubmitQueryResponse';

export const uploadFile = async (file: File): Promise<string> => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('filename', file.name);
  const response = await axios.post('/analysis/upload', formData, {
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
  const response = await axios.post<SubmitQueryResponse>('/analysis/query', {
    query,
    document_id: documentId,
  });
  return response.data;
};
