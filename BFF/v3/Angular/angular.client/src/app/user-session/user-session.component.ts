import { Component, Inject, OnInit } from '@angular/core';
import { AuthenticationService, Session } from '../authentication.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-user-session',
  templateUrl: './user-session.component.html',
  styleUrls: ['./user-session.component.css']
})
export class UserSessionComponent {
  public session$: Observable<Session>;
  public isAuthenticated$: Observable<boolean>;
  public isAnonymous$: Observable<boolean>;

  constructor(auth: AuthenticationService) {
    this.session$ = auth.getSession();
    this.isAuthenticated$ = auth.getIsAuthenticated();
    this.isAnonymous$ = auth.getIsAnonymous();
  }
}
