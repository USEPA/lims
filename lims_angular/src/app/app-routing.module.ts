import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { TaskListComponent } from "./components/tasks/task-list/task-list.component";
import { UserListComponent } from "./components/users/user-list/user-list.component";
import { WorkflowListComponent } from "./components/workflows/workflow-list/workflow-list.component";
import { TaskDetailComponent } from "./components/tasks/task-detail/task-detail.component";
import { WorkflowDetailComponent } from "./components/workflows/workflow-detail/workflow-detail.component";
import { ProcessorListComponent } from "./components/processors/processor-list/processor-list.component";
import { WorkflowEditorComponent } from "./components/workflows/workflow-editor/workflow-editor.component";
import { LogListComponent } from "./components/logs/log-list/log-list.component";

const routes: Routes = [
    { path: "", component: TaskListComponent },
    { path: "logs", component: LogListComponent },
    { path: "tasks", component: TaskListComponent },
    { path: "tasks/detail/:id", component: TaskDetailComponent },
    { path: "users", component: UserListComponent },
    { path: "workflows", component: WorkflowListComponent },
    { path: "workflows/detail/:id", component: WorkflowDetailComponent },
    { path: "workflows/edit/:id", component: WorkflowEditorComponent },
    { path: "processors", component: ProcessorListComponent },
    { path: "**", redirectTo: "/" },
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
})
export class AppRoutingModule {}
