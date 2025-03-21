function addLoginButton() {
  const buttonContainer = document.querySelector(".topbar");
  if (!buttonContainer) {
  };

  // Create Login Button
  const loginButton = document.createElement("button");
  loginButton.textContent = "Login";
  loginButton.style.marginLeft = "10px";
  loginButton.style.padding = "8px 12px";
  loginButton.style.borderRadius = "5px";
  loginButton.style.border = "none";
  loginButton.style.background = "#007bff";
  loginButton.style.color = "white";
  loginButton.style.cursor = "pointer";

  loginButton.onclick = function () {
    window.location.href = "/bff/login?returnUrl=/swagger/index.html"; // Redirect to login endpoint
  };

  buttonContainer.appendChild(loginButton);
}

// Wait for Swagger UI to load and add the button
setTimeout(addLoginButton, 500);
