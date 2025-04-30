async function checkAuthentication() {
  try {
    // check if we already added it
    let authorizeButton = document.querySelector('.swagger-ui .auth-wrapper .btn');
    if (authorizeButton === null) {

      let response = await fetch('/bff/user',
        {
          credentials: 'include',
          headers: {'x-csrf': '1'}
        });

      let wrapper = document.querySelector('.schemes.wrapper');
      wrapper.insertAdjacentHTML('beforeend', `<div class='auth-wrapper'><button class='btn authorize unlocked btn-success'></button></div>`);
      authorizeButton = document.querySelector('.swagger-ui .auth-wrapper .btn');

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
        authorizeButton.classList.add("btn-success");
      }
    }
  } catch
    (error) {
    console.error("Error checking authentication:", error);
  }
}

document.addEventListener("DOMContentLoaded", e => {
  // watch the only element that's on the page when the page loads
  const swaggerUi = document.querySelector('#swagger-ui');

  const observer = new MutationObserver(async (mutations, obs) => {
    let schemeContainer = document.querySelector('.scheme-container');
    const intervalId = setInterval(() => {
      schemeContainer = document.querySelector('.scheme-container');
      if (schemeContainer) {
        clearInterval(intervalId);
        checkAuthentication();
      }
    }, 100);
  });

  observer.observe(swaggerUi, {
    childList: true,
    subtree: true
  });
});




