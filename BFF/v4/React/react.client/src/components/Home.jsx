import React, { Component } from "react";

export class Home extends Component {
  static displayName = Home.name;

  constructor(props) {
    super(props);
    this.state = {
      todos: [],
      loading: true,
      error: null,
      todoName: "Do something",
      todoDate: "2021-03-01",
    };

    this.createTodo = this.createTodo.bind(this);
    this.deleteTodo = this.deleteTodo.bind(this);
  }

  componentDidMount() {
    (async () => this.populateTodos())();
  }

  async populateTodos() {
    const response = await fetch("todos", {
      headers: {
        "X-CSRF": 1,
      },
    });
    if (response.ok) {
      const data = await response.json();
      this.setState({ todos: data, loading: false, error: null });
    } else if (response.status !== 401) {
      this.setState({ error: response.status });
    }
  }

  async createTodo(e) {
    e.preventDefault();
    const response = await fetch("todos", {
      method: "POST",
      headers: {
        "content-type": "application/json",
        "x-csrf": "1",
      },
      body: JSON.stringify({
        name: this.state.todoName,
        date: this.state.todoDate,
      }),
    });

    if (response.ok) {
      var item = await response.json();
      this.setState({
        todos: [...this.state.todos, item],
        todoName: "Do something",
        todoDate: "2021-03-02",
      });
    } else {
      this.setState({ error: response.status });
    }
  }

  async deleteTodo(id) {
    const response = await fetch(`todos/${id}`, {
      method: "DELETE",
      headers: {
        "x-csrf": 1,
      },
    });
    if (response.ok) {
      const todos = this.state.todos.filter((x) => x.id !== id);
      this.setState({ todos });
    } else {
      this.setState({ error: response.status });
    }
  }

  render() {
    return (
      <>
        <div className="banner">
          <h1>TODOs</h1>
        </div>

        <div className="row">
          <div className="col">
            <h3>Add New</h3>
          </div>
          
          <form className="form-inline">
            <div className="row g-3">
              <div className="col-6">
                <label htmlFor="date" className="form-label">Todo Date</label>
                <input
                  className="form-control"
                  type="date"
                  value={this.state.todoDate}
                  onChange={(e) => this.setState({ todoDate: e.target.value })}
                  />
              </div>
              <div className="col-6">
                <label htmlFor="name" className="form-label">Todo Name</label>
                <input
                  className="form-control"
                  value={this.state.todoName}
                  onChange={(e) => this.setState({ todoName: e.target.value })}
                  />
              </div>

              <div className="col-12">
                <button
                  className="btn btn-primary"
                  onClick={this.createTodo}
                  >
                  Create
                </button>
              </div>
            </div>
          </form>
        </div>
        {this.state.error !== null && (
          <div className="row">
            <div className="col">
              <div className="alert alert-warning hide">
                <strong>Error: </strong>
                <span>{this.state.error}</span>
              </div>
            </div>
          </div>
        )}

        <div className="row">
          <table className="table table-striped table-sm">
            <thead>
              <tr>
                <th/>
                <th>Id</th>
                <th>Date</th>
                <th>Note</th>
                <th>User</th>
              </tr>
            </thead>
            <tbody>
              {this.state.todos.map((todo) => (
                <tr key={todo.id}>
                  <td>
                    <button
                      onClick={async () => this.deleteTodo(todo.id)}
                      className="btn btn-danger"
                    >
                      delete
                    </button>
                  </td>
                  <td>{todo.id}</td>
                  <td>{todo.date}</td>
                  <td>{todo.name}</td>
                  <td>{todo.user}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </>
    );
  }
}
