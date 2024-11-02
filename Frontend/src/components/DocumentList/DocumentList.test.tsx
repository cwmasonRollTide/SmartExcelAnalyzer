import React from 'react';
import '@testing-library/jest-dom';
import { render, screen, fireEvent } from '@testing-library/react';
import DocumentList from './DocumentList';
import { DocumentListProps } from './DocumentListProps';
import { describe, expect, it, jest } from '@jest/globals';

const mockDocuments = [
  { id: '1', name: 'Document 1' },
  { id: '2', name: 'Document 2' },
];

describe('DocumentList', () => {
  it('renders the document list', () => {
    render(<DocumentList documents={mockDocuments} selectedDocument={null} onSelectDocument={jest.fn()} />);
    expect(screen.getByText(/Document 1/i)).toBeTruthy();
    expect(screen.getByText(/Document 2/i)).toBeTruthy();
  });

  it('calls onSelectDocument when a document is clicked', () => {
    const onSelectDocumentMock = jest.fn();
    render(<DocumentList documents={mockDocuments} selectedDocument={null} onSelectDocument={onSelectDocumentMock} />);
    
    fireEvent.click(screen.getByText(/Document 1/i));
    expect(onSelectDocumentMock).toHaveBeenCalledWith(mockDocuments[0]);
  });

  it('highlights the selected document', () => {
    render(<DocumentList documents={mockDocuments} selectedDocument={mockDocuments[0]} onSelectDocument={jest.fn()} />);
    expect(screen.getByText(/Document 1/i).closest('button')).toHaveProperty('Mui-selected');
  });
});