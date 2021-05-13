import { Component, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

import { Location } from "@angular/common";

import { TaskManagerService } from "../../services/task-manager.service";
import { Task } from "../../models/task.model";

@Component({
  selector: "app-task-detail",
  templateUrl: "./task-detail.component.html",
  styleUrls: ["./task-detail.component.css"]
})
export class TaskDetailComponent implements OnInit {
  task: Task;
  constructor(
    private route: ActivatedRoute,
    private taskMgr: TaskManagerService,
    private location: Location
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get("id");
    this.task = this.taskMgr.getTask(id);
  }

  rerunTask(id: number): void {
    // re-run existing task
  }

  getWorkflowName(id: string) {
    return this.taskMgr.getWorkflow(id).name;
  }

  getInputFolderPath(id: string) {
    return this.taskMgr.getWorkflow(id).inputFolder;
  }

  getProcessorName(id: string) {
    return this.taskMgr.getWorkflow(id).processor;
  }

  cancelTask(id: number): void {
    // cancel task
  }

  back(): void {
    this.location.back();
  }

  getFormattedDate(date): string {
    return new Date(date).toLocaleString();
  }
}
