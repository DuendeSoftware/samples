import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering } from '@angular/platform-server';
import {provideServerRouting } from '@angular/ssr';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import {csrfHeaderInterceptor} from './Interceptors/csrf-header.interceptor';
import {provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(),
    provideServerRouting(serverRoutes),
    provideHttpClient(withFetch(), withInterceptors([csrfHeaderInterceptor])),
  ]
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
