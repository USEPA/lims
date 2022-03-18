import { Injectable } from "@angular/core";
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpResponse } from "@angular/common/http";

import { Observable, EMPTY } from "rxjs";

import { AuthService } from "../services/auth.service";
import { filter, tap } from "rxjs/operators";
import { Router } from "@angular/router";

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    constructor(public auth: AuthService, private router: Router) {}

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (!request.body?.password) {
            request = request.clone({
                setHeaders: { Authorization: `Bearer ${this.auth.getAuthToken()}`, "Content-Type": "application/json" },
            });
        }
        return next.handle(request).pipe(
            filter((event) => event instanceof HttpResponse),
            tap((event: HttpResponse<any>) => {
                // clear token and redirect to / if status code === 401
                if (event.status === 401) {
                    console.log("STATUS.401>>> ", event);
                    this.auth.logout();
                }
            })
        );
    }
}
