import { BrowserModule } from "@angular/platform-browser";
import { NgModule } from "@angular/core";

import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";

import { AppRoutingModule } from "./app-routing.module";
import { AppComponent } from "./app.component";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";

import { FormsModule, ReactiveFormsModule } from "@angular/forms";

import { CookieService } from "ngx-cookie-service";

import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MatTableModule } from "@angular/material/table";
import { MatSortModule } from "@angular/material/sort";
import { MatPaginatorModule } from "@angular/material/paginator";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatAutocompleteModule } from "@angular/material/autocomplete";
import { MatDialogModule } from "@angular/material/dialog";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatIconModule } from "@angular/material/icon";

import { LoginComponent } from "./admin/login/login.component";
import { MainComponent } from "./components/main/main.component";
import { TaskListComponent } from "./components/tasks/task-list/task-list.component";
import { UserListComponent } from "./components/users/user-list/user-list.component";
import { HeaderComponent } from "./components/header/header.component";
import { WorkflowListComponent } from "./components/workflows/workflow-list/workflow-list.component";
import { TaskDetailComponent } from "./components/tasks/task-detail/task-detail.component";
import { WorkflowEditorComponent } from "./components/workflows/workflow-editor/workflow-editor.component";
import { UserEditorComponent } from "./admin/user-editor/user-editor.component";
import { WorkflowDetailComponent } from "./components/workflows/workflow-detail/workflow-detail.component";
import { RegistrationComponent } from "./admin/registration/registration.component";
import { ProcessorListComponent } from "./components/processors/processor-list/processor-list.component";
import { LogListComponent } from "./components/logs/log-list/log-list.component";

import { DeleteConfirmationDialogComponent } from "./components/dialogs/delete-confirmation-dialog/delete-confirmation-dialog.component";

import { AuthInterceptor } from "./interceptors/auth.interceptor";
import { UnauthorizedRedirect } from "./interceptors/unauthorized-redirect.interceptor";

import { HighlightSearchPipe } from "./pipes/highlight-search.pipe";

@NgModule({
    declarations: [
        AppComponent,
        LoginComponent,
        MainComponent,
        TaskListComponent,
        UserListComponent,
        HeaderComponent,
        WorkflowListComponent,
        TaskDetailComponent,
        WorkflowEditorComponent,
        UserEditorComponent,
        WorkflowDetailComponent,
        RegistrationComponent,
        ProcessorListComponent,
        LogListComponent,
        HighlightSearchPipe,
        DeleteConfirmationDialogComponent,
    ],
    imports: [
        BrowserModule,
        HttpClientModule,
        AppRoutingModule,
        BrowserAnimationsModule,
        FormsModule,
        ReactiveFormsModule,
        MatInputModule,
        MatToolbarModule,
        MatTableModule,
        MatSortModule,
        MatPaginatorModule,
        MatCardModule,
        MatButtonModule,
        MatSelectModule,
        MatProgressSpinnerModule,
        MatAutocompleteModule,
        MatDialogModule,
        MatTooltipModule,
        MatIconModule,
    ],
    providers: [
        CookieService,
        {
            provide: HTTP_INTERCEPTORS,
            useClass: AuthInterceptor,
            multi: true,
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: UnauthorizedRedirect,
            multi: true,
        },
    ],
    bootstrap: [AppComponent],
})
export class AppModule {}
