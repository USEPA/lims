import * as auth from './reducers/auth.reducers';

export interface AppState {
  authState: auth.State;
}
