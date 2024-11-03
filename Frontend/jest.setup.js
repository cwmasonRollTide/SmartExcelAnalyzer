global.importMetaEnv = {
  VITE_SIGNALR_HUB_URL: "http://localhost:5001/hub",
  VITE_BASE_API_URL: "http://localhost:5001/api",
};

Object.defineProperty(global, 'import', {
  value: {
    meta: {
      env: global.importMetaEnv,
    },
  },
});