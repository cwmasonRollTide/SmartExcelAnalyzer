// import { defineConfig } from 'vite';
// import react from '@vitejs/plugin-react';

// export default defineConfig({
//   plugins: [react()],
//   server: {
//       port: 3000,
//       host: '0.0.0.0',
//       strictPort: true,
//       hmr: {
//         clientPort: 3000,
//       }
//       // watch: {
//       //    usePolling: true,
//       // },
//     // proxy: {
//     //   '/api': {
//     //     target: 'http://localhost:5001',
//     //     changeOrigin: true,
//     //     secure: false,
//     //   },
//     // },
//   },
//   build: {
//     outDir: 'dist',
//     sourcemap: true,
//   },
// });

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: true, // This binds to all IPv4 and IPv6 addresses
    port: 3000,
    strictPort: true,
  },
})