import React from 'react';
import QueryResult from './QueryResult';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { QueryResultProps } from '../../interfaces/QueryResultProps';

describe('QueryResult', () => {
  it('renders the query result', async () => {
    const result = 'Test result';
    const question = 'Test question'; 
    const documentId = 'Test docId';

    const res: QueryResultProps = {
      result: {
        answer: result,
        question,
        documentId,
        relevantRows: [],
      }
    };
    
    render(<QueryResult {...res} />);
    await waitFor(() => {
      expect(screen.getByText('Query Result')).toBeInTheDocument();
    });
  });
});
