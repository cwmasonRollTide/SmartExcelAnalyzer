import { getEnv } from "./src/utils/getEnv"
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const baseApiUrl = getEnv('VITE_BASE_API_URL', 'http://backend/api')
  const baseSignalRurl = getEnv('VITE_SIGNALR_HUB_URL', 'http://backend/progressHub');

  return {
    plugins: [react()],
    server: {
      port: 3000,
      host: "traefik",
      strictPort: true,
      proxy: {
        "/api": {
          target: baseApiUrl,
          changeOrigin: true,
          secure: false,
        },
        "/progressHub": {
          secure: false,
          changeOrigin: true,
          target: baseSignalRurl,
        },
      },
    },
    build: {
      outDir: "dist",
      sourcemap: true,
    },
  };
});