import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
      port: 3000,
      host: '0.0.0.0',
      hmr: {
        clientPort: 3000,
        host: 'localhost',
      }
      // watch: {
      //    usePolling: true,
      // },
    // proxy: {
    //   '/api': {
    //     target: 'http://localhost:5001',
    //     changeOrigin: true,
    //     secure: false,
    //   },
    // },
  },
});