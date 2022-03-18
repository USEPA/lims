import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { TasklistComponent } from "./components/tasklist/tasklist.component";
import { UsersComponent } from "./components/users/users.component";
import { WorkflowsComponent } from "./components/workflows/workflows.component";
import { TaskDetailComponent } from "./components/task-detail/task-detail.component";
import { WorkflowDetailComponent } from "./components/workflow-detail/workflow-detail.component";
import { ProcessorsComponent } from "./components/processors/processors.component";
import { WorkflowEditorComponent } from "./components/workflow-editor/workflow-editor.component";
import { LogsComponent } from "./components/logs/logs.component";

const routes: Routes = [
    { path: "", component: TasklistComponent },
    { path: "logs", component: LogsComponent },
    { path: "tasks", component: TasklistComponent },
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
