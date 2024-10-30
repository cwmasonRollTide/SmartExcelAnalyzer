module.exports = {
  testEnvironment: 'jest-environment-jsdom',
  transform: {
    '^.+\\.(ts|tsx)$': 'babel-jest',
  },
  moduleFileExtensions: ['ts', 'tsx', 'js', 'cjs', 'json'],
  setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
  globals: {
    'import.meta.env': global.importMetaEnv,
  },
  verbose: true,
};