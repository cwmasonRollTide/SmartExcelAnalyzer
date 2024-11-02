import React from 'react';
import '@testing-library/jest-dom';
import FileUpload from './FileUpload';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { describe, expect, it, jest } from '@jest/globals';
import { render, screen, fireEvent } from '@testing-library/react';
let connectionBuilder: HubConnectionBuilder = new HubConnectionBuilder();

describe('FileUpload', () => {
  beforeAll(() => {
    process.env.VITE_SIGNALR_HUB_URL = 'http://localhost:5001';
    const mockHubConnectionBuilder = {
      withUrl: jest.fn((url: string) : HubConnectionBuilder => { return connectionBuilder.withUrl(url); }),
      withAutomaticReconnect: jest.fn(() : HubConnectionBuilder => { return connectionBuilder.withAutomaticReconnect(); }),
      configureLogging: jest.fn(() : HubConnectionBuilder  =>  { return connectionBuilder.configureLogging(1); }),
      withServerTimeout: jest.fn(() : HubConnectionBuilder => { return connectionBuilder.withServerTimeout(10000); }),
      withKeepAliveInterval: jest.fn((): HubConnectionBuilder => { return connectionBuilder.withKeepAliveInterval(20000); }),
      withStatefulReconnect: jest.fn((): HubConnectionBuilder => { return connectionBuilder.withStatefulReconnect(); }),
      build: jest.fn((): HubConnection => {
        return {
          on: jest.fn((onMethod: string, callback: (arg1: number, arg2: number) => void) => {
            if (onMethod === 'progress') {
              callback(0.5, 0.75);
            }
          }),
          start: jest.fn(() => Promise.resolve().then(() => { console.log('SignalR Connected'); })),
        } as unknown as ReturnType<HubConnectionBuilder['build']>;
      }),
    };
  
    jest.mock('@microsoft/signalr', () => ({
      HubConnectionBuilder: mockHubConnectionBuilder,
      HubConnection: () => {
        return {
          on: () => {},
          start: () => {},
        };
      },
      LogLevel: { Information: 1 },
    }));
  });

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
    const connection = new HubConnectionBuilder()
      .withUrl(process.env.VITE_SIGNALR_HUB_URL as string)
      .build();
    
    const onProgressMock = jest.fn();
    connection.on('progress', onProgressMock);

    onProgressMock(0.5, 0.75);

    expect(onProgressMock).toHaveBeenCalledWith(0.5, 0.75);
    expect(screen.getByText(/Parsing Progress:/i)).toBeTruthy();
    expect(screen.getByText(/Saving Progress:/i)).toBeTruthy();
    expect(screen.getByRole('progressbar', { name: /Parsing Progress:/i })).toHaveProperty('aria-valuenow', '50');
    expect(screen.getByRole('progressbar', { name: /Saving Progress:/i })).toHaveProperty('aria-valuenow', '75');
  });
});
