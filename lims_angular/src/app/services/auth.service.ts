import { environment } from "../../environments/environment";

import { Injectable } from "@angular/core";

import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable, throwError, of } from "rxjs";
import { timeout, catchError, tap } from "rxjs/operators";

import { User } from "../models/user.model";

@Injectable({
  providedIn: "root"
})
export class AuthService {
  httpOptions = {
    headers: new HttpHeaders({
      "Content-Type": "application/json"
    })
  };

  private users: User[] = [];

  private authenticated = false;
  private authToken = { access: null, refresh: null };

  constructor(private http: HttpClient) {}

  // /Users - returns json: all registered users
  getUsers(): Observable<any> {
    const options = {
      headers: new HttpHeaders({
        Authorization: "Bearer " + this.getAuthToken(),
        "Content-Type": "application/json"
      })
    };
    return this.http.get<User[]>(environment.authUrl, options).pipe(
      timeout(5000),
      tap(users => {
        if (users) {
          this.users = [...users];
        }
      }),
      catchError(err => {
        return of({ error: "failed to retrieve users!" });
      })
    );
  }

  getUserByName(username: string): User {
    for (const user of this.users) {
      if (user.username === username) {
        return user;
      }
    }
  }

  // POST/auth/users/ - endpoint that allows registration of new users
  // required params - username, password
  // No authentication required
  registerNewUser(
    username: string,
    password: string,
    firstname?: string,
    lastname?: string,
    email?: string
  ): Observable<User> {
    const newUser = {
      username,
      password,
      firstname: firstname || "unknown",
      lastname: lastname || "unknown",
      email: email || "unknown"
    };
    return this.http
      .post<any>(environment.authUrl + "register/", newUser, this.httpOptions)
      .pipe(
        // timeout(10000),
        tap((response: any) => {
          console.log("response from users/register: " + response);
        }),
        catchError(err => {
          return of({ error: "failed to register user!" });
        })
      );
  }

  // POST/users/authenticate - logs in user and returns access and refresh jwt tokens
  // params - username, password
  // No authentication required
  login(username: string, password: string): Observable<any> {
    const login = {
      username,
      password
    };
    return this.http
      .post<any>(environment.authUrl + "authenticate/", login, this.httpOptions)
      .pipe(
        // timeout(10000),
        tap((response: any) => {
          this.authToken.access = response.token;
          this.authToken.refresh = response.refresh;
          this.authenticated = true;
        }),
        catchError(err => {
          console.log(err);
          return of({ error: "falied to login user!" });
        })
      );
  }

  logout(): void {
    this.authenticated = false;
    this.authToken = { access: null, refresh: null };
  }

  // api call
  // NOT CURRENTLY IMPLEMENTED
  disableUser(username: string): void {
    // disable an existing user and update userlist
    const user = this.getUserByName(username);
    user.enabled = false;
  }

  isAuthenticated(): boolean {
    return this.authenticated;
  }

  getAuthToken(): string {
    return this.authToken.access;
  }

  getAuthRefreshToken(): string {
    return this.authToken.refresh;
  }
}
