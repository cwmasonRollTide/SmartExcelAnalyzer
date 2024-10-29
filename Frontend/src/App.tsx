import React, { useState } from 'react';
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material';
import { Paper, Snackbar, Alert, Box } from '@mui/material';
import { uploadFile, submitQuery } from './services/api';
import { SubmitQueryResponse } from './interfaces/SubmitQueryResponse.ts';
import DocumentList from './components/DocumentList/DocumentList';
import FileUpload from './components/FileUpload/FileUpload';
import QueryForm from './components/QueryForm/QueryForm';
import QueryResult from './components/QueryResult/QueryResult.tsx';
import { Document } from './interfaces/Document.tsx';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
    background: {
      default: '#f0f4f8',
    },
  },
});

function App() {
  const [toastOpen, setToastOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState('');
  const [documents, setDocuments] = useState<Document[]>([]);
  const [queryResult, setQueryResult] = useState<SubmitQueryResponse | null>(null);
  const [selectedDocument, setSelectedDocument] = useState<Document | null>(null);
  const [toastSeverity, setToastSeverity] = useState<'success' | 'error'>('success');

  const handleFileUpload = async (file: File) => {
    try {
      const documentId = await uploadFile(file);
      const newDocument = { id: documentId, name: file.name };
      setDocuments([...documents, newDocument]);
      showToast('File uploaded successfully', 'success');
    } catch (error) {
      console.error('File upload failed:', error);
      showToast('File upload failed', 'error');
    }
  };

  const handleDocumentSelect = (document: Document) => {
    setSelectedDocument(document);
    setQueryResult(null);
  };

  const handleQuerySubmit = async (question: string) => {
    if (selectedDocument) {
      try {
        const result = await submitQuery(
          question,
          selectedDocument.id
        );
        setQueryResult(result);
      } catch (error) {
        console.error('Query submission failed:', error);
        showToast('Query submission failed', 'error');
      }
    }
  };

  const showToast = (message: string, severity: 'success' | 'error') => {
    setToastMessage(message);
    setToastSeverity(severity);
    setToastOpen(true);
  };

  const handleToastClose = (_event?: React.SyntheticEvent | Event, reason?: string) => {
    if (reason === 'clickaway') return;
    setToastOpen(false);
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box
        sx={{
          minHeight: '100vh',
          width: '100vw',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          bgcolor: 'background.default',
        }}
      >
        <Box
          sx={{
            width: '100%',
            maxWidth: '600px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 3,
            p: 3,
          }}
        >
          <Paper
            elevation={3}
            sx={{
              p: 3,
              width: '100%',
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
            }}
          >
            <FileUpload onFileUpload={handleFileUpload} />
            <Box sx={{ mt: 2, width: '100%' }}>
              <DocumentList
                documents={documents}
                selectedDocument={selectedDocument}
                onSelectDocument={handleDocumentSelect}
              />
            </Box>
            <Box sx={{ mt: 2, width: '100%' }}>
              <QueryForm
                onQuerySubmit={handleQuerySubmit}
                isDocumentSelected={!!selectedDocument}
              />
            </Box>
          </Paper>
          {queryResult && (
            <Paper elevation={3} sx={{ p: 3, width: '100%'}}>
              <QueryResult result={queryResult} />
            </Paper>
          )}
        </Box>
      </Box>
      <Snackbar 
        open={toastOpen} 
        autoHideDuration={6000} 
        onClose={handleToastClose}
      >
        <Alert 
          onClose={handleToastClose} 
          severity={toastSeverity} 
          sx={{ width: '100%' }}
        >
          {toastMessage}
        </Alert>
      </Snackbar>
    </ThemeProvider>
  );
}

export default App;