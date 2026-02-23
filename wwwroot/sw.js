// RepEngine Service Worker — PWA offline support
const CACHE_NAME = 'repengine-v4';
const API_CACHE = 'repengine-api-v4';

const STATIC_ASSETS = [
    '/',
    '/Index',
    '/Dashboard',
    '/Governance',
    '/PostJob',
    '/Marketplace',
    '/Offline',
    '/css/site.css',
    '/js/site.js',
    '/js/wallet.js',
    '/js/pwa.js',
    '/js/ui.js',
    '/manifest.json',
    '/icons/icon-192x192.png',
    '/icons/icon-512x512.png',
    '/favicon.ico'
];

// ── Install: pre-cache static assets ──────────────────────
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

// ── Activate: clean old caches ────────────────────────────
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys
                    .filter(k => k !== CACHE_NAME && k !== API_CACHE)
                    .map(k => caches.delete(k))
            )
        ).then(() => self.clients.claim())
    );
});

// ── Fetch strategy ────────────────────────────────────────
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Skip non-GET (POST votes, applications, etc.)
    if (request.method !== 'GET') return;

    // API calls → network-first with cache fallback
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(networkFirstWithCache(request));
        return;
    }

    // Static assets → cache-first
    event.respondWith(cacheFirstWithNetwork(request));
});

async function cacheFirstWithNetwork(request) {
    const cached = await caches.match(request);
    if (cached) return cached;
    try {
        const response = await fetch(request);
        if (response.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, response.clone());
        }
        return response;
    } catch {
        // Offline fallback for pages
        if (request.mode === 'navigate') {
            return caches.match('/Offline') || new Response('Offline', { status: 503 });
        }
        return new Response('Offline', { status: 503 });
    }
}

async function networkFirstWithCache(request) {
    try {
        const response = await fetch(request);
        if (response.ok) {
            const cache = await caches.open(API_CACHE);
            cache.put(request, response.clone());
        }
        return response;
    } catch {
        const cached = await caches.match(request);
        return cached || new Response(JSON.stringify({ error: 'Offline' }), {
            status: 503,
            headers: { 'Content-Type': 'application/json' }
        });
    }
}

// ── Handle update messages ────────────────────────────────
self.addEventListener('message', event => {
    if (event.data?.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
