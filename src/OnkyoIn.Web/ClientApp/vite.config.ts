import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Proxy API calls to the OnkyoIn.Web backend during development.
    proxy: {
      '/api': {
        target: 'https://localhost:7261',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
