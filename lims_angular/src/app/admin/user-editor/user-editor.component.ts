import { Component, OnInit, Output, EventEmitter } from '@angular/core';

import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-user-editor',
  templateUrl: './user-editor.component.html',
  styleUrls: ['./user-editor.component.css']
})
export class UserEditorComponent implements OnInit {
  @Output() editing = new EventEmitter<boolean>();

  errorMessage: string;

  constructor(private auth: AuthService) {}

  ngOnInit() {}

  saveUser(): void {
    // update user
  }

  cancel(): void {
    this.editing.emit(false);
  }
}
