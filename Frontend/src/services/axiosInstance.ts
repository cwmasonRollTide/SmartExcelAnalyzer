import axios from 'axios';
const baseUrl = import.meta.env.VITE_BASE_API_URL as string;

const axiosInstance = axios.create({
  baseURL: `${baseUrl}/api`,
  withCredentials: false,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

export default axiosInstance;