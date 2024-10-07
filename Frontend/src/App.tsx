import React, { useState, useEffect } from 'react';
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material';
import { Container, Paper, Snackbar, Alert, Box } from '@mui/material';
import { startHealthCheck } from './utils/healthCheck';
import { uploadFile, submitQuery, QueryResponse } from './services/api';
import DocumentList from './components/DocumentList/DocumentList';
import FileUpload from './components/FileUpload/FileUpload';
import QueryForm from './components/QueryForm/QueryForm';
import QueryResult from './components/QueryResult/QueryResult';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
  },
});

interface Document {
  id: string;
  name: string;
}

function App() {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [selectedDocument, setSelectedDocument] = useState<Document | null>(null);
  const [queryResult, setQueryResult] = useState<QueryResponse | null>(null);
  const [toastOpen, setToastOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState('');
  const [toastSeverity, setToastSeverity] = useState<'success' | 'error'>('success');

  useEffect(() => {
    const healthCheckInterval = startHealthCheck();
    return () => clearInterval(healthCheckInterval);
  }, []);

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
        const result = await submitQuery({
          question,
          documentId: selectedDocument.id,
        });
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
    if (reason === 'clickaway') {
      return;
    }
    setToastOpen(false);
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
          <Box sx={{ flexBasis: { xs: '100%', md: '30%' }, flexGrow: 1 }}>
            <Paper
              sx={{
                p: 2,
                display: 'flex',
                flexDirection: 'column',
                height: 240,
              }}
            >
              <FileUpload onFileUpload={handleFileUpload} />
              <DocumentList
                documents={documents}
                selectedDocument={selectedDocument}
                onSelectDocument={handleDocumentSelect}
              />
            </Paper>
          </Box>
          <Box sx={{ flexBasis: { xs: '100%', md: '65%' }, flexGrow: 1 }}>
            <Paper
              sx={{
                p: 2,
                display: 'flex',
                flexDirection: 'column',
                height: 240,
                mb: 2,
              }}
            >
              <QueryForm
                onQuerySubmit={handleQuerySubmit}
                isDocumentSelected={!!selectedDocument}
              />
            </Paper>
            {queryResult && (
              <Paper sx={{ p: 2, display: 'flex', flexDirection: 'column' }}>
                <QueryResult result={queryResult} />
              </Paper>
            )}
          </Box>
        </Box>
      </Container>
      <Snackbar open={toastOpen} autoHideDuration={6000} onClose={handleToastClose}>
        <Alert onClose={handleToastClose} severity={toastSeverity} sx={{ width: '100%' }}>
          {toastMessage}
        </Alert>
      </Snackbar>
    </ThemeProvider>
  );
}

export default App;