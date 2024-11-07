import React, { useState, useEffect } from 'react';
import { Typography, Box, LinearProgress } from '@mui/material';
import { FileUpload as MuiFileUpload } from '@mui/icons-material';
import { useDropzone } from 'react-dropzone';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { FileUploadProps } from './FileUploadProps';
import { getEnv } from '../../utils/getEnv';

const SIGNALR_HUB_URL = import.meta.env.VITE_SIGNALR_HUB_URL || getEnv('VITE_SIGNALR_HUB_URL', '');

const FileUpload: React.FC<FileUploadProps> = ({ onFileUpload }): React.ReactElement => {
  const [parseProgress, setParseProgress] = useState(0);
  const [saveProgress, setSaveProgress] = useState(0);
  const [connection, setConnection] = useState<HubConnection | null>(null);

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl(SIGNALR_HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .withServerTimeout(10000)
      .withKeepAliveInterval(20000)
      .withStatefulReconnect()
      .build();
    newConnection.onclose((error) => {
      console.log('SignalR Closed: ', error);
    });
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

  const onDrop = (acceptedFiles: File[]) => {
    if (acceptedFiles.length > 0) {
      onFileUpload(acceptedFiles[0]);
    }
  };

  const {getRootProps, getInputProps, isDragActive} = useDropzone({
    onDrop,
    accept: {
      'application/vnd.ms-excel': ['.xls', '.xlsx'],
      'text/csv': ['.csv'],
    },
    maxFiles: 1,
  });

  return (
    <Box
      {...getRootProps()}
      sx={{
        border: '2px dashed',
        borderColor: isDragActive ? 'primary.main' : 'grey.300',
        borderRadius: 2,
        p: 2,
        textAlign: 'center',
        cursor: 'pointer',
      }}
    >
      <input {...getInputProps()} />
      <MuiFileUpload sx={{ fontSize: 48 }} />
      <Typography variant="body2" sx={{ mt: 2 }}>
        {isDragActive ? 
          'Drop the file here...' :
          'Drag and drop your file here or click to select'
        }
      </Typography>
      {(parseProgress > 0 || saveProgress > 0) && (
        <Box sx={{ mt: 2 }}>
          <Typography variant="body2">
            Parsing Progress:
          </Typography>
          <LinearProgress 
            variant="determinate"
            value={parseProgress}
          />
          <Typography
            variant="body2"
            sx={{ mt: 1 }} 
          >
            Saving Progress:
          </Typography>
          <LinearProgress
            variant="determinate" 
            value={saveProgress}
          />
        </Box>
      )}
    </Box>
  );
};

export default FileUpload;
