import { createTraceId } from "../core/trace.js";
import { StatusBar } from "./statusBar.js";
import { InputEditor } from "./inputEditor.js";
import { OutputPanel } from "./outputPanel.js";

/**
 * Orchestrates all pieces on the Category Attribute Generator page.
 */
export class CategoryAttributePage {
    /**
     * @param {Object} options
     * @param {import("../core/logger.js").ConsoleLogger} options.logger
     * @param {import("../core/apiClient.js").ApiClient} options.apiClient
     */
    constructor({ logger, apiClient }) {
        this.logger = logger;
        this.apiClient = apiClient;

        this.statusBar = null;
        this.inputEditor = null;
        this.outputPanel = null;
        this.generateButton = null;
        this.formatButton = null;
        this.copyButton = null;

        /** @type {AbortController | null} */
        this.currentAbortController = null;
    }

    init() {
        const inputEl = /** @type {HTMLTextAreaElement|null} */ (document.getElementById("inputJson"));
        const outputEl = document.getElementById("outputJson");
        const statusEl = document.getElementById("status");
        const generateButton = /** @type {HTMLButtonElement|null} */ (document.getElementById("generateButton"));
        const formatButton = /** @type {HTMLButtonElement|null} */ (document.getElementById("formatButton"));
        const copyButton = /** @type {HTMLButtonElement|null} */ (document.getElementById("copyOutputButton"));

        if (!inputEl || !outputEl || !statusEl || !generateButton) {
            throw new Error("One or more required DOM elements are missing.");
        }

        this.statusBar = new StatusBar(statusEl);
        this.inputEditor = new InputEditor(inputEl, this.logger, this.statusBar);
        this.outputPanel = new OutputPanel(outputEl, this.logger, this.statusBar);
        this.generateButton = generateButton;
        this.formatButton = formatButton;
        this.copyButton = copyButton;

        this._initInputAndHandlers();
        this.logger.info("CategoryAttributePage initialized.", undefined);
    }

    _initInputAndHandlers() {
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

        this.inputEditor.initWithSample(sampleInput);
        this.inputEditor.bindPersistence();

        this.generateButton.addEventListener("click", () => {
            const traceId = createTraceId();
            this.logger.info("Generate button clicked.", traceId);

            const payload = this.inputEditor.getParsedOrShowError(traceId);
            if (!payload) return;

            this._callBackend(payload, traceId);
        });

        if (this.formatButton) {
            this.formatButton.addEventListener("click", () => {
                const traceId = createTraceId();
                this.inputEditor.formatJson(traceId);
            });
        }

        if (this.copyButton) {
            this.copyButton.addEventListener("click", async () => {
                const traceId = createTraceId();
                await this.outputPanel.copyToClipboard(traceId);
            });
        }
    }

    async _callBackend(payload, traceId) {
        // Cancel previous in-flight request
        if (this.currentAbortController) {
            this.logger.info("Aborting previous in-flight request.", traceId);
            this.currentAbortController.abort();
        }

        const abortController = new AbortController();
        this.currentAbortController = abortController;

        this._setButtonBusy(true);
        this.statusBar.set("idle", "Calling backend and OpenAI…");
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
                this.outputPanel.setRaw(text);
                this.statusBar.set("error", "Backend returned a non-JSON response.");
                this.logger.error("Backend returned non-JSON response.", traceId, text);
                return;
            }

            if (!response.ok) {
                this.statusBar.set(
                    "error",
                    data.message || `Backend returned an error (HTTP ${response.status}).`
                );
                this.logger.warn("Backend returned an error.", traceId, {
                    status: response.status,
                    payload: data
                });
            } else {
                this.statusBar.set("success", "Successfully generated attributes.");
                this.logger.info("Successfully generated attributes.", traceId);
            }

            this.outputPanel.setJson(data);
        } catch (err) {
            if (err.name === "AbortError") {
                this.logger.info("Request was aborted.", traceId);
            } else {
                this.statusBar.set("error", "Network error while calling backend: " + err);
                this.logger.error("Network error while calling backend.", traceId, err);
            }
        } finally {
            const elapsedMs = performance.now() - start;

            if (this.currentAbortController === abortController) {
                this.currentAbortController = null;
                this._setButtonBusy(false);

                const baseText = this.generateButton.dataset.baseText || "Generate Attributes";
                this.generateButton.textContent = `${baseText} (${elapsedMs.toLocaleString()} ms)`;

                this.logger.info("Request completed.", traceId, {
                    elapsedMs
                });
            }
        }
    }

    _setButtonBusy(isBusy) {
        const btn = this.generateButton;
        if (!btn) return;

        const baseText = btn.dataset.baseText || btn.textContent;
        btn.dataset.baseText = baseText;

        btn.disabled = isBusy;

        if (isBusy) {
            btn.textContent = baseText + " …";
            btn.classList.add("button-loading");
        } else {
            btn.classList.remove("button-loading");
            btn.textContent = baseText;
        }
    }
}