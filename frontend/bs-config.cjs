const { createProxyMiddleware } = require('http-proxy-middleware');

const BACKEND_URL = process.env.BACKEND_URL || 'http://backend:3001';

console.log('[bs-config] Proxying /api ->', BACKEND_URL);

module.exports = {
  server: {
    baseDir: 'public',
    middleware: [
      createProxyMiddleware({
        target: BACKEND_URL,
        changeOrigin: true,
        pathFilter: '/api',
      }),
    ],
  },
  files: ['public/**/*.{html,css,js}'],
  port: 3000,
  ui: false,
  notify: false,
  open: false,
};
