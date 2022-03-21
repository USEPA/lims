import { Component, Inject } from "@angular/core";

import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";

@Component({
    selector: "app-delete-confirmation-dialog",
    templateUrl: "./delete-confirmation-dialog.component.html",
    styleUrls: ["./delete-confirmation-dialog.component.css"],
})
export class DeleteConfirmationDialogComponent {
    constructor(public dialogRef: MatDialogRef<any>, @Inject(MAT_DIALOG_DATA) public data: any) {}

    onNoClick(): void {
        this.dialogRef.close(false);
    }

    confirmDelete(): void {
        this.dialogRef.close(true);
    }
}
