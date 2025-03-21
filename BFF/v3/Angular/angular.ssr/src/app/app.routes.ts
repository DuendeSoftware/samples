import { Routes } from '@angular/router';
import {AppComponent} from './app.component';
import { UserSessionComponent } from './user-session/user-session.component';
import {TodosComponent} from './todos/todos.component';

export const routes: Routes = [
  { path: '', component: TodosComponent, pathMatch: 'full' },
  { path: 'user-session', component: UserSessionComponent },
];
