import React from 'react';
import QueryResult from './QueryResult';
import { render, screen } from '@testing-library/react';
import { SubmitQueryResponse } from '../../interfaces/SubmitQueryResponse';

describe('QueryResult', () => {
  it('renders the query result', () => {
    const result = 'Test result';
    const question = 'Test question';
    const documentId = 'Test docId';

    const res: SubmitQueryResponse = {
      answer: result,
      question: question,
      documentId: documentId,
    };
    render(<QueryResult result={res} />);
    expect(screen.getByText(result)).toBeTruthy();
    render(<Token />);
  });
});

const Token: React.FC = () => {
  return <div>Token</div>;
};