import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { Subscription } from 'rxjs';

import { AuthService } from "../../services/auth.service";
import { TaskManagerService } from "../../services/task-manager.service";
import { EnvService } from "../../services/env.service";



@Component({
    selector: "app-header",
    templateUrl: "./header.component.html",
    styleUrls: ["./header.component.css"],
})
export class HeaderComponent implements OnInit {
    
    errors = [];  // list of workflows with errors
    configSetSub: Subscription;
    envName: string = "";

    constructor(
        private auth: AuthService,
        private taskMgr: TaskManagerService,
        private router: Router,
        private envService: EnvService
    ) {}

    ngOnInit() {
        this.configSetSub = this.envService.configSetObservable.subscribe(configSet => {
          if (configSet === true) {
            this.envName = this.envService.config.envName;
          }
        });
    }

    logout(): void {
        // logs user out
        this.auth.logout();
    }

    gotoLogs(): void {
        this.router.navigateByUrl("/logs");
    }

    gotoTasks(): void {
        this.router.navigateByUrl("/tasks");
    }

    gotoUsers(): void {
        this.router.navigateByUrl("/users");
    }

    gotoWorkflows(): void {
        this.router.navigateByUrl("/workflows");
    }

    gotoProcessors(): void {
        this.router.navigateByUrl("/processors");
    }

    isAuthenticated(): boolean {
        return this.auth.isAuthenticated();
    }
}
