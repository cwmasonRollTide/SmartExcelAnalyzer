import React, { act } from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import QueryForm from './QueryForm';
import { describe, expect, it, jest } from '@jest/globals';

describe('QueryForm', () => {
  it('renders the query form', () => {
    render(<QueryForm onQuerySubmit={jest.fn()} isDocumentSelected={true} />);
    expect(screen.getByLabelText('Ask a question about the selected document')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Submit Query' })).toBeTruthy();
  });

  it('calls onQuerySubmit with query when submitted', async () => {
    const onQuerySubmitMock = jest.fn();
    render(<QueryForm onQuerySubmit={onQuerySubmitMock} isDocumentSelected={true} />);
    
    const input = screen.getByLabelText('Ask a question about the selected document');
    const query = 'Test query';
    await act(async () => {
      fireEvent.change(input, { target: { value: query } });
      fireEvent.click(screen.getByRole('button', { name: 'Submit Query' }));
    });
    
    await waitFor(() => {
      expect(onQuerySubmitMock).toHaveBeenCalledWith(query);
    });
  });
});
