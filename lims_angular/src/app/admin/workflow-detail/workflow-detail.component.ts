import { Component, OnInit } from "@angular/core";
import { Location } from "@angular/common";
import { ActivatedRoute } from "@angular/router";

import { TaskManagerService } from "../../services/task-manager.service";
import { Workflow } from "../../models/workflow.model";

@Component({
  selector: "app-workflow-detail",
  templateUrl: "./workflow-detail.component.html",
  styleUrls: ["./workflow-detail.component.css"]
})
export class WorkflowDetailComponent implements OnInit {
  workflow: Workflow;
  constructor(
    private taskMgr: TaskManagerService,
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
  editWorkflow(): void {
    // open edit component
  }

  // api call
  deleteWorkflow(): void {
    // remove workflow from workflow list
  }

  back(): void {
    this.location.back();
  }
}
