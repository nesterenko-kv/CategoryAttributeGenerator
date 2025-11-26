export function createTraceId() {
    if (window.crypto && typeof window.crypto.randomUUID === "function") {
        return window.crypto.randomUUID();
    }

    return `client-${Date.now()}-${Math.floor(Math.random() * 1e9)}`;
}