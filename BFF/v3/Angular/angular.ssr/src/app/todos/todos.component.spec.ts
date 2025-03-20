import { ComponentFixture, TestBed, fakeAsync, flush } from '@angular/core/testing';
import { TodosComponent } from './todos.component';
import { AuthService } from '../Services/auth.service';
import { FormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { signal, WritableSignal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {HttpClient, HttpErrorResponse} from '@angular/common/http';
import {defer, of, throwError } from 'rxjs';

describe('TodosComponent', () => {
  let component: TodosComponent;
  let fixture: ComponentFixture<TodosComponent>;
  let mockAuthService: Partial<AuthService>;
  let mockHttpClient: Partial<HttpClient>;
  let mockIsAuthenticated: WritableSignal<boolean>;
  let mockIsAnonymous: WritableSignal<boolean>;

  beforeEach(() => {
    mockIsAuthenticated = signal(false);
    mockIsAnonymous = signal(true);

    mockAuthService = {
      isAuthenticated: mockIsAuthenticated,
      isAnonymous: mockIsAnonymous,
    };

    mockHttpClient = {
      get: jasmine.createSpy('get').and.returnValue(of([])),  // Provides a default
      post: jasmine.createSpy('post').and.returnValue(of(null)),
      delete: jasmine.createSpy('delete').and.returnValue(of(null)),
    };

    TestBed.configureTestingModule({
      imports: [TodosComponent, FormsModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: HttpClient, useValue: mockHttpClient },
      ],
    });

    fixture = TestBed.createComponent(TodosComponent);
    component = fixture.componentInstance;
    // NO fixture.detectChanges() here!
  });


  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display login message when anonymous', () => {
    mockIsAnonymous.set(true);
    mockIsAuthenticated.set(false);
    fixture.detectChanges();

    const loginMessage = fixture.debugElement.query(By.css('p'));
    expect(loginMessage).toBeTruthy();
    expect(loginMessage.nativeElement.textContent).toContain('Please log in');
  });

  it('should display todo form and table when authenticated', fakeAsync(() => {
    mockIsAnonymous.set(false);
    mockIsAuthenticated.set(true);
    (mockHttpClient.get as jasmine.Spy).and.returnValue(of([]));
    fixture.detectChanges(); // Initial change detection

    const form = fixture.debugElement.query(By.css('form'));
    expect(form).toBeTruthy();

    const table = fixture.debugElement.query(By.css('table'));
    expect(table).toBeTruthy();
    flush();
  }));

  it('should create a new todo', fakeAsync(() => {
    mockIsAnonymous.set(false);
    mockIsAuthenticated.set(true);
    (mockHttpClient.get as jasmine.Spy).and.returnValue(of([])); //Initial fetch
    fixture.detectChanges(); // Initial change detection to show form.

    component.name.set('Test Todo');
    component.date.set('2024-03-15');
    fixture.detectChanges();  // Update the form inputs

    const newTodo = { id: 1, name: 'Test Todo', date: '2024-03-15', user: 'TestUser' };
    (mockHttpClient.post as jasmine.Spy).and.returnValue(of(newTodo));

    const createButton = fixture.debugElement.query(By.css('button')); // No need for [type="submit"]
    createButton.nativeElement.click();

    expect(mockHttpClient.post).toHaveBeenCalledWith('todos', { name: 'Test Todo', date: '2024-03-15' });

    flush();
    fixture.detectChanges();

    expect(component.todos().length).toBe(1);
    expect(component.todos()[0]).toEqual(newTodo);

    const tableRows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(tableRows.length).toBe(1);
    expect(tableRows[0].children[2].nativeElement.textContent).toContain('3/15/24');
    expect(tableRows[0].children[3].nativeElement.textContent).toContain('Test Todo');
  }));

  it('should delete a todo', fakeAsync(() => {
    mockIsAnonymous.set(false);
    mockIsAuthenticated.set(true);
    const mockTodos = [
      { id: 1, name: 'Todo 1', date: '2024-03-15', user: 'TestUser' },
      { id: 2, name: 'Todo 2', date: '2024-03-16', user: 'TestUser' },
    ];

    (mockHttpClient.get as jasmine.Spy).and.returnValue(of(mockTodos));
    fixture.detectChanges();
    flush();
    fixture.detectChanges();


    (mockHttpClient.delete as jasmine.Spy).and.returnValue(of({}));

    const deleteButton = fixture.debugElement.query(By.css('tbody tr button')); // First button
    deleteButton.nativeElement.click();

    expect(mockHttpClient.delete).toHaveBeenCalledWith('todos/1');

    flush();
    fixture.detectChanges();

    expect(component.todos().length).toBe(1);
    expect(component.todos()[0].id).toBe(2);

    const tableRows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(tableRows.length).toBe(1);
    expect(tableRows[0].children[1].nativeElement.textContent).toBe('2');
  }));

  it('should display error message on error', fakeAsync(() => {
    mockIsAnonymous.set(false);
    mockIsAuthenticated.set(true);

    const errorMessage = 'Test Error Message';

    (mockHttpClient.get as jasmine.Spy).and.returnValue(
      throwError(() => new HttpErrorResponse({ error: errorMessage, status: 500, statusText: errorMessage})) // Pass in error
    );

    fixture.detectChanges();
    flush();
    fixture.detectChanges();

    const errorDiv = fixture.debugElement.query(By.css('.alert-warning'));
    expect(errorDiv).toBeTruthy();
    expect(errorDiv.nativeElement.textContent).toBe('Error: Test Error Message'); // Exact match
  }));


  it('should display no todo message when authenticated and no data', fakeAsync(() => {
    mockIsAnonymous.set(false);
    mockIsAuthenticated.set(true);
    (mockHttpClient.get as jasmine.Spy).and.returnValue(of([]));
    fixture.detectChanges();

    flush();

    const emptyMessage = fixture.debugElement.query(By.css('td[colspan="5"]'));
    expect(emptyMessage).toBeTruthy();
  }));


  it('should not display error message when created correctly', fakeAsync(() => {
    mockIsAnonymous.set(false);
    mockIsAuthenticated.set(true);
    (mockHttpClient.get as jasmine.Spy).and.returnValue(of([]));
    fixture.detectChanges();

    component.name.set('Test Todo');
    component.date.set('2024-03-15');
    fixture.detectChanges();

    const newTodo = { id: 1, name: 'Test Todo', date: '2024-03-15', user: 'TestUser' };
    (mockHttpClient.post as jasmine.Spy).and.returnValue(of(newTodo));

    const createButton = fixture.debugElement.query(By.css('button')); // No need for [type="submit"]
    expect(createButton.nativeElement.disabled).toBe(false); //button should not be disabled

    createButton.nativeElement.click();


    flush();
    fixture.detectChanges();

    const errorDiv = fixture.debugElement.query(By.css('.alert-warning'));
    expect(errorDiv).toBeNull();
  }));
});
