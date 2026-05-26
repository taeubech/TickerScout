import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const BACKEND_URL = 'http://localhost:5051'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/hubs': {
        target: BACKEND_URL,
        ws: true,
      },
      '/api': {
        target: BACKEND_URL,
      },
    },
  },
})
