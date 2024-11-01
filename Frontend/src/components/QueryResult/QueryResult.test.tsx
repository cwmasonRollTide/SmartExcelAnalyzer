import React from 'react';
import '@testing-library/jest-dom';
import QueryResult from './QueryResult';
import { QueryResultProps } from './QueryResultProps';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, jest } from '@jest/globals';

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
    expect(screen.getByText('Query Result')).toBeTruthy();
    expect(screen.getByText(`Question: ${question}`)).toBeTruthy();
    expect(screen.getByText(`Answer: ${result}`)).toBeTruthy();
    expect(screen.getByText(`Document ID: ${documentId}`)).toBeTruthy();
  });
});