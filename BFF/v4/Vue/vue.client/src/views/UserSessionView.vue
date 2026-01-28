<template>
  <div class="user-session-container">
    <div v-if="isAuthenticated">
      <h2>User Session Claims</h2>
      <div v-if="user && user.length > 0">
        <div class="table-responsive">
          <table class="table table-striped table-bordered">
            <thead class="thead-dark">
              <tr>
                <th>Claim Type</th>
                <th>Claim Value</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(claim, index) in user" :key="index">
                <td>{{ claim.type }}</td>
                <td>{{ claim.value }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
      <div v-else>
        <p>Loading user claims or no claims found.</p>
      </div>
    </div>
    <div v-else>
      <p>User is not authenticated. Please <a href="#" @click.prevent="login">Login</a>.</p>
    </div>
  </div>
</template>

<script setup>
import { isAuthenticated, user, login } from '../auth';
</script>

<style scoped>
.user-session-container {
  width: 100%;
  padding: 15px;
  box-sizing: border-box;
}
@media (min-width: 769px) {
  .user-session-container {
    max-width: 1000px;
    margin: 0 auto;
  }
}

h2 {
  text-align: center;
  margin: 20px;
}
.table-responsive {
  width: 100%;
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
}
.table {
  width: 100%;
  min-width: 300px;
}
.thead-dark {
  color: #fff;
  background-color: #212529;
  border-color: #32383e;
}
</style>
