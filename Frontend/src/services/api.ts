const API_URL = import.meta.env.DEV ? 'http://localhost:5001' : '';

export const uploadFile = async (file: File): Promise<any> => {
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

export const submitQuery = async (query: string): Promise<any> => {
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