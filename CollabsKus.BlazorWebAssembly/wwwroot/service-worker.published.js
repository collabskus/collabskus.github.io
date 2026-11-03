self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.svg$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/, /\.md$/];
const offlineAssetsExclude = [/^service-worker\.js$/];
const unverifiedAssets = [/\.html$/];

const baseUrl = new URL(self.registration.scope);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);
const indexUrl = new URL('index.html', baseUrl).href;

async function onInstall(event) {
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(new URL(asset.url, baseUrl).href, {
            integrity: unverifiedAssets.some(pattern => pattern.test(asset.url)) ? '' : asset.hash,
            cache: 'no-cache'
        }));
    const cache = await caches.open(cacheName);
    await cache.addAll(assetsRequests);
}

async function onActivate(event) {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
    await self.clients.claim();
}

async function onFetch(event) {
    if (event.request.method !== 'GET') {
        return fetch(event.request);
    }

    const shouldServeIndexHtml = event.request.mode === 'navigate'
        && !manifestUrlList.some(url => url === event.request.url);

    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(shouldServeIndexHtml ? indexUrl : event.request);
    if (cachedResponse) {
        return cachedResponse;
    }

    return fetch(event.request);
}
