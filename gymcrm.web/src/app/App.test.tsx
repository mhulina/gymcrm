import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

test('redirects to the login page when signed out', async () => {
  render(<App />);
  expect(await screen.findByRole('heading', { name: /sign in/i })).toBeInTheDocument();
});
