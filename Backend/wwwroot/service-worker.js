// BiDE Service Worker - enables PWA install + basic caching
const CACHE_NAME = 'bide-cache-v1';
const ASSETS = [
    '/',
    '/css/site.css',
    '/js/site.js',
    '/images/logo.png'
];

// Install - cache core assets
self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_NAME).then(function (cache) {
            return cache.addAll(ASSETS).catch(function () { /* ignore failures */ });
        })
    );
    self.skipWaiting();
});

// Activate - clean old caches
self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(
                keys.filter(function (k) { return k !== CACHE_NAME; })
                    .map(function (k) { return caches.delete(k); })
            );
        })
    );
    self.clients.claim();
});

// Fetch - network first, fall back to cache (so live data stays fresh)
self.addEventListener('fetch', function (event) {
    if (event.request.method !== 'GET') return;
    event.respondWith(
        fetch(event.request).catch(function () {
            return caches.match(event.request);
        })
    );
});
