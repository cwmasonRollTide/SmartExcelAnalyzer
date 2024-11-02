import React from 'react';
import '@testing-library/jest-dom';
import FileUpload from './FileUpload';
import { describe, expect, it, jest } from '@jest/globals';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { render, screen, fireEvent } from '@testing-library/react';

jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockImplementation(() => ({
    withUrl: jest.fn().mockReturnThis(),
    withAutomaticReconnect: jest.fn().mockReturnThis(),
    configureLogging: jest.fn().mockReturnThis(),
    build: jest.fn().mockReturnValue({
      start: jest.fn(),
      on: jest.fn((methodName: string, newMethod: (arg1: number, arg2: number) => void) => {
        if (methodName === 'progress') {
          newMethod(0.5, 0.75);
        }
      }),
    }),
  })),
}));

  describe('FileUpload', () => {
    it('renders the file upload component', () => {
      render(<FileUpload onFileUpload={jest.fn()} />);
      expect(screen.getByText(/Drag and drop your file here or click to select/i)).toBeTruthy();
    });

    it('calls onFileUpload when a file is dropped', async () => {
      const onFileUploadMock = jest.fn();
      render(<FileUpload onFileUpload={onFileUploadMock} />);
      
      const file = new File(['dummy content'], 'example.xlsx', { type: 'application/vnd.ms-excel' });
      const input = screen.getByLabelText(/Drag and drop your file here or click to select/i);
      
      fireEvent.drop(input, {
        target: { files: [file] },
      });

      expect(onFileUploadMock).toHaveBeenCalledWith(file);
    });

    it('shows progress bars when progress is received', async () => {
      render(<FileUpload onFileUpload={jest.fn()} />);
      const connection = new HubConnectionBuilder().withUrl('').build();
      connection.on('progress', (parsingProgress: number, savingProgress: number) => {
      expect(parsingProgress).toBe(0.5);
      expect(savingProgress).toBe(0.75);
    
      expect(screen.getByText(/Parsing Progress:/i)).toBeTruthy();
      expect(screen.getByText(/Saving Progress:/i)).toBeTruthy();
      expect(screen.getByRole('progressbar', { name: /Parsing Progress:/i })).toHaveProperty('aria-valuenow', '50');
      expect(screen.getByRole('progressbar', { name: /Saving Progress:/i })).toHaveProperty('aria-valuenow', '75');
    });
  });
});
