<script setup lang="ts">
import { ref, onMounted } from 'vue'
import HelloWorld from './components/HelloWorld.vue'

const user = ref<any[] | null>(null)
const loading = ref(true)

const getClaim = (type: string) => {
  if (!user.value) return null
  const claim = user.value.find((c: any) => c.type === type)
  return claim ? claim.value : null
}

onMounted(async () => {
  try {
    const res = await fetch('/bff/user', { credentials: 'include' })
    if (!res.ok) {
      user.value = null
    } else {
      const claims = await res.json()
      if (Array.isArray(claims) && claims.length > 0) {
        user.value = claims
      } else {
        user.value = null
      }
    }
  } catch {
    user.value = null
  }
  loading.value = false
})
</script>

<template>
  <div>
    <a href="https://vite.dev" target="_blank">
      <img src="/vite.svg" class="logo" alt="Vite logo" />
    </a>
    <a href="https://vuejs.org/" target="_blank">
      <img src="./assets/vue.svg" class="logo vue" alt="Vue logo" />
    </a>
  </div>
  <HelloWorld msg="Vite + Vue" />

  <div style="margin-top:2rem">
    <template v-if="loading">
      Loading...
    </template>
    <template v-else>
      <template v-if="!user">
        <button @click="window.location.href = '/bff/login'">Login</button>
      </template>
      <template v-else>
        <button @click="window.location.href = getClaim('bff:logout_url') || '/bff/logout'">Logout</button>
        <div style="margin-top:1rem;text-align:left">
          <h3>User Info</h3>
          <ul>
            <li><strong>Name:</strong> {{ getClaim('name') }}</li>
            <li><strong>Subject:</strong> {{ getClaim('sub') }}</li>
            <li><strong>IdP:</strong> {{ getClaim('idp') }}</li>
            <li><strong>SID:</strong> {{ getClaim('sid') }}</li>
            <li><strong>Logout URL:</strong> {{ getClaim('bff:logout_url') }}</li>
          </ul>
        </div>
      </template>
    </template>
  </div>
</template>

<style scoped>
.logo {
  height: 6em;
  padding: 1.5em;
  will-change: filter;
  transition: filter 300ms;
}
.logo:hover {
  filter: drop-shadow(0 0 2em #646cffaa);
}
.logo.vue:hover {
  filter: drop-shadow(0 0 2em #42b883aa);
}
</style>
