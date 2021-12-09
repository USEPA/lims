import { BrowserModule } from "@angular/platform-browser";
import { NgModule } from "@angular/core";

import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";

import { AppRoutingModule } from "./app-routing.module";
import { AppComponent } from "./app.component";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";

import { CookieService } from "ngx-cookie-service";

import { FormsModule, ReactiveFormsModule } from "@angular/forms";

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

import { LoginComponent } from "./admin/login/login.component";
import { MainComponent } from "./main/main.component";
import { TasklistComponent } from "./admin/tasklist/tasklist.component";
import { UsersComponent } from "./admin/users/users.component";
import { HeaderComponent } from "./header/header.component";
import { WorkflowsComponent } from "./admin/workflows/workflows.component";
import { TaskDetailComponent } from "./admin/task-detail/task-detail.component";
import { WorkflowEditorComponent } from "./admin/workflow-editor/workflow-editor.component";
import { UserEditorComponent } from "./admin/user-editor/user-editor.component";
import { WorkflowDetailComponent } from "./admin/workflow-detail/workflow-detail.component";
import { RegistrationComponent } from "./admin/registration/registration.component";

import { ProcessorsComponent } from "./admin/processors/processors.component";

import { UnauthorizedRedirect } from "./interceptors/unauthorized-redirect.interrceptor";
import { LogsComponent } from "./components/logs/logs.component";

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    MainComponent,
    TasklistComponent,
    UsersComponent,
    HeaderComponent,
    WorkflowsComponent,
    TaskDetailComponent,
    WorkflowEditorComponent,
    UserEditorComponent,
    WorkflowDetailComponent,
    RegistrationComponent,
    ProcessorsComponent,
    LogsComponent,
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
  ],
  providers: [
    CookieService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: UnauthorizedRedirect,
      multi: true,
    },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
