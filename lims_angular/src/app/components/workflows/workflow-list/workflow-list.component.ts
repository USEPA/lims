import { AfterViewChecked, Component, ElementRef, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";
import { FormControl } from "@angular/forms";

import { Observable } from "rxjs";
import { map, startWith } from "rxjs/operators";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";
import { MatPaginator } from "@angular/material/paginator";
import { MatDialog } from "@angular/material/dialog";

import { DeleteConfirmationDialogComponent } from "src/app/components/dialogs/delete-confirmation-dialog/delete-confirmation-dialog.component";

import { TaskManagerService } from "../../../services/task-manager.service";

import { Workflow } from "../../../models/workflow.model";

@Component({
    selector: "app-workflow-list",
    templateUrl: "./workflow-list.component.html",
    styleUrls: ["./workflow-list.component.css"],
})
export class WorkflowListComponent implements OnInit {
    loadingWorkflows: boolean;
    statusMessage: string;

    filter = "";

    filterInput = new FormControl();
    options: string[] = ["SCHEDULED", "CANCELLED"];
    filteredOptions: Observable<string[]>;

    columnNames = ["name", "processor", "creationDate", "active", "remove"];
    sortableData = new MatTableDataSource();
    workflows: Workflow[];

    editingWorkflow = false;

    constructor(private taskMgr: TaskManagerService, private router: Router, public dialog: MatDialog) {}

    @ViewChild(MatSort, { static: true }) sort: MatSort;
    @ViewChild(MatPaginator) paginator: MatPaginator;
    ngOnInit() {
        this.loadingWorkflows = true;
        this.statusMessage = "";

        this.sortableData.data = [];
        this.filteredOptions = this.filterInput.valueChanges.pipe(
            startWith(""),
            map((value) => this.filterOptions(value))
        );

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
        this.router.navigateByUrl("/workflows/create");
    }

    removeWorkflow(workflow): void {
        const dialogRef = this.dialog.open(DeleteConfirmationDialogComponent, {
            data: { type: `Workflow ${workflow.name}` },
        });

        dialogRef.afterClosed().subscribe((confirmDelete) => {
            if (confirmDelete) {
                this.taskMgr.removeWorkflow(workflow.id).subscribe((response) => {
                    this.getWorkflows();
                });
            }
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

    doFilter(value: string): void {
        console.log("sortableData: ", this.sortableData);
        this.filter = value;
        this.sortableData.filter = value.trim().toLocaleLowerCase();
    }

    filterOptions(value: string): string[] {
        const filterValue = value.toLowerCase();

        return this.options.filter((option) => option.toLowerCase().includes(filterValue));
    }
}
