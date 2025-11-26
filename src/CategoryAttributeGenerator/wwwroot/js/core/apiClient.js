import { ConsoleLogger } from "./logger.js";

/**
 * Minimal API client wrapper around fetch for JSON POST requests.
 */
export class ApiClient {
    /**
     * @param {Object} options
     * @param {string} options.endpoint - Full endpoint url or path.
     * @param {ConsoleLogger} options.logger
     */
    constructor({ endpoint, logger }) {
        this.endpoint = endpoint;
        this.logger = logger;
    }

    /**
     * Sends a JSON POST request to the configured endpoint.
     * @param {unknown} body - Request payload.
     * @param {string} traceId - Correlation id to be sent in headers.
     * @param {AbortSignal} abortSignal - AbortSignal object associated with request.
     * @returns {Promise<{ response: Response, text: string }>}
     */
    async postJson(body, traceId, abortSignal) {
        const headers = {
            "Content-Type": "application/json"
        };

        if (traceId) {
            headers["X-Trace-Id"] = traceId;
        }

        this.logger.debug("Sending POST request to API.", traceId, {
            endpoint: this.endpoint
        });

        const response = await fetch(this.endpoint, {
            method: "POST",
            headers,
            body: JSON.stringify(body),
            signal: abortSignal
        });

        const text = await response.text();

        this.logger.debug("Received response from API.", traceId, {
            status: response.status,
            ok: response.ok
        });

        return { response, text };
    }
}
