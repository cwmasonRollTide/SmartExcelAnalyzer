import { getEnv } from "./src/utils/getEnv"
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const baseApiUrl = getEnv('VITE_BASE_API_URL', 'http://localhost:81/traefikfilter/api')
  const baseSignalRurl = getEnv('VITE_SIGNALR_HUB_URL', 'http://localhost:81/traefikfilter/progressHub');

  return {
    plugins: [react()],
    server: {
      port: 3000,
      host: "traefik",
      strictPort: true,
      proxy: {
        '/traefikfilter/api': {
          target: baseApiUrl,
          secure: false,
          // Since we removed /smartapi from the API calls
          rewrite: (path) => path.replace(/^\/traefikfilter\/api/, ''),
        },
        '/traefikfilter/progressHub': {
          target: baseSignalRurl,
          secure: false,
          ws: true,
          rewrite: (path) => path.replace(/^\/traefikfilter/, ''),
        },
      },
    },
    build: {
      outDir: "dist",
      sourcemap: true,
    },
  };
});