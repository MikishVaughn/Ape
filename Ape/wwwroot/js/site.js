// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ========================================
// Top Button (Scroll to Top) Functionality
// ========================================
// To use: Just add the HTML button to your page:
// <button class="btn" id="top-button" title="Go to top" style="display:none">⬆️</button>
// ========================================

document.addEventListener('DOMContentLoaded', function() {
    // Get the button
    var topButton = document.getElementById("top-button");

    // Only initialize if button exists on the page
    if (topButton) {
        // When the user scrolls down 320px from the top, show the button
        window.addEventListener('scroll', function() {
            if (document.body.scrollTop > 320 || document.documentElement.scrollTop > 320) {
                topButton.style.display = "flex";
            } else {
                topButton.style.display = "none";
            }
        });

        // When the user clicks on the button, scroll to the top
        topButton.addEventListener('click', function() {
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });

            // Fallback for older browsers
            document.body.scrollTop = 0;
            document.documentElement.scrollTop = 0;
        });
    }
});
