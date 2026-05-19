import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';

export type Session = {
  isAuthenticated: boolean;
  isError?: boolean;
  name?: string | null;
};

@Injectable({
  providedIn: 'root',
})
export class AuthenticationService {
  private readonly http = inject(HttpClient);
  private session$: Observable<Session> | null = null;

  public getSession(ignoreCache: boolean = false): Observable<Session> {
    if (!this.session$ || ignoreCache) {
      this.session$ = this.http
        .get<Session>('/session', { withCredentials: true })
        .pipe(
          catchError((error) => {
            console.error(error);
            return of({ isAuthenticated: false, isError: true });
          }),
        );
    }
    return this.session$;
  }
}
