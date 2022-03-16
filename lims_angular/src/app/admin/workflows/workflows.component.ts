import { Component, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";

import { TaskManagerService } from "../../services/task-manager.service";
import { Workflow } from "../../models/workflow.model";
import { MatPaginator } from "@angular/material/paginator";

@Component({
    selector: "app-workflows",
    templateUrl: "./workflows.component.html",
    styleUrls: ["./workflows.component.css"],
})
export class WorkflowsComponent implements OnInit {
    loadingWorkflows: boolean;
    statusMessage: string;

    columnNames = ["name", "processor", "creationDate", "active", "remove"];
    workflows: Workflow[];
    sortableData = new MatTableDataSource();

    editingWorkflow = false;

    constructor(private taskMgr: TaskManagerService, private router: Router) {}

    @ViewChild(MatSort, { static: true }) sort: MatSort;
    @ViewChild(MatPaginator) paginator: MatPaginator;
    ngOnInit() {
        this.loadingWorkflows = true;
        this.statusMessage = "";

        this.getWorkflows();
    }

    ngAfterViewInit() {
        this.sortableData.paginator = this.paginator;
    }

    getWorkflows() {
        this.taskMgr.getWorkflows().subscribe(
            (workflows) => {
                if (workflows.error) {
                    this.statusMessage = workflows.error;
                } else {
                    if (workflows && workflows.length > 0) {
                        this.workflows = [...workflows];
                        this.sortableData.data = [...this.workflows];
                        this.sortableData.sort = this.sort;
                        this.statusMessage = "";
                        console.log("workflows: ", this.workflows);
                    } else {
                        this.statusMessage = "There are currently no Workflows available";
                    }
                }
            },
            (err) => {
                this.statusMessage = "Error retrieving data";
            },
            () => {
                this.loadingWorkflows = false;
            }
        );
    }

    gotoWorkflowDetail(id: string) {
        this.router.navigateByUrl("/workflows/detail/" + id);
    }

    addWorkflow(): void {
        this.editingWorkflow = true;
    }

    removeWorkflow(workflowId): void {
        console.log("remove ", workflowId);
        this.taskMgr.removeWorkflow(workflowId).subscribe((response) => {
            console.log("removeWorkflow: ", response);
            this.getWorkflows();
        });
    }

    toggleEnable(workflow): void {
        if (workflow.active) {
            this.taskMgr.disableWorkflow(workflow).subscribe((response) => {
                this.getWorkflows();
            });
        } else {
            this.taskMgr.enableWorkflow(workflow).subscribe((response) => {
                this.getWorkflows();
            });
        }
    }

    isEditing($event): void {
        this.editingWorkflow = $event;
        if (!this.editingWorkflow) {
            this.getWorkflows();
        }
    }
}
