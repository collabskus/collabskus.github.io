(function () {
    'use strict';

    function notify(dotNetRef, method, value) {
        if (!dotNetRef) {
            return;
        }
        dotNetRef.invokeMethodAsync(method, value).catch(function () { });
    }

    window.pageMonitor = {
        init: function (dotNetRef) {
            var observers = {};
            var ref = dotNetRef;

            var onVisibility = function () { notify(ref, 'OnVisibilityChanged', !document.hidden); };
            var onOnline = function () { notify(ref, 'OnConnectivityChanged', true); };
            var onOffline = function () { notify(ref, 'OnConnectivityChanged', false); };

            document.addEventListener('visibilitychange', onVisibility);
            window.addEventListener('online', onOnline);
            window.addEventListener('offline', onOffline);

            return {
                isOnline: function () {
                    return navigator.onLine !== false;
                },

                observe: function (elementId, callbackName) {
                    var target = document.getElementById(elementId);
                    if (!target || !('IntersectionObserver' in window)) {
                        return;
                    }
                    if (observers[elementId]) {
                        observers[elementId].disconnect();
                    }
                    var observer = new IntersectionObserver(function (entries) {
                        notify(ref, callbackName, entries[0].isIntersecting);
                    }, { threshold: 0.0 });
                    observer.observe(target);
                    observers[elementId] = observer;
                },

                dispose: function () {
                    document.removeEventListener('visibilitychange', onVisibility);
                    window.removeEventListener('online', onOnline);
                    window.removeEventListener('offline', onOffline);
                    Object.keys(observers).forEach(function (id) {
                        observers[id].disconnect();
                    });
                    observers = {};
                    ref = null;
                }
            };
        }
    };

    window.appInterop = {
        getLocation: function (options) {
            return new Promise(function (resolve) {
                if (!navigator.geolocation) {
                    resolve(null);
                    return;
                }
                navigator.geolocation.getCurrentPosition(
                    function (pos) {
                        var timeZone = '';
                        try {
                            timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || '';
                        } catch (e) {
                            timeZone = '';
                        }
                        resolve({
                            latitude: pos.coords.latitude,
                            longitude: pos.coords.longitude,
                            offsetMinutes: -new Date().getTimezoneOffset(),
                            timeZone: timeZone
                        });
                    },
                    function () {
                        resolve(null);
                    },
                    options || undefined
                );
            });
        }
    };

    window.appStorage = {
        get: function (key) {
            try {
                return window.localStorage.getItem(key);
            } catch (e) {
                return null;
            }
        },

        set: function (key, value) {
            try {
                window.localStorage.setItem(key, value);
                return true;
            } catch (e) {
                return false;
            }
        },

        remove: function (key) {
            try {
                window.localStorage.removeItem(key);
            } catch (e) {
            }
        }
    };

    function lerpHex(c1, c2, t) {
        var r1 = parseInt(c1.slice(1, 3), 16), g1 = parseInt(c1.slice(3, 5), 16), b1 = parseInt(c1.slice(5, 7), 16);
        var r2 = parseInt(c2.slice(1, 3), 16), g2 = parseInt(c2.slice(3, 5), 16), b2 = parseInt(c2.slice(5, 7), 16);
        var r = Math.round(r1 + (r2 - r1) * t), g = Math.round(g1 + (g2 - g1) * t), b = Math.round(b1 + (b2 - b1) * t);
        return 'rgb(' + r + ',' + g + ',' + b + ')';
    }

    window.sunTracker = {
        _canvases: {},

        init: function (canvasId) {
            if (this._canvases[canvasId]) {
                window.removeEventListener('resize', this._canvases[canvasId].resize);
                delete this._canvases[canvasId];
            }

            var canvas = document.getElementById(canvasId);
            if (!canvas) return;
            var resize = function () {
                canvas.width = canvas.offsetWidth;
                canvas.height = canvas.offsetHeight || 300;
            };
            resize();
            window.addEventListener('resize', resize);
            this._canvases[canvasId] = { canvas: canvas, resize: resize };
        },

        draw: function (canvasId, altitude, azimuth, dayFraction, isAboveHorizon, fullDayFraction) {
            var entry = this._canvases[canvasId];
            if (!entry) return;
            var canvas = entry.canvas;
            var ctx = canvas.getContext('2d');
            var W = canvas.width, H = canvas.height;
            var cx = W / 2;
            var horizY = H * 0.55;
            var arcRx = W * 0.44;
            var arcRyDay = horizY - 15;
            var arcRyNight = (H - horizY) * 0.6;

            ctx.clearRect(0, 0, W, H);

            var skyTop, skyBot;
            if (altitude > 15) { skyTop = '#061228'; skyBot = '#0d2040'; }
            else if (altitude > 0) { var t = altitude / 15; skyTop = lerpHex('#0f2050', '#061228', t); skyBot = lerpHex('#3a1a08', '#0d2040', t); }
            else if (altitude > -6) { var t2 = (altitude + 6) / 6; skyTop = lerpHex('#0a0e18', '#0f2050', t2); skyBot = lerpHex('#1a0e04', '#3a1a08', t2); }
            else { skyTop = '#04080f'; skyBot = '#080c16'; }

            var skyGrad = ctx.createLinearGradient(0, 0, 0, horizY);
            skyGrad.addColorStop(0, skyTop); skyGrad.addColorStop(1, skyBot);
            ctx.fillStyle = skyGrad;
            ctx.fillRect(0, 0, W, horizY);

            var gnd = ctx.createLinearGradient(0, horizY, 0, H);
            gnd.addColorStop(0, '#0e180e'); gnd.addColorStop(1, '#060c06');
            ctx.fillStyle = gnd;
            ctx.fillRect(0, horizY, W, H - horizY);

            if (altitude > -8 && altitude < 20) {
                var tg = Math.max(0, 1 - Math.abs(altitude) / 20);
                var glow = ctx.createRadialGradient(cx, horizY, 0, cx, horizY, W * 0.5);
                glow.addColorStop(0, 'rgba(255,120,40,' + (0.38 * tg) + ')');
                glow.addColorStop(0.4, 'rgba(255,80,20,' + (0.15 * tg) + ')');
                glow.addColorStop(1, 'rgba(0,0,0,0)');
                ctx.fillStyle = glow;
                ctx.fillRect(0, horizY - H * 0.3, W, H * 0.4);
            }

            ctx.beginPath(); ctx.moveTo(0, horizY); ctx.lineTo(W, horizY);
            ctx.strokeStyle = 'rgba(240,165,0,0.22)'; ctx.lineWidth = 1; ctx.stroke();

            ctx.beginPath();
            ctx.ellipse(cx, horizY, arcRx, arcRyDay, 0, Math.PI, 0, false);
            ctx.strokeStyle = 'rgba(240,165,0,0.10)';
            ctx.lineWidth = 1;
            ctx.setLineDash([4, 7]);
            ctx.stroke();
            ctx.setLineDash([]);

            ctx.beginPath();
            ctx.ellipse(cx, horizY, arcRx, arcRyNight, 0, 0, Math.PI, false);
            ctx.strokeStyle = 'rgba(100,140,220,0.06)';
            ctx.lineWidth = 1;
            ctx.setLineDash([3, 9]);
            ctx.stroke();
            ctx.setLineDash([]);

            var fdf = (fullDayFraction !== undefined && fullDayFraction !== null) ? fullDayFraction : 0.5;
            var theta = 1.5 * Math.PI - fdf * 2 * Math.PI;
            var sinT = Math.sin(theta);
            var cosT = Math.cos(theta);
            var sunX = cx + arcRx * cosT;
            var sunY;
            if (sinT >= 0) {
                sunY = horizY - arcRyDay * sinT;
            } else {
                sunY = horizY - arcRyNight * sinT;
            }

            if (isAboveHorizon) {
                var corona = ctx.createRadialGradient(sunX, sunY, 2, sunX, sunY, 42);
                corona.addColorStop(0, 'rgba(255,220,80,0.92)');
                corona.addColorStop(0.2, 'rgba(255,180,40,0.5)');
                corona.addColorStop(0.6, 'rgba(255,140,20,0.14)');
                corona.addColorStop(1, 'rgba(0,0,0,0)');
                ctx.fillStyle = corona;
                ctx.beginPath(); ctx.arc(sunX, sunY, 42, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath(); ctx.arc(sunX, sunY, 11, 0, Math.PI * 2);
                ctx.fillStyle = '#ffe060'; ctx.fill();
            } else {
                var nightGlow = ctx.createRadialGradient(sunX, sunY, 1, sunX, sunY, 20);
                nightGlow.addColorStop(0, 'rgba(255,160,60,0.20)');
                nightGlow.addColorStop(1, 'rgba(0,0,0,0)');
                ctx.fillStyle = nightGlow;
                ctx.beginPath(); ctx.arc(sunX, sunY, 20, 0, Math.PI * 2); ctx.fill();
                ctx.beginPath(); ctx.arc(sunX, sunY, 6, 0, Math.PI * 2);
                ctx.fillStyle = 'rgba(255,160,60,0.35)'; ctx.fill();
            }

            ctx.font = '10px "Courier New", monospace';
            ctx.fillStyle = 'rgba(240,165,0,0.55)';
            ctx.textAlign = 'left';
            ctx.fillText('ALT ' + (altitude >= 0 ? '+' : '') + altitude.toFixed(1) + '\u00B0', 12, H - 10);
            ctx.textAlign = 'right';
            ctx.fillText('AZ ' + azimuth.toFixed(1) + '\u00B0', W - 12, H - 10);
            ctx.fillStyle = 'rgba(255,255,255,0.18)';
            ctx.textAlign = 'left'; ctx.fillText('E', 12, horizY - 5);
            ctx.textAlign = 'right'; ctx.fillText('W', W - 12, horizY - 5);
            ctx.textAlign = 'center'; ctx.fillText('S', cx, horizY - 5);
        },

        dispose: function (canvasId) {
            var entry = this._canvases[canvasId];
            if (!entry) return;
            window.removeEventListener('resize', entry.resize);
            delete this._canvases[canvasId];
        }
    };
}());
