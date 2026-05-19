import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of } from 'rxjs';
import { jwtDecode } from 'jwt-decode';

type UserSessionDto = {
  accessToken: string;
  idToken: string;
};

type DecodedToken = {
  label: string;
  raw: string;
  header: string;
  payload: string;
};

@Component({
  selector: 'app-user-session',
  templateUrl: './user-token.component.html',
  styleUrl: './user-token.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserSessionComponent {
  private readonly http = inject(HttpClient);

  protected readonly tokens = toSignal<DecodedToken[] | null>(
    this.http.get<UserSessionDto>('/bff/v1/user-token').pipe(
      map((result) => [
        this.decode('Access token', result.accessToken),
        this.decode('ID token', result.idToken),
      ]),
      catchError((error) => {
        console.error(error);
        return of(null);
      }),
    ),
    { initialValue: null },
  );

  private decode(label: string, token: string): DecodedToken {
    return {
      label,
      raw: token,
      header: JSON.stringify(jwtDecode(token, { header: true }), null, 2),
      payload: JSON.stringify(jwtDecode(token), null, 2),
    };
  }
}
