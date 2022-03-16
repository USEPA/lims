import { Component, OnInit, ViewChild } from "@angular/core";

import { Processor } from "src/app/models/processor.model";
import { TaskManagerService } from "src/app/services/task-manager.service";
import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";
import { MatPaginator } from "@angular/material/paginator";

@Component({
    selector: "app-processors",
    templateUrl: "./processors.component.html",
    styleUrls: ["./processors.component.css"],
})
export class ProcessorsComponent implements OnInit {
    loadingProcessors: boolean;
    statusMessage: string;
    addingProcessor: boolean;

    columnNames = ["name", "description", "file_type", "processor_status"];
    processors: Processor[];
    sortableData = new MatTableDataSource();

    constructor(private taskMgr: TaskManagerService) {}

    @ViewChild(MatSort, { static: true }) sort: MatSort;
    @ViewChild(MatPaginator) paginator: MatPaginator;
    ngOnInit() {
        this.loadingProcessors = true;
        this.statusMessage = "";
        this.processors = [];

        this.getProcessors();
    }

    ngAfterViewInit() {
        this.sortableData.paginator = this.paginator;
    }

    getProcessors(): void {
        this.taskMgr.getProcessors().subscribe(
            (processors) => {
                if (processors.error) {
                    this.statusMessage = processors.error;
                } else {
                    if (processors && processors.length > 0) {
                        this.processors = [...processors];
                        this.sortableData.data = [...this.processors];
                        this.sortableData.sort = this.sort;
                        this.statusMessage = "";
                        console.log("processors: ", this.processors);
                    } else {
                        this.statusMessage = "There are currently no Processors installed";
                    }
                }
            },
            (err) => {
                this.statusMessage = "Error retrieving data";
            },
            () => {
                this.loadingProcessors = false;
            }
        );
    }

    toggleEnable(processor): void {
        if (processor.enabled) {
            this.taskMgr.disableProcessor(processor).subscribe(() => {
                this.getProcessors();
            });
        } else {
            this.taskMgr.disableProcessor(processor).subscribe(() => {
                this.getProcessors();
            });
        }
    }
}
