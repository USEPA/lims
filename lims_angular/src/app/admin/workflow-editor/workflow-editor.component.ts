import { Component, OnInit, Output, EventEmitter } from "@angular/core";
import { TaskManagerService } from "src/app/services/task-manager.service";
import { Router, ActivatedRoute } from "@angular/router";

import { Workflow } from "src/app/models/workflow.model";

@Component({
  selector: "app-workflow-editor",
  templateUrl: "./workflow-editor.component.html",
  styleUrls: ["./workflow-editor.component.css"]
})
export class WorkflowEditorComponent implements OnInit {
  @Output() editing = new EventEmitter<boolean>();

  workflow: Workflow;
  processors = [];
  statusMessage = "";

  constructor(
    private taskMgr: TaskManagerService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get("id");
    this.workflow = this.taskMgr.getWorkflow(id);
    this.taskMgr.getProcessors().subscribe(response => {
      if (response.error) {
        this.statusMessage = "No processors installed";
      } else {
        if (response && response.length > 0) {
          this.processors = [...response];
        } else {
          this.statusMessage = "No processors installed";
        }
      }
    });
  }

  saveWorkflow(
    name: HTMLInputElement,
    processor: HTMLSelectElement,
    inputFolder: HTMLInputElement,
    outputFolder: HTMLInputElement,
    interval: HTMLInputElement
  ): void {
    if (name.value.length < 1) {
      this.statusMessage = "Workflows must include a workflow name";
      return;
    }
    if (processor.value === "null" || processor.value === undefined) {
      this.statusMessage = "Workflows must include a valid processor";
      return;
    }
    if (inputFolder.value.length < 1) {
      this.statusMessage = "You must provide a path to the input folder";
      return;
    }
    if (outputFolder.value.length < 1) {
      this.statusMessage = "You must provide a path to the output folder";
      return;
    }
    if (+interval.value < 1) {
      this.statusMessage = "Intervals must be at least one minute induration";
      return;
    }
    const newWorkflow = {
      name: name.value,
      processor: processor.value,
      inputFolder: inputFolder.value,
      outputFolder: outputFolder.value,
      interval: interval.value
    };
    this.taskMgr.addWorkflow(newWorkflow).subscribe(() => {
      this.editing.emit(false);
    });
  }

  cancel(): void {
    this.editing.emit(false);
  }
}
