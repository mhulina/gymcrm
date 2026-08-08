// jest-dom adds custom jest matchers for asserting on DOM nodes.
// allows you to do things like:
// expect(element).toHaveTextContent(/react/i)
// learn more: https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom';

// jest-environment-jsdom (pinned by react-scripts) doesn't expose Node's
// TextEncoder/TextDecoder on the jsdom global, but react-router v7 needs them
// at import time. Polyfill from Node's own util module.
import { TextEncoder, TextDecoder } from 'util';
Object.assign(global, { TextEncoder, TextDecoder });

// jsdom doesn't implement window.matchMedia, but ThemeContext uses it to read
// the system color-scheme preference. Stub it out with a deterministic default.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});
