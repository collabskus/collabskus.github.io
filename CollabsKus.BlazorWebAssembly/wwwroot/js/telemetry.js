const sdkUrl = "https://js.monitor.azure.com/scripts/b/ai.3.gbl.min.js";
const cloudRole = "collabskus-web";
const maxQueueLength = 250;

const State = Object.freeze({ Idle: "idle", Loading: "loading", Ready: "ready", Disabled: "disabled" });

let state = State.Idle;
let sdk = null;
let queue = [];

function loadScript(src) {
    return new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = src;
        script.async = true;
        script.crossOrigin = "anonymous";
        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load ${src}`));
        document.head.appendChild(script);
    });
}

function disable(reason) {
    state = State.Disabled;
    queue = [];
    console.warn(`Telemetry disabled: ${reason}`);
}

function execute(action) {
    try {
        action(sdk);
    } catch (error) {
        console.warn("Telemetry call failed", error);
    }
}

function enqueue(action) {
    if (state === State.Ready) {
        execute(action);
    } else if (state !== State.Disabled && queue.length < maxQueueLength) {
        queue.push(action);
    }
}

async function start(connectionString, environment) {
    try {
        await loadScript(sdkUrl);
        const ns = window.Microsoft && window.Microsoft.ApplicationInsights;
        if (!ns || typeof ns.ApplicationInsights !== "function") {
            throw new Error("Application Insights SDK namespace not found");
        }
        sdk = new ns.ApplicationInsights({
            config: {
                connectionString: connectionString,
                enableAutoRouteTracking: false
            }
        });
        sdk.addTelemetryInitializer(envelope => {
            envelope.tags = envelope.tags || {};
            envelope.tags["ai.cloud.role"] = cloudRole;
            envelope.data = envelope.data || {};
            envelope.data.environment = environment;
        });
        sdk.loadAppInsights();
        sdk.trackPageView();
        state = State.Ready;
        const pending = queue;
        queue = [];
        pending.forEach(execute);
    } catch (error) {
        disable(error.message);
    }
}

export function init(connectionString, environment) {
    if (state !== State.Idle) {
        return;
    }
    if (!connectionString) {
        disable("no connection string configured");
        return;
    }
    state = State.Loading;
    start(connectionString, environment || "Production");
}

export function trackPageView(name, uri) {
    enqueue(s => {
        const pageView = {};
        if (name) pageView.name = name;
        if (uri) pageView.uri = uri;
        s.trackPageView(pageView);
    });
}

export function trackEvent(name, properties) {
    enqueue(s => s.trackEvent({ name: name }, properties || undefined));
}

export function trackException(message, stack, typeName, properties) {
    enqueue(s => {
        const error = new Error(message || "Unknown error");
        if (typeName) error.name = typeName;
        if (stack) error.stack = stack;
        s.trackException({ exception: error, severityLevel: 3 }, properties || undefined);
    });
}

export function trackTrace(message, severityLevel, properties) {
    enqueue(s => s.trackTrace({ message: message, severityLevel: severityLevel }, properties || undefined));
}

export function flush() {
    enqueue(s => s.flush());
}
