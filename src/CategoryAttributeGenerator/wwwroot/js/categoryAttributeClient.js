
/**
 * Lightweight client for the Category Attribute Generator UI.
 * Encapsulates DOM access, state handling, and API calls.
 */
export class CategoryAttributeClient {
    /**
     * @param {Object} options
     * @param {string} options.endpoint - API endpoint URL
     * @param {ConsoleLogger} options.logger
     * @param {ApiClient} options.apiClient
     */
    constructor({ endpoint = "/api/category-attributes", logger, apiClient }) {
        this.endpoint = endpoint;
        this.logger = logger;
        this.apiClient = apiClient;

        this.inputEl = null;
        this.outputEl = null;
        this.statusEl = null;
        this.buttonEl = null;

        /** @type {AbortController | null} */
        this.currentAbortController = null;
    }

    /**
     * Initializes the client: resolves DOM elements, pre-fills sample payload,
     * and wires default event handlers.
     */
    init() {
        this._resolveElements();
        this._prefillSamplePayload();
        this._attachHandlers();
        this.logger.info("CategoryAttributeClient initialized.", undefined);
    }

    /**
     * Sets the current status message with a semantic type.
     * @param {"idle"|"success"|"error"} type
     * @param {string} message
     */
    setStatus(type, message) {
        if (!this.statusEl) return;

        this.statusEl.textContent = message;

        const baseClass = "status";
        switch (type) {
            case "success":
                this.statusEl.className = `${baseClass} success`;
                break;
            case "error":
                this.statusEl.className = `${baseClass} error`;
                break;
            default:
                this.statusEl.className = baseClass;
                break;
        }
    }

    _resolveElements() {
        const inputEl = document.getElementById("inputJson");
        const outputEl = document.getElementById("outputJson");
        const statusEl = document.getElementById("status");
        const buttonEl = document.getElementById("generateButton");

        if (!inputEl || !outputEl || !statusEl || !buttonEl) {
            throw new Error("One or more required DOM elements are missing.");
        }

        this.inputEl = inputEl;
        this.outputEl = outputEl;
        this.statusEl = statusEl;
        this.buttonEl = buttonEl;
    }

    _prefillSamplePayload() {
        const sampleInput = [
            {
                categoryName: "TVs",
                subCategories: [
                    { categoryId: 80, categoryName: "TVs" },
                    { categoryId: 948, categoryName: "All-Weather TVs" },
                    { categoryId: 37, categoryName: "TV Accessories" }
                ]
            }
        ];

        if (this.inputEl) {
            this.inputEl.value = JSON.stringify(sampleInput, null, 2);
        }
    }

    _attachHandlers() {
        if (!this.buttonEl) return;

        this.buttonEl.addEventListener("click", async () => {
            const traceId = this._createTraceId();
            this.logger.info("Generate button clicked.", traceId);

            const payload = this._parseInputJson(traceId);
            if (!payload) {
                return;
            }

            await this._callBackend(payload, traceId);
        });
    }

    _parseInputJson(traceId) {
        if (!this.inputEl) return null;

        const raw = this.inputEl.value.trim();
        if (!raw) {
            this.setStatus("error", "Please provide a non-empty JSON payload.");
            this.logger.warn("Empty payload provided.", traceId);
            return null;
        }

        try {
            return JSON.parse(raw);
        } catch (err) {
            this.setStatus("error", "Client-side JSON parsing failed: " + err);
            this.logger.error("Failed to parse JSON input.", traceId, err);
            return null;
        }
    }

    _setButtonBusy(isBusy) {
        if (!this.buttonEl) return;

        const baseText = this.buttonEl.dataset.baseText || this.buttonEl.textContent;
        this.buttonEl.dataset.baseText = baseText;

        if (isBusy) {
            this.buttonEl.disabled = true;
            this.buttonEl.textContent = baseText + " …";
        } else {
            this.buttonEl.disabled = false;
            this.buttonEl.textContent = baseText;
        }
    }

    _createTraceId() {
        if (window.crypto && typeof window.crypto.randomUUID === "function") {
            return window.crypto.randomUUID();
        }

        // Fallback: simple pseudo-random id
        return `client-${Date.now()}-${Math.floor(Math.random() * 1e9)}`;
    }

    async _callBackend(payload, traceId) {
        if (!this.outputEl || !this.buttonEl) return;
        // отменяем предыдущий запрос, если был
        if (this.currentAbortController) {
            this.logger.info("Aborting previous in-flight request.", traceId);
            this.currentAbortController.abort();
        }

        const abortController = new AbortController();
        this.currentAbortController = abortController;

        this._setButtonBusy(true);
        this.setStatus("idle", "Calling backend and OpenAI…");
        this.logger.info("Calling backend.", traceId);

        const start = performance.now();

        try {
            const { response, text } = await this.apiClient.postJson(
                payload,
                traceId,
                abortController.signal
            );

            let data;
            try {
                data = JSON.parse(text);
            } catch {
                this.outputEl.textContent = text;
                this.setStatus("error", "Backend returned a non-JSON response.");
                this.logger.error("Backend returned non-JSON response.", traceId, text);
                return;
            }

            if (!response.ok) {
                this.setStatus(
                    "error",
                    data.message || `Backend returned an error (HTTP ${response.status}).`
                );
                this.logger.warn("Backend returned an error.", traceId, {
                    status: response.status,
                    payload: data
                });
            } else {
                this.setStatus("success", "Successfully generated attributes.");
                this.logger.info("Successfully generated attributes.", traceId);
            }

            this.outputEl.textContent = JSON.stringify(data, null, 2);
        } catch (err) {
            if (err.name === "AbortError") {
                this.logger.info("Request was aborted.", traceId);
                // show nothing to user, the new request already started
            } else {
                this.setStatus("error", "Network error while calling backend: " + err);
                this.logger.error("Network error while calling backend.", traceId, err);
            }
        } finally {
            const elapsedMs = performance.now() - start;

            // сбрасываем только если этот контроллер ещё актуален
            if (this.currentAbortController === abortController) {
                this.currentAbortController = null;
                this.buttonEl.disabled = false;
                const baseText = this.buttonEl.dataset.baseText || "Generate Attributes";
                this.buttonEl.textContent = `${baseText} (${elapsedMs.toLocaleString()} ms)`;

                this.logger.info("Request completed.", traceId, {
                    elapsedMs
                });
            }
        }
    }
}
