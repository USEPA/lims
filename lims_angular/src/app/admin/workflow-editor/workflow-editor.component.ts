import { Component, OnInit, Output, EventEmitter } from "@angular/core";
import { TaskManagerService } from "src/app/services/task-manager.service";
import { ActivatedRoute, Router } from "@angular/router";

import { FormBuilder, FormGroup, Validators } from "@angular/forms";

import { Workflow } from "src/app/models/workflow.model";

@Component({
  selector: "app-workflow-editor",
  templateUrl: "./workflow-editor.component.html",
  styleUrls: ["./workflow-editor.component.css"],
})
export class WorkflowEditorComponent implements OnInit {
  @Output() editing = new EventEmitter<boolean>();
  workflowForm: FormGroup;
  cardTitle = "Add workflow";
  buttonText = "Save workflow";
  redirect = false;

  workflow: Workflow;
  processors = [];
  statusMessage = "";

  constructor(
    private taskMgr: TaskManagerService,
    private router: Router,
    private route: ActivatedRoute,
    private fb: FormBuilder
  ) { }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get("id");

    this.workflowForm = this.fb.group({
      name: [null, Validators.required],
      processor: [null, Validators.required],
      inputFolder: [null, Validators.required],
      outputFolder: [null, Validators.required],
      archiveFolder: [null],
      interval: [null, Validators.required],
    });

    if (id) {
      this.workflow = this.taskMgr.getWorkflow(id);
      this.cardTitle = "Edit workflow";
      this.buttonText = "Save changes";
      this.redirect = true;
      this.populateForm(this.workflow);
    }

    this.taskMgr.getProcessors().subscribe((response) => {
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

  populateForm(workflow) {
    this.workflowForm.get("name").setValue(workflow.name);
    this.workflowForm.get("processor").setValue(workflow.processor);
    this.workflowForm.get("inputFolder").setValue(workflow.inputFolder);
    this.workflowForm.get("outputFolder").setValue(workflow.outputFolder);
    this.workflowForm.get("archiveFolder").setValue(workflow.outputFolder);
    this.workflowForm.get("interval").setValue(workflow.interval);
  }

  saveWorkflow(): void {
    this.workflowForm.updateValueAndValidity();
    const name = this.workflowForm.get("name").value;
    const processor = this.workflowForm.get("processor").value;
    const inputFolder = this.workflowForm.get("inputFolder").value;
    const outputFolder = this.workflowForm.get("outputFolder").value;
    const archiveFolder = this.workflowForm.get("archiveFolder").value;
    const interval = this.workflowForm.get("interval").value;
    if (name.length < 1) {
      this.statusMessage = "Workflows must include a workflow name";
      return;
    }
    if (processor === "") {
      this.statusMessage = "Workflows must include a valid processor";
      return;
    }
    if (inputFolder.length < 1) {
      this.statusMessage = "You must provide a path to the input folder";
      return;
    }
    if (outputFolder.length < 1) {
      this.statusMessage = "You must provide a path to the output folder";
      return;
    }
    if (+interval < 1) {
      this.statusMessage = "Intervals must be at least one minute induration";
      return;
    }
    const newWorkflow = {
      name,
      processor,
      inputFolder,
      outputFolder,
      archiveFolder,
      interval,
    };
    if (this.redirect) {
      this.taskMgr.updateWorkflow(newWorkflow).subscribe(() => {
        // TODO: error checking/messaging
        this.cancel();
      });
    } else {
      this.taskMgr.addWorkflow(newWorkflow).subscribe(() => {
        // TODO: error checking/messaging
        this.cancel();
      });
    }
  }

  cancel(): void {
    if (this.redirect) {
      this.router.navigateByUrl("/workflows");
    } else {
      this.editing.emit(false);
    }
  }
}
