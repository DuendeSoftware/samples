<template>
  <header>
    <nav
      class="navbar navbar-expand-sm navbar-light bg-white border-bottom box-shadow mb-3 mt-3"
    >
      <div class="container">
        <router-link class="navbar-brand" to="/">Vue BFF Sample</router-link>
        <button
          class="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target=".navbar-collapse"
          aria-controls="navbarNav"
          :aria-expanded="isExpanded"
          aria-label="Toggle navigation"
          @click="toggle"
        >
          <span class="navbar-toggler-icon"></span>
        </button>
        <div
          class="navbar-collapse collapse d-sm-inline-flex justify-content-end"
          :class="{ show: isExpanded }"
        >
          <ul class="navbar-nav flex-grow">
            <li class="nav-item">
              <router-link
                class="nav-link text-dark"
                to="/"
                exact-active-class="link-active"
              >Home
              </router-link
              >
            </li>
            <li class="nav-item" v-if="isAuthenticated">
              <router-link
                class="nav-link text-dark"
                to="/user-session"
                exact-active-class="link-active"
              >Show User Session
              </router-link
              >
            </li>
            <li class="nav-item" v-if="isAuthenticated">
              <router-link
                class="nav-link text-dark"
                to="/todos"
                exact-active-class="link-active"
              >ToDo
              </router-link
              >
            </li>
            <li class="nav-item" v-else>
              <a class="nav-link text-dark" href="bff/login">Login</a>
            </li>
            <li class="nav-item" v-if="isAuthenticated">
              <a class="nav-link text-dark" @click.prevent="logout" href="#">Signout</a>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  </header>
</template>

<script setup>
import {onMounted, ref} from 'vue'
import {isAuthenticated, logout} from '../auth'

const isExpanded = ref(false)
const toggle = () => {
  isExpanded.value = !isExpanded.value
}

</script>

<style scoped>
a.navbar-brand {
  white-space: normal;
  text-align: center;
  word-break: break-all;
}

html {
  font-size: 14px;
}

@media (min-width: 768px) {
  html {
    font-size: 16px;
  }
}

.box-shadow {
  box-shadow: 0 0.25rem 0.75rem rgba(0, 0, 0, 0.05);
}
</style>
