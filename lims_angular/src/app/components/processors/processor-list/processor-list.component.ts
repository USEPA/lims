import { Component, OnInit, ViewChild } from "@angular/core";
import { FormControl } from "@angular/forms";

import { Observable } from "rxjs";
import { map, startWith } from "rxjs/operators";

import { MatSort } from "@angular/material/sort";
import { MatTableDataSource } from "@angular/material/table";
import { MatPaginator } from "@angular/material/paginator";

import { TaskManagerService } from "src/app/services/task-manager.service";

import { Processor } from "src/app/models/processor.model";

@Component({
    selector: "app-processor-list",
    templateUrl: "./processor-list.component.html",
    styleUrls: ["./processor-list.component.css"],
})
export class ProcessorListComponent implements OnInit {
    loadingProcessors: boolean;
    statusMessage: string;

    filter = "";

    filterInput = new FormControl();
    options: string[] = ["SCHEDULED", "CANCELLED"];
    filteredOptions: Observable<string[]>;

    columnNames = ["name", "description", "file_type", "processor_status"];
    processors: Processor[] = [];
    sortableData = new MatTableDataSource();

    addingProcessor: boolean;

    constructor(private taskMgr: TaskManagerService) {}

    @ViewChild(MatSort, { static: true }) sort: MatSort;
    @ViewChild(MatPaginator) paginator: MatPaginator;
    ngOnInit() {
        this.loadingProcessors = true;
        this.statusMessage = "";

        this.sortableData.data = [];
        this.filteredOptions = this.filterInput.valueChanges.pipe(
            startWith(""),
            map((value) => this.filterOptions(value))
        );
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
