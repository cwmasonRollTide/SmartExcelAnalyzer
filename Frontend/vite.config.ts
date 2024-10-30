require('dotenv').config();
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    host: true,
    strictPort: true,
    proxy: {
      '/api': {
        target: process.env.VITE_BASE_API_URL,
        changeOrigin: false,
        secure: false,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
});
