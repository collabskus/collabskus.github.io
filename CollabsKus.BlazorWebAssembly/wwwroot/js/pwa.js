(function () {
    'use strict';

    if (!('serviceWorker' in navigator)) {
        return;
    }

    var hadController = !!navigator.serviceWorker.controller;
    var reloading = false;

    function showUpdatePrompt(worker) {
        var banner = document.getElementById('app-update');
        var button = document.getElementById('app-update-reload');
        if (!banner || !button) {
            return;
        }
        button.onclick = function () {
            button.disabled = true;
            worker.postMessage({ type: 'SKIP_WAITING' });
        };
        banner.hidden = false;
    }

    function trackInstalling(registration) {
        var installing = registration.installing;
        if (!installing) {
            return;
        }
        installing.addEventListener('statechange', function () {
            if (installing.state === 'installed' && navigator.serviceWorker.controller) {
                showUpdatePrompt(installing);
            }
        });
    }

    navigator.serviceWorker.addEventListener('controllerchange', function () {
        if (!hadController || reloading) {
            return;
        }
        reloading = true;
        window.location.reload();
    });

    window.addEventListener('load', function () {
        navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' })
            .then(function (registration) {
                if (registration.waiting && navigator.serviceWorker.controller) {
                    showUpdatePrompt(registration.waiting);
                }
                registration.addEventListener('updatefound', function () {
                    trackInstalling(registration);
                });
                document.addEventListener('visibilitychange', function () {
                    if (!document.hidden && navigator.onLine !== false) {
                        registration.update().catch(function () { });
                    }
                });
            })
            .catch(function (error) {
                console.warn('Service worker registration failed', error);
            });
    });
}());
