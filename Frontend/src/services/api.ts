
/**
 * Uploads a file to the analysis/upload endpoint for the llm to interpret
 * its data and save to the database
 * 
 * @param {File} file - The file to be uploaded.
 * @returns {Promise<string>} - A promise that resolves to the server's response.
 * @throws {Error} - Throws an error if the file upload fails.
 */
export const uploadFile = async (file: File): Promise<string> => {
  try {
    const formData = new FormData();
    formData.append('file', file);
    const response = await fetch(`${import.meta.env.VITE_BASE_API_URL}/api/analysis/upload`, {
      method: 'POST',
      body: formData,
    });

    if (!response.ok) {
      throw new Error('File upload failed');
    }
    return response.json();
  } catch (error) {
    console.error(error);
    throw new Error('File upload failed');
  }
}

type Query = {
  question: string;
  documentId: string;
};

export interface QueryResponse extends Query {
  answer: string;
  relevantRows: any[];
}

/**
 * Ask a question about a document that has been uploaded to the server.
 * Computes the text of the question from the LLM and compares it to the
 * data from the document its already converted to get relevant answers
 * @param query 
 * @returns 
 */
export const submitQuery = async (query: Query): Promise<QueryResponse> => {
  const response = await fetch(`${import.meta.env.VITE_BASE_API_URL}/api/analysis/query`, {
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