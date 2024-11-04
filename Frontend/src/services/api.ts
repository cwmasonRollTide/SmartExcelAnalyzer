import axios from 'axios';
import { SubmitQueryResponse } from './SubmitQueryResponse';
import { FinalizeResponse } from './FinalizeResponse';
import { ChunkedUploadResponse } from './ChunkedUploadResponse';

const CHUNK_SIZE = 5 * 1024 * 1024; // 5MB

export const uploadFileInChunks = async (file: File): Promise<ChunkedUploadResponse> => {
  const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
  const documentId = await initializeUpload(file.name);

  for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
    const start = chunkIndex * CHUNK_SIZE;
    const end = Math.min(start + CHUNK_SIZE, file.size);
    const chunk = file.slice(start, end);

    const formData = new FormData();
    formData.append('file', chunk);
    formData.append('chunkIndex', chunkIndex.toString());
    formData.append('totalChunks', totalChunks.toString());

    await axios.post('/analysis/upload-chunk', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  return finalizeUpload(documentId);
};

const initializeUpload = async (filename: string): Promise<string> => {
  const response = await axios.post('/analysis/initialize-upload', { filename });
  return response.data.documentId;
};

const finalizeUpload = async (documentId: string): Promise<FinalizeResponse> => {
  const response = await axios.post('/analysis/finalize-upload', { documentId });
  return response.data;
};

export const uploadFile = async (file: File): Promise<string> => {
  const formData = new FormData();
  formData.append('file', file);

  const response = await axios.post('/analysis/upload', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });

  return response.data.documentId;
}

export const submitQuery = async (
  query: string, 
  documentId: string
): Promise<SubmitQueryResponse> => {
  const response = await axios.post<SubmitQueryResponse>('/analysis/query', {
    query,
    documentId,
  });
  return response.data;
};


