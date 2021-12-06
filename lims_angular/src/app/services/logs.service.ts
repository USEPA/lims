import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Injectable } from "@angular/core";

import { Observable, of } from "rxjs";
import { catchError } from "rxjs/operators";

import { environment } from "src/environments/environment";

import { AuthService } from "./auth.service";

@Injectable({
  providedIn: "root",
})
export class LogsService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  getLogs(): Observable<any> {
    const options = {
      headers: new HttpHeaders({
        Authorization: "Bearer " + this.auth.getAuthToken(),
        "Content-Type": "application/json",
      }),
    };
    return this.http.get(environment.apiUrl + "logs/", options).pipe(
      // timeout(5000),
      catchError((err) => {
        console.log("logService error: ", err);
        return of({ error: "failed to retrieve logs!" });
      })
    );
  }
}
