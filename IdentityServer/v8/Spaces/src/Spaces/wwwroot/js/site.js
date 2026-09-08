// Spaces Demo — site.js
// Minimal vanilla JS. No frameworks required.
// Currently provides only cosmetic/accessibility helpers.

(function () {
    "use strict";

    // ── Copy-to-clipboard for raw token ──────────────────────────────────────
    // Wraps any element with class "token-raw-value" in a container that shows
    // a lightweight copy button. Gracefully skipped if the Clipboard API is
    // unavailable.
    function addCopyButtons() {
        if (!navigator.clipboard) return;

        document.querySelectorAll(".token-raw-value").forEach(function (el) {
            var wrap = el.closest(".token-raw-wrap");
            if (!wrap || wrap.dataset.copyAdded) return;
            wrap.dataset.copyAdded = "1";
            wrap.style.position = "relative";

            var btn = document.createElement("button");
            btn.textContent = "Copy";
            btn.className = "copy-btn";
            btn.style.cssText =
                "position:absolute;top:6px;right:6px;font-size:0.72rem;" +
                "padding:2px 8px;border:1px solid #d1d5db;border-radius:4px;" +
                "background:#fff;cursor:pointer;color:#374151;";

            btn.addEventListener("click", function () {
                navigator.clipboard.writeText(el.textContent).then(function () {
                    btn.textContent = "Copied!";
                    setTimeout(function () { btn.textContent = "Copy"; }, 1500);
                });
            });

            wrap.appendChild(btn);
        });
    }

    // Run after DOM is ready.
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", addCopyButtons);
    } else {
        addCopyButtons();
    }
})();
