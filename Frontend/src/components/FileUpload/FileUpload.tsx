import React, { useState, useEffect } from 'react';
import { Button, Typography, Box, LinearProgress } from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

const SIGNALR_HUB_URL = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5001';

interface FileUploadProps {
  onFileUpload: (file: File) => void;
}

const FileUpload: React.FC<FileUploadProps> = ({ onFileUpload }) => {
  const [dragActive, setDragActive] = useState(false);
  const [parseProgress, setParseProgress] = useState(0);
  const [saveProgress, setSaveProgress] = useState(0);
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl(`${SIGNALR_HUB_URL}`)  // Updated to use the full URL
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('SignalR Connected');
          connection.on('ReceiveProgress', (parseProgress: number, saveProgress: number) => {
            setParseProgress(parseProgress * 100);
            setSaveProgress(saveProgress * 100);
          });
        })
        .catch((err: Error) => console.error('SignalR Connection Error: ', err));
    }
  }, [connection]);

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      onFileUpload(e.dataTransfer.files[0]);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    e.preventDefault();
    if (e.target.files && e.target.files[0]) {
      onFileUpload(e.target.files[0]);
    }
  };

  return (
    <Box
      onDrop={handleDrop}
      onDragOver={handleDrag}
      onDragEnter={handleDrag}
      onDragLeave={handleDrag}
      sx={{
        border: '2px dashed',
        borderColor: dragActive ? 'primary.main' : 'grey.300',
        borderRadius: 2,
        p: 2,
        textAlign: 'center',
        cursor: 'pointer',
      }}
    >
      <input
        type="file"
        id="file-upload"
        style={{ display: 'none' }}
        onChange={handleChange}
        accept=".xlsx,.xls,.csv"
      />
      <label htmlFor="file-upload">
        <Button
          component="span"
          variant="contained"
          startIcon={<CloudUploadIcon />}
        >
          Upload Excel or CSV
        </Button>
      </label>
      <Typography variant="body2" sx={{ mt: 2 }}>
        Drag and drop your file here or click to select
      </Typography>
      {(parseProgress > 0 || saveProgress > 0) && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2">Parsing Progress:</Typography>
          <LinearProgress variant="determinate" value={parseProgress} />
          <Typography variant="body2" sx={{ mt: 1 }}>Saving Progress:</Typography>
          <LinearProgress variant="determinate" value={saveProgress} />
        </Box>
      )}
    </Box>
  );
};

export default FileUpload;