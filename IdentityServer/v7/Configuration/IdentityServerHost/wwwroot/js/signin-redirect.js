"use strict";

(function () {
  var meta = document.querySelector("meta[http-equiv=refresh]");
  if (!meta) {
    console.error("Could not find meta tag for signin redirect.");
    return;
  }

  var redirectUrl = meta.getAttribute("data-url");
  if (!redirectUrl || !redirectUrl.trim()) {
    console.error("No signin redirect URL found in meta tag.");
    return;
  }

  var url;
  try {
    url = new URL(redirectUrl, window.location.href);
  } catch (error) {
    console.error("Invalid signin redirect URL.", error);
    return;
  }

  // Native clients may use custom schemes, so block dangerous schemes rather than allow only HTTP(S).
  if (url.protocol === "javascript:" || url.protocol === "data:" || url.protocol === "vbscript:") {
    console.error("Signin redirect URL has an unsafe scheme.");
    return;
  }

  window.location.href = url.href;
})();
