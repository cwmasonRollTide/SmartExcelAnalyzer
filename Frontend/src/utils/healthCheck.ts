import axios from 'axios';

const HEALTH_CHECK_INTERVAL = 60000; // 1 minute

export function startHealthCheck() {
  const checkHealth = async () => {
    try {
      const response = await axios.get('/api/health');
      if (response.data.status === 'OK') {
        console.log('Health check passed');
      } else {
        console.error('Health check failed');
      }
    } catch (error) {
      console.error('Health check failed', error);
    }
  };

  checkHealth();

  return setInterval(checkHealth, HEALTH_CHECK_INTERVAL);
}