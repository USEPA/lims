import { environment } from "../../environments/environment";

import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";

import { Observable, of } from "rxjs";
import { timeout, catchError, tap } from "rxjs/operators";

import { CookieService } from "ngx-cookie-service";

import { User } from "../models/user.model";

@Injectable({
    providedIn: "root",
})
export class AuthService {
    private users: User[] = [];

    constructor(private http: HttpClient, private cookieService: CookieService) {}

    // /Users - returns json: all registered users
    getUsers(): Observable<any> {
        return this.http.get<User[]>(environment.authUrl).pipe(
            timeout(5000),
            tap((users) => {
                if (users) {
                    this.users = [...users];
                }
            }),
            catchError((err) => {
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
        firstname: string,
        lastname: string,
        email: string
    ): Observable<User> {
        const newUser = {
            username,
            password,
            firstname,
            lastname,
            email,
        };
        return this.http.post<any>(environment.authUrl + "register/", newUser).pipe(
            // timeout(10000),
            catchError((err) => {
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
            password,
        };
        return this.http.post<any>(environment.authUrl + "authenticate/", login).pipe(
            // timeout(10000),
            tap((token: any) => {
                this.setToken(token);
            }),
            catchError((err) => {
                console.log(err);
                return of({ error: "falied to login user!" });
            })
        );
    }

    setToken(token) {
        this.cookieService.set("JWT_TOKEN", token.token, { expires: 1 });
    }

    isAuthenticated(): boolean {
        return this.cookieService.get("JWT_TOKEN") ? true : false;
    }

    getAuthToken(): string {
        return this.cookieService.get("JWT_TOKEN");
    }

    logout(): void {
        this.cookieService.deleteAll();
    }

    // NOT CURRENTLY IMPLEMENTED
    disableUser(username: string): void {
        // disable an existing user and update userlist
        const user = this.getUserByName(username);
        user.enabled = false;
    }
}
