import React, { useState } from 'react';
import { 
  TextField, 
  Button, 
  Box 
} from '@mui/material';
import { QueryFormProps } from '../../interfaces/QueryFormProps';

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
    <Box
      sx={{ mt: 2 }}
      component="form" 
      onSubmit={handleSubmit} 
    >
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