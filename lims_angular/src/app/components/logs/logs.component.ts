import { Component, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";

import { AuthService } from "src/app/services/auth.service";
import { LogsService } from "src/app/services/logs.service";

@Component({
  selector: "app-logs",
  templateUrl: "./logs.component.html",
  styleUrls: ["./logs.component.css"],
})
export class LogsComponent implements OnInit {
  loadingLogs: boolean;
  statusMessage: string;

  columnNames = ["type", "processor", "message"];
  sortableData = new MatTableDataSource();
  logList = [];

  constructor(private logService: LogsService, private auth: AuthService) {}

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  ngOnInit() {
    this.loadingLogs = true;
    this.statusMessage = "";

    this.updateLogList();
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

  public doFilter = (value: string) => {
    this.sortableData.filter = value.trim().toLocaleLowerCase();
  };
}
