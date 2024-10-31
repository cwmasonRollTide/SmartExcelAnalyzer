import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import dotenv from 'dotenv';

dotenv.config();

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5001,
    host: true,
    strictPort: true,
    proxy: {
      '/api': {
        secure: true,
        changeOrigin: false,
        target: process.env.VITE_BASE_API_URL,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
});
