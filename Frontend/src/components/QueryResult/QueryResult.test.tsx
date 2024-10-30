import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import QueryResult from './QueryResult';
import { QueryResultProps } from '../../interfaces/QueryResultProps';

describe('QueryResult', () => {
  it('renders the query result', () => {
    const result = 'Test result';
    const question = 'Test question';
    const documentId = 'Test docId';

    const res: QueryResultProps = {
      result: {
        answer: result,
        question,
        documentId,
        relevantRows: [{ key: 'value' }],
      },
    };

    render(<QueryResult {...res} />);
    expect(screen.getByText('Query Result')).toBeInTheDocument();
    expect(screen.getByText(`Question: ${question}`)).toBeInTheDocument();
    expect(screen.getByText(`Answer: ${result}`)).toBeInTheDocument();
    expect(screen.getByText(`Document ID: ${documentId}`)).toBeInTheDocument();
  });
});