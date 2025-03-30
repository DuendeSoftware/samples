import {createRouter, createWebHistory} from 'vue-router'
import HomeView from '../views/HomeView.vue'
import {isAuthenticated} from "@/auth.js";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/user-session',
      name: 'user-session',
      component: () => import('../views/UserSessionView.vue'),
      beforeEnter: (to, from, next) => {
        if (isAuthenticated.value) {
          next(); // Proceed
        } else {
          console.warn('Access denied: /user-session requires authentication.');
          next('/'); // Redirect to home
        }
      }
    },
    {
      path: '/todos',
      name: 'todos',
      component: () => import('../views/TodoView.vue'),
      beforeEnter: (to, from, next) => {
        if (isAuthenticated.value) {
          next();
        } else {
          console.warn('Access denied: /todos requires authentication.');
          next('/');
        }
      }
    }
  ],
})

export default router
