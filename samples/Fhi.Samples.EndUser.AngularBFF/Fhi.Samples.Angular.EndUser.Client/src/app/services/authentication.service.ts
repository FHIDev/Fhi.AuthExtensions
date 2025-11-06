import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { catchError, Observable } from 'rxjs';
import { of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthenticationService {
  private readonly http = inject(HttpClient);
  private session$: Observable<Session> | null = null;

  public getSession(ignoreCache: boolean = false): Observable<Session> {
    if (!this.session$ || ignoreCache) {
      this.session$ = this.http.get<Session>('/session', { withCredentials: true }).pipe(
        catchError((error) => {
          console.log(error);
          return of({ isAuthenticated: false, isError: true });
        })
      );
    }
    return this.session$;
  }
}


    

type Session = {
  isAuthenticated: boolean | false,
  isError: boolean | false
  
}

