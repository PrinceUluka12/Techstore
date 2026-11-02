import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:5010',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': { // the back office's live alerts
        target: 'https://localhost:5010',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
      '/uploads': {
        target: 'https://localhost:5010',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})