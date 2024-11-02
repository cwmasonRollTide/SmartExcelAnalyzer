module.exports = {
  testEnvironment: 'jest-environment-jsdom',
  transform: {
    '^.+\\.(ts|tsx|js|jsx)$': 'babel-jest',
  },
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
  globals: {
    'import.meta.env': global.importMetaEnv,
    VITE_SIGNALR_HUB_URL: 'http://localhost:5001',
  },
  verbose: true,
};