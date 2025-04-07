import { ref } from 'vue';

export const isAuthenticated = ref(false);
export const user = ref(null); // Holds user claims after successful check

/**
 * Checks the user's authentication status by calling the BFF's user endpoint.
 * Mimics app.js fetch approach but retains 'credentials: include' for cross-origin.
 */
export async function checkAuthStatus() {
  try {
    const res = await fetch('/bff/user', {
      credentials: 'include',
      headers: {
        'X-CSRF': '1'
      }
    });

    if (res.ok) {
      isAuthenticated.value = true;
      user.value = await res.json();
    } else if (res.status === 401) {
      isAuthenticated.value = false;
      user.value = null;
      console.log('User is not authenticated (401 from /bff/user).');
    } else {
      console.error('Error checking auth status:', res.status, res.statusText);
      isAuthenticated.value = false;
      user.value = null;
    }
  } catch (error) {
    console.error('Network error checking auth status:', error);
    isAuthenticated.value = false;
    user.value = null;
  }
}

/**
 * Initiates the login flow by redirecting to the BFF's login endpoint.
 */
export function login() {
  window.location.href = '/bff/login';
}

/**
 * Initiates the logout flow using the secure logout URL from user claims.
 * Mimics the app.js logout approach.
 */
export function logout() {
  if (user.value) {
    const logoutUrlClaim = user.value.find(claim => claim.type === 'bff:logout_url');
    if (logoutUrlClaim) {
      window.location.href = logoutUrlClaim.value;
      return;
    } else {
      console.error("Security Error: bff:logout_url claim not found in user data.");
    }
  }

  // Fallback ONLY if user claims were somehow not loaded (less secure)
  console.warn("User claims not loaded, attempting simple fallback logout.");
  window.location.href = '/bff/logout';
}

