import { Component, OnInit, Output, EventEmitter } from "@angular/core";

import { AuthService } from "src/app/services/auth.service";
import { Router } from "@angular/router";

@Component({
  selector: "app-registration",
  templateUrl: "./registration.component.html",
  styleUrls: ["./registration.component.css"]
})
export class RegistrationComponent implements OnInit {
  @Output() registeringUser = new EventEmitter<boolean>();
  waitingForResponse: boolean;
  errorMessage: string;

  constructor(private auth: AuthService, private router: Router) {}

  ngOnInit() {
    this.waitingForResponse = false;
    this.errorMessage = "";
  }

  registerUser(
    username: HTMLInputElement,
    password: HTMLInputElement,
    firstname?: HTMLInputElement,
    lastname?: HTMLInputElement,
    email?: HTMLInputElement
  ) {
    if (username.value.length < 1 || password.value.length < 1) {
      alert("Username and Password are required");
      return;
    }
    this.waitingForResponse = true;
    this.errorMessage = "";
    this.auth
      .registerNewUser(
        username.value,
        password.value,
        "unknown",
        "unknown",
        "unknown"
      )
      .subscribe(response => {
        this.handleRegisterResponse(response, username.value, password.value);
      });
  }

  handleRegisterResponse(response, username, password): void {
    // this endpoint returns a null response on success
    this.waitingForResponse = false;
    if (response) {
      if (response.error) {
        this.errorMessage = "Failed to register user!";
      } else {
        // should never get here
        console.log("response not null and no error: " + response);
        this.cancel();
      }
    } else {
      // registration successful, log in user
      this.auth.login(username, password).subscribe(res => {
        if (res.error) {
          this.errorMessage = "Registered new user, but auto login failed";
        } else {
          this.cancel();
          this.router.navigateByUrl("/tasks");
        }
      });
    }
  }

  cancel(): void {
    this.registeringUser.emit(false);
  }
}
