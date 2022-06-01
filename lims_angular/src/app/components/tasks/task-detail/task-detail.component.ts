import { Component, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { Location } from "@angular/common";

import { TaskManagerService } from "../../../services/task-manager.service";

import { Task } from "../../../models/task.model";

@Component({
    selector: "app-task-detail",
    templateUrl: "./task-detail.component.html",
    styleUrls: ["./task-detail.component.css"],
})
export class TaskDetailComponent implements OnInit {
    task: Task;
    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private taskMgr: TaskManagerService,
        private location: Location
    ) {}

    ngOnInit() {
        const id = this.route.snapshot.paramMap.get("id");
        this.taskMgr.getTask(id).subscribe((task) => {
            if (!task) {
                this.router.navigateByUrl("/");
            }
            this.task = task;
            this.taskMgr.getWorkflow(this.task.workflowID).subscribe((workflow) => {
                this.task["workflowName"] = workflow.name;
                this.task["processor"] = workflow.processor;
                this.task["inputFolder"] = workflow.inputFolder;
            });
        });
    }

    rerunTask(id: number): void {
        // re-run existing task
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
