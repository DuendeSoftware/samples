<script setup>
import NavView from './views/NavView.vue';
import AsyncRouterViewWrapper from './components/AsyncRouterViewWrapper.vue';
</script>

<template>
  <header>
    <div class="wrapper">
      <NavView />
    </div>
  </header>

  <Suspense>
    <template #default>
      <AsyncRouterViewWrapper />
    </template>
    <template #fallback>
      <div class="container text-center mt-5">
        <p>Loading application...</p>
      </div>
    </template>
  </Suspense>
</template>

<style scoped>
</style>
