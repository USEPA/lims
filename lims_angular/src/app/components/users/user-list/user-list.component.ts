import { Component, OnInit, ViewChild } from "@angular/core";
import { FormControl } from "@angular/forms";

import { Observable } from "rxjs";
import { map, startWith } from "rxjs/operators";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";
import { MatPaginator } from "@angular/material/paginator";

import { AuthService } from "src/app/services/auth.service";

import { User } from "src/app/models/user.model";

@Component({
    selector: "app-user-list",
    templateUrl: "./user-list.component.html",
    styleUrls: ["./user-list.component.css"],
})
export class UserListComponent implements OnInit {
    loadingUsers: boolean;
    statusMessage = "";

    filter = "";

    filterInput = new FormControl();
    options: string[] = ["SCHEDULED", "CANCELLED"];
    filteredOptions: Observable<string[]>;

    columnNames = ["username", "date-disabled"];
    users: User[];
    sortableData = new MatTableDataSource();

    editingUser = false;

    constructor(private auth: AuthService) {}

    @ViewChild(MatSort, { static: true }) sort: MatSort;
    @ViewChild(MatPaginator) paginator: MatPaginator;
    ngOnInit() {
        this.loadingUsers = true;

        this.sortableData.data = [];
        this.filteredOptions = this.filterInput.valueChanges.pipe(
            startWith(""),
            map((value) => this.filterOptions(value))
        );

        this.auth.getUsers().subscribe(
            (users) => {
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
            (err) => {
                this.statusMessage = "Error retrieving data";
            },
            () => {
                this.loadingUsers = false;
            }
        );
    }

    ngAfterViewInit() {
        this.sortableData.paginator = this.paginator;
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

    doFilter(value: string): void {
        console.log("sortableData: ", this.sortableData);
        this.filter = value;
        this.sortableData.filter = value.trim().toLocaleLowerCase();
    }

    filterOptions(value: string): string[] {
        const filterValue = value.toLowerCase();

        return this.options.filter((option) => option.toLowerCase().includes(filterValue));
    }
}
