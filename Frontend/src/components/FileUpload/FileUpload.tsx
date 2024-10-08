import React, { useState } from 'react';
import { Button, Typography, Box } from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';

interface FileUploadProps {
  onFileUpload: (file: File) => void;
}

const FileUpload: React.FC<FileUploadProps> = ({ onFileUpload }) => {
  const [dragActive, setDragActive] = useState(false);

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
    </Box>
  );
};

export default FileUpload;