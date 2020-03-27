import { Component, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";

import { TaskManagerService } from "src/app/services/task-manager.service";

import { Task } from "src/app/models/task.model";
import { Workflow } from "src/app/models/workflow.model";

@Component({
  selector: "app-tasklist",
  templateUrl: "./tasklist.component.html",
  styleUrls: ["./tasklist.component.css"]
})
export class TasklistComponent implements OnInit {
  loadingTasklist: boolean;
  loadingWorkflows: boolean;
  statusMessage: string;

  columnNames = ["task", "workflow", "status", "date"];
  taskList: Task[];
  sortableData = new MatTableDataSource();
  workflows: Workflow[];

  constructor(private taskMgr: TaskManagerService, private router: Router) {}

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  ngOnInit() {
    this.loadingTasklist = true;
    this.loadingWorkflows = true;
    this.statusMessage = "";

    this.taskMgr.getTasks().subscribe(
      tasks => {
        if (tasks.error) {
          this.statusMessage = tasks.error;
        } else {
          if (tasks && tasks.length > 0) {
            this.taskList = [...tasks];
            this.sortableData.data = [...this.taskList];
            this.sortableData.sort = this.sort;
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
    const _date = new Date(date);
    const hours = _date.getHours();
    const mins = _date.getMinutes();
    const secs = _date.getSeconds();
    return `${hours}:${mins}:${secs} ${_date.toDateString()}`;
  }

  cancelTask(): void {
    console.log("task canceled!");
  }
}
