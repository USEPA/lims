import { Component, OnInit } from "@angular/core";

import { AuthService } from "../../services/auth.service";

import { CookieService } from "ngx-cookie-service";

@Component({
    selector: "app-main",
    templateUrl: "./main.component.html",
    styleUrls: ["./main.component.css"],
})
export class MainComponent implements OnInit {
    constructor(private auth: AuthService, private cookieService: CookieService) {}

    ngOnInit() {}

    isAuthenticated(): boolean {
        return this.auth.isAuthenticated();
    }
}
