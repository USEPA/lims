import { Component, OnInit, Output, EventEmitter } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { Location } from "@angular/common";

import { TaskManagerService } from "src/app/services/task-manager.service";

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
        private location: Location,
        private fb: FormBuilder
    ) {}

    ngOnInit() {
        const id = this.route.snapshot.paramMap.get("id");

        this.workflowForm = this.fb.group({
            name: ["", Validators.required],
            processor: ["", Validators.required],
            interval: ["", Validators.required],
            inputFolder: ["", Validators.required],
            outputFolder: ["", Validators.required],
            archiveFolder: ["", Validators.required],
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
        // a workflow returned from the backend includes an id field that the form doesn't have
        const newWorkflow = {
            name: workflow.name,
            processor: workflow.processor,
            interval: workflow.interval,
            inputFolder: workflow.inputFolder,
            outputFolder: workflow.outputFolder,
            archiveFolder: workflow.archiveFolder,
        };
        this.workflowForm.setValue(newWorkflow);
    }

    saveWorkflow(): void {
        console.log("this.workflow: ", this.workflow);
        this.workflowForm.updateValueAndValidity();
        this.statusMessage = "";
        const newWorkflow = this.workflowForm.value;
        if (this.workflowForm.valid) {
            const paths = {
                paths: {
                    input: newWorkflow.inputFolder,
                    output: newWorkflow.outputFolder,
                    archive: newWorkflow.archiveFolder,
                },
            };
            this.taskMgr.validatePaths(paths).subscribe((pathValidity) => {
                // TODO: error checking/messaging
                let pathsValid = true;
                for (let path of Object.keys(pathValidity)) {
                    if (!pathValidity[path]) {
                        pathsValid = false;
                        this.statusMessage += `The ${path} path is invalid. `;
                    }
                }
                if (pathsValid) {
                    if (this.redirect) {
                        newWorkflow["id"] = this.workflow.id;
                        newWorkflow["active"] = true;
                        this.taskMgr.updateWorkflow(newWorkflow).subscribe(() => {
                            // TODO: error checking/messaging
                            this.router.navigateByUrl("/tasks");
                        });
                    } else {
                        this.taskMgr.createWorkflow(newWorkflow).subscribe(() => {
                            // TODO: error checking/messaging
                            this.router.navigateByUrl("/tasks");
                        });
                    }
                }
            });
        } else {
            if (newWorkflow.name.length < 1) {
                this.statusMessage += "Workflows must include a workflow name. ";
            }
            if (newWorkflow.processor === "") {
                this.statusMessage += "Workflows must include a processor. ";
            }
            if (+newWorkflow.interval < 1) {
                this.statusMessage += "Intervals must be at least one minute in duration. ";
            }
            if (newWorkflow.inputFolder.length < 1) {
                this.statusMessage += "You must provide a path to the input folder. ";
            }
            if (newWorkflow.outputFolder.length < 1) {
                this.statusMessage += "You must provide a path to the output folder. ";
            }
            if (newWorkflow.archiveFolder.length < 1) {
                this.statusMessage += "You must provide a path to the backup folder.";
            }
        }
    }

    cancel(): void {
        if (this.redirect) {
            this.router.navigateByUrl("/workflows");
        } else {
            this.location.back();
        }
    }
}
