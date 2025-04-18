"use strict";

/**
 * Performs automatic redirect back to the native client application after signin.
 */
(function () {
  var meta = document.querySelector("meta[http-equiv=refresh]");
  if (!meta) {
    console.error("Could not find meta tag for signin redirect.");
    return;
  }

  var url = meta.getAttribute("data-url");
  if (!url) {
    console.error("No signin redirect URL found in meta tag.");
    return;
  }

  // *** Protocol Validation (https://codeql.github.com/codeql-query-help/javascript/js-xss-through-dom/)  ***
  // Ensure the URL starts with http: or https: to prevent potential XSS via javascript: URIs
  // Convert to lowercase for case-insensitive comparison.
  var lowerUrl = url.toLowerCase();
  if (!lowerUrl.startsWith("http:") && !lowerUrl.startsWith("https:")) {
    console.error("Signin redirect URL has an invalid scheme:", url);
    return;
  }

  // If the URL is valid (for web), perform the redirect
  window.location.href = url;
})();
