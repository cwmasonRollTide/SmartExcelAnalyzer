import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import QueryForm from './QueryForm';

describe('QueryForm', () => {
  it('renders the query form', () => {
    render(<QueryForm onQuerySubmit={() => jest.fn(() => true)} isDocumentSelected={true} />);
    expect(screen.getByLabelText('Ask a question about the selected document')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Submit Query' })).toBeTruthy();
  });

  it('calls onQuerySubmit with query when submitted', () => {
    const onQuerySubmitMock = jest.fn();
    render(<QueryForm onQuerySubmit={onQuerySubmitMock} isDocumentSelected={true} />);
    
    const input = screen.getByLabelText('Ask a question about the selected document');
    const query = 'Test query';
    fireEvent.change(input, { target: { value: query } });
    fireEvent.click(screen.getByRole('button', { name: 'Submit Query' }));
    
    expect(onQuerySubmitMock).toHaveBeenCalledWith(query);
  });
});
