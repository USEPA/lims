import { Component, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";

import { AuthService } from "src/app/services/auth.service";
import { TaskManagerService } from "src/app/services/task-manager.service";
import { LogsService } from "src/app/services/logs.service";

@Component({
  selector: "app-logs",
  templateUrl: "./logs.component.html",
  styleUrls: ["./logs.component.css"],
})
export class LogsComponent implements OnInit {
  // tasklist refresh interval in ms
  logs = [];
  loadingLogs: boolean;
  statusMessage: string;

  errors = [];

  columnNames = ["taskID", "workflowID", "processor", "status"];
  sortableData = new MatTableDataSource();

  constructor(
    private logService: LogsService,
    private auth: AuthService,
    private router: Router
  ) {}

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  ngOnInit() {
    this.loadingLogs = true;
    this.statusMessage = "";

    this.updateLogTable();
  }

  updateLogTable(): void {
    if (this.auth.isAuthenticated()) {
      this.logService.getLogs().subscribe(
        (logs) => {
          if (logs.error) {
            console.log(logs.error);
          } else {
            console.log("logs: ", logs);
            this.logs = [...logs];
            this.sortableData.data = [...this.logs];
            this.sortableData.sort = this.sort;
            this.statusMessage = "";
          }
        },
        (err) => {
          console.log(err);
          this.sortableData.data = [];
          this.sortableData.sort = this.sort;
        },
        () => {
          this.loadingLogs = false;
        }
      );
    }
  }
}
