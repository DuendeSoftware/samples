import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, signal, inject, effect } from '@angular/core';
import { AuthService } from '../Services/auth.service';
import { FormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-todos',
  standalone: true, // Make it standalone!  Very important.
  imports: [FormsModule, CommonModule, DatePipe],
  templateUrl: './todos.component.html',
})
export class TodosComponent {

  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  // Signals
  public readonly todos = signal<Todo[]>([]);
  public readonly date = signal<string>((new Date()).toISOString().split('T')[0]);
  public readonly name = signal<string>("");
  public hasError = false;
  public errorMessage = '';

  public authenticated = this.auth.isAuthenticated;
  public anonymous = this.auth.isAnonymous;


  // Fetch todos effect
  private fetchTodosEffect = effect(() => {
    if (this.authenticated()) {
      this.fetchTodos();
    }
  });


  public createTodo(): void {
    this.http
      .post<Todo | null>('todos', {
        name: this.name(),
        date: this.date(),
      })
      .pipe(
        catchError(this.showError)
      )
      .subscribe((todo) => {
        if (todo) {
          this.todos.update((currentTodos) => [...currentTodos, todo]);
          this.name.set("");
        }
      });
  }

  public deleteTodo(id: number): void {
    this.http.delete(`todos/${id}`)
      .pipe(catchError(this.showError))
      .subscribe(() => {
        this.todos.update((currentTodos) =>
          currentTodos.filter((x) => x.id !== id)
        );
      });
  }

  private fetchTodos(): void {
    this.http
      .get<Todo[] | null>('todos')
      .pipe(catchError(this.showError))
      .subscribe((todos) => {
        if (todos) {
          this.todos.set(todos);
        }
      });
  }

  private readonly showError = (err: HttpErrorResponse) => {
    this.hasError = true;
    let message = 'An unknown error occurred!';

    // Check different possible locations of the error message
    if (err.error && typeof err.error === 'string') {
      message = err.error; // Directly a string.
    } else if (err.error && err.error.message) {
      message = err.error.message; // Common place for error messages
    } else if (err.error && err.error.detail) {
      message = err.error.detail;  //ASP.NET Core problem details
    } else if (err.message) {
      message = err.message; // Fallback to the top-level message
    }
    this.errorMessage = message;
    return of(null);
  };
}

interface Todo {
  id: number;
  name: string;
  date: string;
  user: string;
}
