async function checkAuthentication() {
  try {
    let response = await fetch('/bff/user', { credentials: 'include', headers: { "x-csrf": 1 } });

    let wrapper = document.querySelector('.schemes.wrapper');

    wrapper.insertAdjacentHTML('beforeend', `<div class='auth-wrapper'><button class='btn authorize unlocked btn-success'></button></div>`);

    let authorizeButton = document.querySelector('.swagger-ui .auth-wrapper .btn');
    if (!authorizeButton) return;

    if (response.status === 401) {
      // User is unauthenticated
      authorizeButton.innerText = "Log in";
      authorizeButton.onclick = function (event) {
        event.preventDefault();
        window.location.href = "/bff/login?returnUrl=" + window.location.pathname;
      };

    } else if (response.status === 200) {

      let claims = await response.json();

      let logoutUrlClaim = claims.find(claim => claim.type === 'bff:logout_url');
      if (logoutUrlClaim) {
        authorizeButton.onclick = function (event) {
          event.preventDefault();
          window.location.href = `${logoutUrlClaim.value}&returnUrl=${window.location.pathname}`;
        };
      }
      // User is authenticated
      authorizeButton.innerText = "Log out";
      authorizeButton.classList.add("btn-success"); // Optional: Change button style

    }
  } catch (error) {
    console.error("Error checking authentication:", error);
  }
}

const observer = new MutationObserver((mutations, obs) => {
  const wrapper = document.querySelector('.schemes.wrapper');
  if (wrapper) {
    checkAuthentication();
    obs.disconnect();
  }
});

observer.observe(document, {
  childList: true,
  subtree: true
});
