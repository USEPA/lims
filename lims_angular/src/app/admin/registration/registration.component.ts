import { Component, OnInit, Output, EventEmitter } from "@angular/core";

import { AuthService } from "src/app/services/auth.service";

@Component({
  selector: "app-registration",
  templateUrl: "./registration.component.html",
  styleUrls: ["./registration.component.css"]
})
export class RegistrationComponent implements OnInit {
  @Output() registeringUser = new EventEmitter<boolean>();
  waitingForResponse: boolean;
  errorMessage: string;

  constructor(private auth: AuthService) {}

  ngOnInit() {
    this.waitingForResponse = false;
    this.errorMessage = "";
  }

  registerUser(
    firstname: HTMLInputElement,
    lastname: HTMLInputElement,
    username: HTMLInputElement,
    email: HTMLInputElement,
    password: HTMLInputElement
  ) {
    this.waitingForResponse = true;
    this.errorMessage = "";
    this.auth
      .registerNewUser(
        firstname.value,
        lastname.value,
        username.value,
        email.value,
        password.value
      )
      .subscribe(response => {
        this.handleRegisterResponse(response);
      });
  }

  handleRegisterResponse(response): void {
    // this endpoint returns null on success
    this.waitingForResponse = false;
    if (response) {
      if (response.error) {
        this.errorMessage = response.error;
      } else {
        console.log(response);
        this.cancel();
      }
    } else {
      this.cancel();
    }
  }

  cancel(): void {
    this.registeringUser.emit(false);
  }
}
