import { Injectable } from '@angular/core';
import { Action } from '@ngrx/store';
import { Router } from '@angular/router';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { Observable, of } from 'rxjs';
import { map, switchMap, catchError } from 'rxjs/operators';

import { AuthService } from '../../services/auth.service';

@Injectable()
export class AuthEffects {
  constructor(private actions: Actions, private auth: AuthService, private router: Router) {}
}
