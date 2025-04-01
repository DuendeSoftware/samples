import fs from 'fs'
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import { env } from 'process';
import path from 'path';
import child_process from 'child_process';

const baseFolder =
  env.APPDATA !== undefined && env.APPDATA !== ''
    ? `${env.APPDATA}/ASP.NET/https`
    : `${env.HOME}/.aspnet/dev-certs/https`;

const certificateName = "vue.client"; // Change this to your app name
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
  if (0 !== child_process.spawnSync('dotnet', [
    'dev-certs',
    'https',
    '--export-path',
    certFilePath,
    '--format',
    'Pem',
    '--no-password',
  ], { stdio: 'inherit', }).status) {
    throw new Error("Could not create certificate.");
  }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:6001';


export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  server: {
    port: 4200,
    https: {
      key: fs.readFileSync(keyFilePath),
      cert: fs.readFileSync(certFilePath),
    },
    proxy: {
      '/bff': {
       target,
        secure: false,
      },
      '/signin-oidc': {
       target,
        secure: false,
      },
      '/signout-callback-oidc': {
        target,
        secure: false,
      },
      '/todos': {
        target,
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
