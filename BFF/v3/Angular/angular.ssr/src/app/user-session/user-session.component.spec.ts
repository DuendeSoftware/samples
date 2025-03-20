import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserSessionComponent } from './user-session.component';
import {signal, WritableSignal} from '@angular/core';
import {AuthService, Claim, Session} from '../Services/auth.service';
import {By} from '@angular/platform-browser';

describe('UserSessionComponent', () => {
  let component: UserSessionComponent;
  let fixture: ComponentFixture<UserSessionComponent>;
  let mockAuthService: Partial<AuthService>;
  let mockSession: WritableSignal<Session>;
  let mockIsAuthenticated: WritableSignal<boolean>;
  let mockIsAnonymous: WritableSignal<boolean>;

  beforeEach(async () => {
    mockSession = signal<Session>(null);
    mockIsAuthenticated = signal(false);
    mockIsAnonymous = signal(true);

    mockAuthService = {
      session: mockSession,
      isAuthenticated: mockIsAuthenticated,
      isAnonymous: mockIsAnonymous,
    };
    await TestBed.configureTestingModule({
      imports: [UserSessionComponent], providers: [{
        provide: AuthService, useValue: mockAuthService
      }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserSessionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('should display "Loading..." when anonymous', () => {
    mockIsAnonymous.set(true);
    mockIsAuthenticated.set(false);
    fixture.detectChanges();

    const loadingElement = fixture.debugElement.query(By.css('p'));
    expect(loadingElement).toBeTruthy();
    expect(loadingElement.nativeElement.textContent).toContain('Loading...');

    const tableElement = fixture.debugElement.query(By.css('table'));
    expect(tableElement).toBeNull();
  });

  it('should display the table with claims when authenticated', () => {
    const testClaims: Claim[] = [
      { type: 'name', value: 'Test User' },
      { type: 'role', value: 'Admin' },
    ];
    mockSession.set(testClaims);
    mockIsAnonymous.set(false);
    mockIsAuthenticated.set(true);
    fixture.detectChanges();

    const tableElement = fixture.debugElement.query(By.css('table'));
    expect(tableElement).toBeTruthy();

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows.length).toBe(2);

    expect(rows[0].children[0].nativeElement.textContent).toContain('name');
    expect(rows[0].children[1].nativeElement.textContent).toContain('Test User');
    expect(rows[1].children[0].nativeElement.textContent).toContain('role');
    expect(rows[1].children[1].nativeElement.textContent).toContain('Admin');
  });

  it('should display "No claims available" when authenticated but session is empty', () => {
    mockSession.set([]);
    mockIsAuthenticated.set(true);
    mockIsAnonymous.set(false);
    fixture.detectChanges();

    const tableElement = fixture.debugElement.query(By.css('table'));
    expect(tableElement).toBeTruthy();

    const emptyMessage = fixture.debugElement.query(By.css('td[colspan="2"]'));
    expect(emptyMessage).toBeTruthy();
    expect(emptyMessage.nativeElement.textContent).toContain(
      'No claims available'
    );
  });

  it('should display "No claims available" when authenticated but session is null', () => {
    mockSession.set(null);
    mockIsAuthenticated.set(true);
    mockIsAnonymous.set(false);
    fixture.detectChanges();
    const tableElement = fixture.debugElement.query(By.css('table'));
    expect(tableElement).toBeTruthy();

    const emptyMessage = fixture.debugElement.query(By.css('td[colspan="2"]'));
    expect(emptyMessage).toBeTruthy();
    expect(emptyMessage.nativeElement.textContent).toContain('No claims available.');
  });
});
