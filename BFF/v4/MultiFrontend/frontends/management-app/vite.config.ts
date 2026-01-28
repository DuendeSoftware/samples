/**
 * Name: vite.config.ts
 * Description: Vite configuration file
 */
import react from '@vitejs/plugin-react-swc'
import { type UserConfig, defineConfig } from 'vite';
import { spawn } from "child_process";
import fs from "fs";
import path from "path";

import * as process from "process";

// Get base folder for certificates.
const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ''
        ? `${process.env.APPDATA}/ASP.NET/https`
        : `${process.env.HOME}/.aspnet/https`;

// Generate the certificate name using the NPM package name
const certificateName = process.env.npm_package_name;

// Define certificate filepath
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
// Define key filepath
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

// Export Vite configuration
export default defineConfig(async () => {
  // Ensure the certificate and key exist
  if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    // Wait for the certificate to be generated
    await new Promise<void>((resolve) => {
      spawn('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
      ], { stdio: 'inherit', })
          .on('exit', (code: any) => {
            resolve();
            if (code) {
              process.exit(code);
            }
          });
    });
  };

  // Define Vite configuration
  const config: UserConfig = {
    plugins: [react()],
    base: "https://localhost:5173",
    server: {
      origin: "https://localhost:5173",
      port: 5173,
      strictPort: true,
      https: {
        cert: certFilePath,
        key: keyFilePath
      },
      hmr: {
        host: "localhost",
        clientPort: 5173
      }
    }
  }

  return config;
});
