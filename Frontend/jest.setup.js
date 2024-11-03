global.importMetaEnv = {
  VITE_SIGNALR_HUB_URL: "http://localhost:3000/hub",
  VITE_BASE_API_URL: "http://localhost:3000/api",
};

Object.defineProperty(global, 'import', {
  value: {
    meta: {
      env: global.importMetaEnv,
    },
  },
});