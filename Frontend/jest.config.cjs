module.exports = {
  testEnvironment: 'jest-environment-jsdom',
  transform: {
    '^.+\\.(ts|tsx|js|jsx)$': 'babel-jest',
  },
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
  verbose: true,
  globals: {
    'process.env': {
      VITE_BASE_API_URL: 'http://backend:5001/api',
      VITE_SIGNALR_HUB_URL: 'http://backend:5001/progressHub',
    },
  },
};
