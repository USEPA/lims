import { environment } from "../../environments/environment";

import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";

import { Observable, of } from "rxjs";
import { catchError, tap } from "rxjs/operators";

import { AuthService } from "./auth.service";

import { Task } from "../models/task.model";
import { Workflow } from "../models/workflow.model";

@Injectable({
    providedIn: "root",
})
export class TaskManagerService {
    private taskList: Task[];
    private workflows: Workflow[];
    private processors: any[];

    constructor(private http: HttpClient, private auth: AuthService) {
        this.getWorkflows().subscribe();
    }

    // GET/api/tasks - returns all tasks
    getTasks(): Observable<any> {
        return this.http.get<any>(environment.apiUrl + "tasks/").pipe(
            // timeout(5000),
            tap((tasks) => {
                if (tasks) {
                    this.taskList = [...tasks];
                }
            }),
            catchError((err) => {
                return of({ error: "failed to retrieve tasks!" });
            })
        );
    }

    // given an id returns a task obj from this.taskList
    getTask(id: string): Task {
        for (const task of this.taskList) {
            if (task.id === id) {
                return task;
            }
        }
        return {
            id: null,
            taskID: null,
            start: null,
            filePath: null,
            processor: null,
            workflowID: null,
            status: null,
            error: null,
            message: null,
        };
    }

    // GET/api/tasks/+id deletes a task by id
    deleteTask(id: string): Observable<any> {
        return this.http.delete(environment.apiUrl + `tasks/${id}`);
    }

    // GET/api/workflows - returns all workflows
    getWorkflows(): Observable<any> {
        return this.http.get<any>(environment.apiUrl + "workflows/").pipe(
            // timeout(5000),
            tap((workflows) => {
                if (workflows) {
                    this.workflows = [...workflows];
                }
            }),
            catchError((err) => {
                return of({ error: "failed to retrieve workflows!" });
            })
        );
    }

    // given an id returns a workflow obj from this.workflows
    getWorkflow(id: string): Workflow {
        if (this.workflows) {
            for (const wf of this.workflows) {
                if (id === wf.id) {
                    return wf;
                }
            }
        }
        return {
            id: null,
            name: null,
            processor: null,
            inputFolder: null,
            outputFolder: null,
            archiveFolder: null,
            interval: null,
            active: null,
            creationDate: null,
        };
    }

    // POST/api/utilitie/dircheck
    // accepts a dictionary of paths and returns a boolean for the existance of each
    validatePaths(paths): Observable<any> {
        return this.http.post<any>(environment.apiUrl + "utility/dircheck/", paths).pipe(
            catchError((err) => {
                return of({ error: err });
            })
        );
    }

    // POST/api/workflows - crates a new workflow
    createWorkflow(workflow: any): Observable<any> {
        const newWorkflow = JSON.stringify(workflow);
        return this.http.post<any>(environment.apiUrl + "workflows/", newWorkflow).pipe(
            // timeout(5000),
            catchError((err) => {
                return of({ error: "failed to add workflow!" });
            })
        );
    }

    // GET/api/workflows/execute/+id - immediately executes a task for a given workflow
    executeWorkflow(workflow: any): Observable<any> {
        return this.http.get(environment.apiUrl + `workflows/execute/${workflow.id}`).pipe(
            // timeout(5000),
            catchError((err) => {
                return of({ error: "failed to execute workflow!" });
            })
        );
    }

    // PUT/api/workflows - updates an existing workflow
    updateWorkflow(workflow: any): Observable<any> {
        const newWorkflow = JSON.stringify(workflow);
        return this.http.put<any>(environment.apiUrl + "workflows/", newWorkflow).pipe(
            // timeout(5000),
            catchError((err) => {
                return of({ error: "failed to update workflow!" });
            })
        );
    }

    // calls this.updateWorkflow, returns an Observable
    enableWorkflow(workflow): Observable<any> {
        workflow.active = true;
        return this.updateWorkflow(workflow);
    }

    // calls this.updateWorkflow, returns an Observable
    disableWorkflow(workflow): Observable<any> {
        workflow.active = false;
        return this.updateWorkflow(workflow);
    }

    // delete an existing workflow and associated tasks
    // DELETE/api/workflows/+id
    removeWorkflow(id: number): Observable<any> {
        return this.http.delete<any>(environment.apiUrl + "workflows/" + id).pipe(
            // timeout(5000),
            catchError((err) => {
                return of({ error: "failed to disable workflow!" });
            })
        );
    }

    // api/processors
    getProcessors(): Observable<any> {
        return this.http.get<any>(environment.apiUrl + "processors/").pipe(
            // timeout(5000),
            tap((processors) => {
                if (processors) {
                    this.processors = [...processors];
                }
            }),
            catchError((err) => {
                return of({ error: "failed to retrieve processors!" });
            })
        );
    }

    // PUT/api/processors
    updateProcessor(processor): Observable<any> {
        return this.http.put<any>(environment.apiUrl + "processors/", processor).pipe(
            // timeout(5000),
            catchError((err) => {
                return of({ error: "failed to update processor!" });
            })
        );
    }

    // calls this.updateProcessor, returns an Observable
    enableProcessor(processor): Observable<any> {
        processor.enabled = true;
        return this.updateProcessor(processor);
    }

    // calls this.updateProcessor, returns an Observable
    disableProcessor(processor): Observable<any> {
        processor.enabled = false;
        return this.updateProcessor(processor);
    }
}
