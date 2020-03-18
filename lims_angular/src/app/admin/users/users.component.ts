import { Component, OnInit, ViewChild } from "@angular/core";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";

import { AuthService } from "src/app/services/auth.service";
import { User } from "src/app/models/user.model";

@Component({
  selector: "app-users",
  templateUrl: "./users.component.html",
  styleUrls: ["./users.component.css"]
})
export class UsersComponent implements OnInit {
  loadingUsers: boolean;
  editingUser = false;
  statusMessage = "";

  columnNames = ["username", "date-added", "date-disabled"];
  users: User[];
  sortableData = new MatTableDataSource();

  constructor(private auth: AuthService) {}

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  ngOnInit() {
    this.loadingUsers = true;
    this.auth.getUsers().subscribe(
      users => {
        if (users.error) {
          this.statusMessage = users.error;
        } else {
          if (users && users.length > 0) {
            this.users = [...users];
            this.sortableData.data = [...this.users];
            this.sortableData.sort = this.sort;
            this.statusMessage = "";
          } else {
            this.statusMessage = "There are currently no users registered";
          }
        }
      },
      err => {
        this.statusMessage = "Error retrieving data";
      },
      () => {
        this.loadingUsers = false;
      }
    );
  }

  addUser(): void {
    this.editingUser = true;
  }

  isEditing($event): void {
    this.editingUser = $event;
  }

  disableUser(username: string): void {
    this.auth.disableUser(username);
  }
}
