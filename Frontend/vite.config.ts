import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import dotenv from 'dotenv';

dotenv.config();

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    host: 'localhost',
    strictPort: true,
    proxy: {
      '/api': {
        secure: false,
        changeOrigin: true,
        target: process.env.VITE_BASE_API_URL,
      },
      '/progressHub': {
        secure: false,
        changeOrigin: true,
        target: process.env.VITE_SIGNALR_HUB_URL,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
});
