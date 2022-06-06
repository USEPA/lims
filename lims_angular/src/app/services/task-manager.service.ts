import { environment } from "../../environments/environment";

import { Injectable, OnDestroy } from "@angular/core";
import { HttpClient } from "@angular/common/http";

import { Observable, of, Subject } from "rxjs";
import { catchError, switchMap, takeUntil, tap } from "rxjs/operators";

import { AuthService } from "./auth.service";

import { Task } from "../models/task.model";
import { Workflow } from "../models/workflow.model";

@Injectable({
    providedIn: "root",
})
export class TaskManagerService implements OnDestroy {
    private ngUnsubscribe = new Subject();

    private taskList: Task[] = [];
    private workflows: Workflow[] = [];

    constructor(private http: HttpClient, private auth: AuthService) {
        this.getWorkflows().subscribe();
    }

    ngOnDestroy() {
        this.ngUnsubscribe.next();
        this.ngUnsubscribe.complete();
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

    // given an id returns an Observable that resolves a Task
    getTask(id: string): Observable<any> {
        return this.getTasks().pipe(
            switchMap((tasks: any[]) => {
                let task = tasks.find((task) => {
                    return task.id == id;
                });

                return of({ ...task });
            }),
            takeUntil(this.ngUnsubscribe),
            catchError((err) => {
                return of({ error: "failed to retrieve workflow!" });
            })
        );
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

    // given an id returns an Observable that resolves a Workflow
    getWorkflow(id: string): Observable<any> {
        return this.getWorkflows().pipe(
            switchMap((workflows: any[]) => {
                let workflow = workflows.find((workflow) => {
                    return workflow.id == id;
                });

                return of({ ...workflow });
            }),
            takeUntil(this.ngUnsubscribe),
            catchError((err) => {
                return of({ error: "failed to retrieve workflow!" });
            })
        );
    }

    // POST/api/utility/dircheck
    // accepts a dictionary of paths and returns a boolean for the existence of each
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
    executeWorkflow(id: string): Observable<any> {
        return this.http.get(environment.apiUrl + `workflows/execute/${id}`).pipe(
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
