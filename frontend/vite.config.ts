import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // FlashSales.Api dev server: keeps the browser same-origin in dev;
      // production fronts both apps behind the same gateway.
      '/api': {
        target: 'http://localhost:5038',
        changeOrigin: true,
      },
    },
  },
})
