<template>
  <RouterView />
</template>

<script setup async>
import { RouterView } from 'vue-router';
import { checkAuthStatus } from '../auth';

try {
  // Perform the await for the initial auth check here
  console.log('AsyncRouterViewWrapper: awaiting checkAuthStatus...');
  await checkAuthStatus();
  console.log('AsyncRouterViewWrapper: checkAuthStatus complete.');
} catch (error) {
  console.error("Failed initial auth check inside AsyncRouterViewWrapper:", error);
}
</script>
