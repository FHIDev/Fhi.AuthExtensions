const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:43371';


const PROXY_CONFIG = [
  {
    context: [
      "/bff",
      "/login",
      "/logout",
      "/session",
      "/signin-oidc"
    ],
    target,
    secure: false,
    changeOrigin: true,
    logLevel: "debug"
  },
  {
    context: [
      "/connect",
    ],
    target: "https://localhost:5001",
    secure: false,
    changeOrigin: true,
    logLevel: "debug"
  }
  
]

module.exports = PROXY_CONFIG;
