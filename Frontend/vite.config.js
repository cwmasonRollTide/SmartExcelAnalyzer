import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],
    server: {
      port: 3000,
      host: 'localhost',
      strictPort: true,
      proxy: {
        '/api': {
          secure: false,
          changeOrigin: true,
          target: env.VITE_BASE_API_URL,
        },
        '/progressHub': {
          secure: false,
          changeOrigin: true,
          target: env.VITE_SIGNALR_HUB_URL,
        },
      },
    },
    build: {
      outDir: 'dist',
      sourcemap: true,
    },
  };
});