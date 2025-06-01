import { defineConfig, loadEnv } from 'vite';
import plugin from '@vitejs/plugin-react';
import mkcert from 'vite-plugin-mkcert';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [plugin(), mkcert()],
    define: {
      'import.meta.env.webapplicationauthenticationexperiment': JSON.stringify(process.env.services__webapplicationauthenticationexperiment__https__0)
    },
    server: {
      port: parseInt(env.PORT),
      proxy: {
        '/api': {
          // the following env variables are injected by aspire into this process by AppHost when this service is configured with WithReference()
          // https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/app-host-overview#service-endpoint-environment-variable-format
          target: process.env.services__webapplicationauthenticationexperiment__https__0 ||
            process.env.services__webapplicationauthenticationexperiment__http__0,
          changeOrigin: true,
          secure: false,
        },
        '/hub': {
          target: process.env.services__webapplicationauthenticationexperiment__https__0 || process.env.services__webapplicationauthenticationexperiment__wss__0,
          ws: true,
          secure: false
        },
      }
    },
  }
});

