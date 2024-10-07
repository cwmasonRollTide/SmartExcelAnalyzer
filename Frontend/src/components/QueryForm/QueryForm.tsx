import React, { useState } from 'react';
import { TextField, Button, Box } from '@mui/material';

interface QueryFormProps {
  onQuerySubmit: (question: string) => void;
  isDocumentSelected: boolean;
}

const QueryForm: React.FC<QueryFormProps> = ({ onQuerySubmit, isDocumentSelected }) => {
  const [question, setQuestion] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (question.trim()) {
      onQuerySubmit(question);
      setQuestion('');
    }
  };

  return (
    <Box component="form" onSubmit={handleSubmit} sx={{ mt: 2 }}>
      <TextField
        fullWidth
        label="Ask a question about the selected document"
        variant="outlined"
        value={question}
        onChange={(e) => setQuestion(e.target.value)}
        disabled={!isDocumentSelected}
        sx={{ mb: 2 }}
      />
      <Button
        type="submit"
        variant="contained"
        disabled={!isDocumentSelected || !question.trim()}
      >
        Submit Query
      </Button>
    </Box>
  );
};

export default QueryForm;