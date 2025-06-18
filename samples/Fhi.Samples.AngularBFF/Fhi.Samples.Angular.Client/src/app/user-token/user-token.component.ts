import { Component, OnInit } from '@angular/core';
import { catchError, map, of } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { NgIf } from '@angular/common';
import { jwtDecode } from "jwt-decode";

type UserSessionDto = {
  accessToken: string,
  idToken: string
}

type TokenInformation = {
  accessToken: string,
  accessTokenJwt: string,
  idToken: string,
  idTokenJwt:string
}


@Component({
    selector: 'app-user-session',
    templateUrl: './user-token.component.html',
    imports: [NgIf]
})


export class UserSessionComponent implements OnInit  {
  public tokenInformation: TokenInformation | null = {
    accessToken: '',
    idToken: '',
    accessTokenJwt: '',
    idTokenJwt: ''
  }; 

  constructor(private http: HttpClient) {
  }

  ngOnInit() {
    this.http.get<UserSessionDto>('/bff/v1/user-token').pipe(
      map(result => {
        const decodedAccessToken = jwtDecode(result.accessToken);
        const decodedIdToken = jwtDecode(result.idToken);
        return {
          accessToken: result.accessToken,
          accessTokenJwt: JSON.stringify(decodedAccessToken),
          idToken: result.idToken,
          idTokenJwt: JSON.stringify(decodedIdToken)
        };
      }),
      catchError(error => {
        console.error(error);
        return of(null);
      })
    ).subscribe(tokenInformation => {
      this.tokenInformation = tokenInformation;
    });
}
}
