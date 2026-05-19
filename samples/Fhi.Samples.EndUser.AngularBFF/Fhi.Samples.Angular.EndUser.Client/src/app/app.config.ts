import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideZonelessChangeDetection,
} from '@angular/core';
import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { AuthenticationService } from './services/authentication.service';
import { AuthInterceptor } from './interceptors/auth.interseptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch(), withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    provideAppInitializer(async () => {
      const auth = inject(AuthenticationService);
      try {
        const session = await firstValueFrom(auth.getSession());
        if (!session.isAuthenticated && !session.isError) {
          window.location.href = '/login';
        }
      } catch (error) {
        console.error('Error getting session:', error);
      }
    }),
  ],
};
