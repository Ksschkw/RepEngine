// site.js — RepEngine UI logic

document.addEventListener('DOMContentLoaded', () => {
    highlightActiveNav();
});

/* ── Bottom Nav + Desktop Nav: highlight active page ───── */
function highlightActiveNav() {
    const path = window.location.pathname.toLowerCase().replace(/\/$/, '') || '/';

    // Mobile bottom nav
    document.querySelectorAll('.bnav-item').forEach(item => {
        const href = (item.getAttribute('href') || '').toLowerCase().replace(/\/$/, '') || '/';
        if (path === href || (href !== '/' && path.startsWith(href))) {
            item.classList.add('active');
        }
    });

    // Desktop nav
    document.querySelectorAll('.dnav-link').forEach(link => {
        const href = (link.getAttribute('href') || link.pathname || '').toLowerCase().replace(/\/$/, '') || '/';
        // asp-page generates href, but we can also check pathname
        const linkPath = new URL(link.href, location.origin).pathname.toLowerCase().replace(/\/$/, '') || '/';
        if (path === linkPath || (linkPath !== '/' && path.startsWith(linkPath))) {
            link.classList.add('active');
        }
    });
}
