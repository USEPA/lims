import { Component, OnInit, ViewChild } from "@angular/core";
import { FormControl } from "@angular/forms";

import { Observable } from "rxjs";
import { map, startWith } from "rxjs/operators";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";
import { MatPaginator } from "@angular/material/paginator";

import { AuthService } from "src/app/services/auth.service";
import { LogsService } from "src/app/services/logs.service";

@Component({
    selector: "app-log-list",
    templateUrl: "./log-list.component.html",
    styleUrls: ["./log-list.component.css"],
})
export class LogListComponent implements OnInit {
    loadingLogs: boolean;
    statusMessage: string;

    columnNames = ["type", "processor", "message", "time"];
    sortableData = new MatTableDataSource();
    logList = [];

    filter = "";

    filterInput = new FormControl();
    options: string[] = ["INFORMATION", "ERROR"];
    filteredOptions: Observable<string[]>;

    constructor(private logService: LogsService, private auth: AuthService) {}

    @ViewChild(MatSort, { static: true }) sort: MatSort;
    @ViewChild(MatPaginator) paginator: MatPaginator;
    ngOnInit() {
        this.loadingLogs = true;
        this.statusMessage = "";

        this.filteredOptions = this.filterInput.valueChanges.pipe(
            startWith(""),
            map((value) => this.filterOptions(value))
        );

        this.updateLogList();
    }

    ngAfterViewInit() {
        this.sortableData.paginator = this.paginator;
    }

    updateLogList(): void {
        if (this.auth.isAuthenticated()) {
            this.logService.getLogs().subscribe(
                (logs) => {
                    if (logs.error) {
                        this.statusMessage = logs.error;
                    } else {
                        if (logs && logs.length) {
                            this.logList = [...logs];
                            this.sortableData.data = [...this.logList];
                            this.sortableData.sort = this.sort;
                            this.statusMessage = "";
                        } else {
                            this.statusMessage = "There are currently no Logs available";
                        }
                    }
                },
                (err) => {
                    console.log(err);
                    this.statusMessage = "Error retrieving logs";
                },
                () => {
                    this.loadingLogs = false;
                }
            );
        }
    }

    doFilter(value: string): void {
        this.filter = value;
        this.sortableData.filter = value.trim().toLocaleLowerCase();
    }

    private filterOptions(value: string): string[] {
        const filterValue = value.toLowerCase();

        return this.options.filter((option) => option.toLowerCase().includes(filterValue));
    }
}
