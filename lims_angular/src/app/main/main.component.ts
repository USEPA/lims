import { Component, OnInit } from '@angular/core';

import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-main',
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.css']
})
export class MainComponent implements OnInit {
  constructor(private auth: AuthService) {}

  ngOnInit() {}

  isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }
}
