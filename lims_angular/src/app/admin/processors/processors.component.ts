import { Component, OnInit, ViewChild } from "@angular/core";

import { Processor } from "src/app/models/processor.model";
import { TaskManagerService } from "src/app/services/task-manager.service";
import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";

@Component({
  selector: "app-processors",
  templateUrl: "./processors.component.html",
  styleUrls: ["./processors.component.css"]
})
export class ProcessorsComponent implements OnInit {
  loadingProcessors: boolean;
  statusMessage: string;
  addingProcessor: boolean;

  columnNames = ["name", "description", "file_type"];
  processors: Processor[];
  sortableData = new MatTableDataSource();

  constructor(private fileMgr: TaskManagerService) {}

  @ViewChild(MatSort, { static: true }) sort: MatSort;
  ngOnInit() {
    this.loadingProcessors = true;
    this.statusMessage = "";
    this.processors = [];

    this.fileMgr.getProcessors().subscribe(
      processors => {
        if (processors.error) {
          this.statusMessage = processors.error;
        } else {
          if (processors && processors.length > 0) {
            this.processors = [...processors];
            this.sortableData.data = [...this.processors];
            this.sortableData.sort = this.sort;
            this.statusMessage = "";
          } else {
            this.statusMessage = "There are currently no Processors installed";
          }
        }
      },
      err => {
        this.statusMessage = "Error retrieving data";
      },
      () => {
        this.loadingProcessors = false;
      }
    );
  }
}
