import { Component, OnInit } from "@angular/core";
import { Location } from "@angular/common";
import { ActivatedRoute, Router } from "@angular/router";

import { TaskManagerService } from "../../services/task-manager.service";
import { Workflow } from "../../models/workflow.model";

@Component({
    selector: "app-workflow-detail",
    templateUrl: "./workflow-detail.component.html",
    styleUrls: ["./workflow-detail.component.css"],
})
export class WorkflowDetailComponent implements OnInit {
    disableText = "Disable workflow";
    enableText = "Enable workflow";
    workflow: Workflow;
    constructor(
        private taskMgr: TaskManagerService,
        private router: Router,
        private route: ActivatedRoute,
        private location: Location
    ) {}

    ngOnInit() {
        const id = this.route.snapshot.paramMap.get("id");
        if (id) {
            this.workflow = this.taskMgr.getWorkflow(id);
        }
    }

    // api call
    editWorkflow(workflow): void {
        this.router.navigateByUrl("workflows/edit/" + workflow.id);
    }

    // api call
    executeWorkflow(workflow): void {
        this.taskMgr.executeWorkflow(workflow).subscribe((response) => {
            console.log("executeNow: ", response);
            this.router.navigateByUrl("tasks/");
        });
    }

    // api call
    toggleEnableWorkflow(workflow): void {
        if (workflow.active) {
            this.taskMgr.disableWorkflow(workflow).subscribe(() => {
                this.back();
            });
        } else {
            this.taskMgr.enableWorkflow(workflow).subscribe(() => {
                this.back();
            });
        }
    }

    getText(active: boolean): string {
        return active ? "Disable workflow" : "Enable workflow";
    }

    back(): void {
        this.location.back();
    }
}
