import fs from 'fs'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

const baseFolder =
  process.env.APPDATA !== undefined && process.env.APPDATA !== ''
    ? `${process.env.APPDATA}/ASP.NET/https`
    : `${process.env.HOME}/.aspnet/https`

const certificateName = process.env.npm_package_name
const pemFilePath = `${baseFolder}/${certificateName}.pem`

export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  server: {
    port: 4200,
    https: {
      key: fs.readFileSync(`${baseFolder}/${certificateName}.key`),
      cert: fs.readFileSync(`${baseFolder}/${certificateName}.pem`),
    },
    proxy: {
      // Forward BFF API calls and specific auth paths to the backend
      '/bff': {
        target: process.env.ASPNETCORE_HTTPS_PORT
          ? `https://localhost:${process.env.ASPNETCORE_HTTPS_PORT}`
          : 'https://localhost:6001', // Your BFF URL
        secure: false,
      },
      // Add this entry to proxy the OIDC callback
      '/signin-oidc': {
        target: process.env.ASPNETCORE_HTTPS_PORT
          ? `https://localhost:${process.env.ASPNETCORE_HTTPS_PORT}`
          : 'https://localhost:6001', // Your BFF URL
        secure: false,
      },
      // Optional: You might also need to proxy the signout callback if using one
      '/signout-callback-oidc': {
        target: process.env.ASPNETCORE_HTTPS_PORT
          ? `https://localhost:${process.env.ASPNETCORE_HTTPS_PORT}`
          : 'https://localhost:6001', // Your BFF URL
        secure: false,
      },
      '/todos': {
        target: process.env.ASPNETCORE_HTTPS_PORT
          ? `https://localhost:${process.env.ASPNETCORE_HTTPS_PORT}`
          : 'https://localhost:6001', // Your API URL
        secure: false,
      }
    }
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
})
