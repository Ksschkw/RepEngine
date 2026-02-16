// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Mobile Bottom Navigation Active State
document.addEventListener('DOMContentLoaded', function () {
    // Set active nav item based on current page
    const currentPath = window.location.pathname;
    const navItems = document.querySelectorAll('.bottom-nav .nav-item');

    navItems.forEach(item => {
        const href = item.getAttribute('href');
        if (href && currentPath.includes(href.replace('/Index', ''))) {
            item.classList.add('active');
        }
    });

    // Add haptic feedback (visual) on tap
    navItems.forEach(item => {
        item.addEventListener('touchstart', function () {
            this.style.transform = 'scale(0.95)';
        });

        item.addEventListener('touchend', function () {
            this.style.transform = 'scale(1)';
        });
    });

    // Wallet connect button mobile optimization
    const walletBtn = document.getElementById('walletConnectBtn');
    if (walletBtn) {
        walletBtn.addEventListener('click', function () {
            // Add visual feedback
            this.style.transform = 'scale(0.95)';
            setTimeout(() => {
                this.style.transform = 'scale(1)';
            }, 100);
        });
    }
});

// Write your JavaScript code.
