import { Component, computed, inject, Signal } from '@angular/core';
import {AuthService, Session} from '../Services/auth.service';

@Component({
  selector: 'app-user-session',
  imports: [],
  templateUrl: './user-session.component.html',
  styleUrl: './user-session.component.css'
})
export class UserSessionComponent {
  private readonly auth = inject(AuthService);
  public session: Signal<Session> = this.auth.session;
  public isAuthenticated = this.auth.isAuthenticated;
  public isAnonymous = this.auth.isAnonymous;

  // Computed signal for claims
  public claims = computed(() => this.session() || []);
}
