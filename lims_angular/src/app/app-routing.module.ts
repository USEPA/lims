import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { TaskListComponent } from "./components/tasks/task-list/task-list.component";
import { UsersComponent } from "./components/users/user-list/users.component";
import { WorkflowsComponent } from "./components/workflows/workflow-list/workflows.component";
import { TaskDetailComponent } from "./components/tasks/task-detail/task-detail.component";
import { WorkflowDetailComponent } from "./components/workflows/workflow-detail/workflow-detail.component";
import { ProcessorsComponent } from "./components/processors/processor-list/processors.component";
import { WorkflowEditorComponent } from "./components/workflows/workflow-editor/workflow-editor.component";
import { LogsComponent } from "./components/logs/log-list/logs.component";

const routes: Routes = [
    { path: "", component: TaskListComponent },
    { path: "logs", component: LogsComponent },
    { path: "tasks", component: TaskListComponent },
    { path: "tasks/detail/:id", component: TaskDetailComponent },
    { path: "users", component: UsersComponent },
    { path: "workflows", component: WorkflowsComponent },
    { path: "workflows/detail/:id", component: WorkflowDetailComponent },
    { path: "workflows/edit/:id", component: WorkflowEditorComponent },
    { path: "processors", component: ProcessorsComponent },
    { path: "**", redirectTo: "/" },
];

@NgModule({
    imports: [RouterModule.forRoot(routes)],
    exports: [RouterModule],
})
export class AppRoutingModule {}
