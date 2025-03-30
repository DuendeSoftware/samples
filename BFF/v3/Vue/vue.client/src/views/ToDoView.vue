<template>
  <div class="todos">
    <div v-if="isAuthenticated">
      <h2>ToDo List</h2>
      <div class="mb-3">
        <form @submit.prevent="addTodo">
          <div class="input-group mb-2">
            <input
              type="text"
              class="form-control"
            placeholder="New ToDo item description"
            v-model="newTodoText"
            required
            />
            <input
              type="datetime-local"
              class="form-control"
              v-model="newTodoDateTime"
              required
            />
            <button class="btn btn-outline-primary" type="submit" :disabled="loading">
              Add ToDo
            </button>
          </div>
        </form>
      </div>
      <div v-if="loading" class="text-center">Loading...</div>
      <div v-if="error" class="alert alert-danger">{{ error }}</div>
      <ul class="list-group" v-if="!loading && todos.length > 0">
        <li v-for="todo in todos" :key="todo.id" class="list-group-item d-flex justify-content-between align-items-center">
          <div>
            <div>{{ todo.name }}</div>
            <small class="text-muted">{{ new Date(todo.date).toLocaleString() }}</small>
          </div>
          <button class="btn btn-sm btn-outline-danger" @click="deleteTodo(todo.id)" :disabled="loading">
            Delete
          </button>
        </li>
      </ul>
      <p v-if="!loading && todos.length === 0 && !error">No ToDo items yet!</p>

    </div>
    <div v-else>
      <p>Please <a href="#" @click.prevent="login">Login</a> to view ToDos.</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { isAuthenticated, login } from '../auth'; // Import auth state
import { fetchApi } from '../utils/api'; // Import the API utility

const todos = ref([]);
const newTodoText = ref('');
// Ref for the new date/time input. Initialize to current date/time
// The slice(0, 16) gets 'YYYY-MM-DDTHH:mm' format needed by datetime-local
const newTodoDateTime = ref(new Date().toISOString().slice(0, 16));
const loading = ref(false);
const error = ref(null);

// Fetch ToDos from the backend (no changes needed here)
async function fetchTodos() {
  loading.value = true;
  error.value = null;
  try {
    todos.value = await fetchApi('/todos'); // GET request
  } catch (err) {
    error.value = err.message || 'Failed to fetch ToDos.';
    todos.value = []; // Clear potentially stale data
  } finally {
    loading.value = false;
  }
}

async function addTodo() {
  // Basic validation
  if (!newTodoText.value.trim() || !newTodoDateTime.value) {
    error.value = "Please provide both description and date/time.";
    return;
  }

  loading.value = true;
  error.value = null;
  try {

    const newTodoPayload = {
      Name: newTodoText.value,
      Date: newTodoDateTime.value
    };

    await fetchApi('/todos', {
      method: 'POST',
      body: JSON.stringify(newTodoPayload),
    });
    newTodoText.value = '';

    await fetchTodos();
  } catch (err) {
    error.value = err.message || 'Failed to add ToDo.';
  }
}

async function deleteTodo(id) {
  loading.value = true;
  error.value = null;
  try {
    await fetchApi(`/todos/${id}`, { // DELETE request
      method: 'DELETE',
    });
    await fetchTodos();
  } catch (err) {
    error.value = err.message || 'Failed to delete ToDo.';
  }
}

onMounted(() => {
  if (isAuthenticated.value) {
    fetchTodos();
  }
});

</script>

<style scoped>
.todos{
  max-width: 600px;
  display: flex;
  flex-direction: column;
  margin: 0 auto;
}
.input-group > .form-control {
  margin-right: 0.5rem;
}
.input-group > .form-control:last-child {
  margin-right: 0;
}
.list-group {
  width: 100%;
}

@media (max-width: 768px) {
  .list-group-item {
    min-width: 0;
    width: 100%;
  }
}

@media (min-width: 769px) {
  .list-group-item {
    min-width: 300px;
  }
}
</style>
