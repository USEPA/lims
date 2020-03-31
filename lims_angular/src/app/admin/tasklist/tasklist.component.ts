import { Component, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";

import { TaskManagerService } from "src/app/services/task-manager.service";

import { Task } from "src/app/models/task.model";
import { Workflow } from "src/app/models/workflow.model";
import { AuthService } from "src/app/services/auth.service";

@Component({
  selector: "app-tasklist",
  templateUrl: "./tasklist.component.html",
  styleUrls: ["./tasklist.component.css"]
})
export class TasklistComponent implements OnInit {
  // tasklist refresh interval in ms
  reloadInterval = 10000;
  loadingTasklist: boolean;
  loadingWorkflows: boolean;
  statusMessage: string;

  columnNames = ["taskID", "workflowID", "status", "start"];
  taskList: Task[];
  sortableData = new MatTableDataSource();
  workflows: Workflow[];

  constructor(
    private taskMgr: TaskManagerService,
    private auth: AuthService,
    private router: Router
  ) {}

  @ViewChild(MatSort, { static: false }) sort: MatSort;
  ngOnInit() {
    this.loadingTasklist = true;
    this.loadingWorkflows = true;
    this.statusMessage = "";

    this.updateTasklist();

    setInterval(() => {
      this.updateTasklist();
    }, this.reloadInterval);
  }

  updateTasklist(): void {
    if (this.auth.isAuthenticated()) {
      this.taskMgr.getTasks().subscribe(
        tasks => {
          if (tasks.error) {
            this.statusMessage = tasks.error;
          } else {
            if (tasks && tasks.length > 0) {
              this.taskList = [...tasks];
              const colsOnly = [];
              for (let task of this.taskList) {
                colsOnly.push({
                  taskID: task.taskID,
                  workflowID: task.workflowID,
                  status: task.status,
                  start: task.start
                });
              }
              this.sortableData.data = [...colsOnly];
              this.sortableData.sort = this.sort;
              this.sort.sort({
                id: "taskID",
                start: "desc",
                disableClear: false
              });
              this.statusMessage = "";
            } else {
              this.statusMessage = "There are currently no Tasks scheduled";
            }
          }
        },
        err => {
          this.statusMessage = "Error retrieving data";
        },
        () => {
          this.loadingTasklist = false;
        }
      );
      this.taskMgr.getWorkflows().subscribe(
        workflows => {
          if (workflows.error) {
            console.log(workflows.error);
          } else {
            this.workflows = [...workflows];
          }
        },
        err => {
          console.log(err);
        },
        () => {
          this.loadingWorkflows = false;
        }
      );
    }
  }

  gotoTaskDetail(id: number) {
    this.router.navigateByUrl("/tasks/detail/" + id);
  }

  gotoWorkflowDetail(id: string) {
    this.router.navigateByUrl("/workflows/detail/" + id);
  }

  getWorkflowName(id: string) {
    return this.taskMgr.getWorkflow(id).name;
  }

  getFormattedDate(date) {
    return new Date(date).toLocaleString();
  }

  cancelTask(): void {
    console.log("task canceled!");
  }
}
