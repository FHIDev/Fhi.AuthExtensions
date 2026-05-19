import { Injectable } from '@angular/core';
import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

/**
 * Per-request opt-out for the global 401 → /login redirect.
 * Default is `false` (existing behaviour: 401 redirects to login).
 * Set to `true` on requests where the component will display the error itself.
 *
 * Usage:
 *   this.http.get(url, {
 *     context: new HttpContext().set(SKIP_AUTH_REDIRECT, true),
 *   });
 */
export const SKIP_AUTH_REDIRECT = new HttpContextToken<boolean>(() => false);

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && !req.context.get(SKIP_AUTH_REDIRECT)) {
          const returnUrl = window.location.pathname + window.location.search;
          window.location.href = '/login?returnUrl=' + encodeURIComponent(returnUrl);
        }
        return throwError(() => error);
      }),
    );
  }
}
