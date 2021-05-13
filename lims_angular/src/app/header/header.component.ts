import { Component, OnInit } from "@angular/core";
import { AuthService } from "../services/auth.service";

import { Router } from "@angular/router";

@Component({
  selector: "app-header",
  templateUrl: "./header.component.html",
  styleUrls: ["./header.component.css"]
})
export class HeaderComponent implements OnInit {
  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit() {}

  logout(): void {
    // logs user out
    this.auth.logout();
  }

  gotoTasks(): void {
    this.router.navigateByUrl("/tasks");
  }

  gotoUsers(): void {
    this.router.navigateByUrl("/users");
  }

  gotoWorkflows(): void {
    this.router.navigateByUrl("/workflows");
  }

  gotoProcessors(): void {
    this.router.navigateByUrl("/processors");
  }

  isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }
}
