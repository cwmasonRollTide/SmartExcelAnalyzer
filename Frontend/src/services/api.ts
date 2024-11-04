import axios from 'axios';
import { SubmitQueryResponse } from './SubmitQueryResponse';

const CHUNK_SIZE = 5 * 1024 * 1024; // 5MB

export const uploadFileInChunks = async (file: File): Promise<string> => {
  const totalChunks = Math.ceil(file.size / CHUNK_SIZE);
  const uploadId = await initializeUpload(file.name);

  for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
    const start = chunkIndex * CHUNK_SIZE;
    const end = Math.min(start + CHUNK_SIZE, file.size);
    const chunk = file.slice(start, end);

    const formData = new FormData();
    formData.append('file', chunk);
    formData.append('filename', file.name);
    formData.append('chunkIndex', chunkIndex.toString());
    formData.append('totalChunks', totalChunks.toString());
    formData.append('uploadId', uploadId);

    await axios.post('/analysis/upload-chunk', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }

  return finalizeUpload(uploadId);
};

const initializeUpload = async (filename: string): Promise<string> => {
  const response = await axios.post('/analysis/initialize-upload', { filename });
  return response.data.uploadId;
};

const finalizeUpload = async (uploadId: string): Promise<string> => {
  const response = await axios.post('/analysis/finalize-upload', { uploadId });
  return response.data.uploadId;
};

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


