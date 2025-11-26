import { loadInputJson, saveInputJson } from "../core/storage.js";

/**
 * Manages the input textarea: loading, formatting and persistence,
 * plus JSON syntax highlight via Prism.js.
 */
export class InputEditor {
    /**
     * @param {HTMLTextAreaElement} textareaEl
     * @param {HTMLElement} highlightCodeEl  // <code id="inputHighlight">
     * @param {import("../core/logger.js").ConsoleLogger} logger
     * @param {import("./statusBar.js").StatusBar} statusBar
     */
    constructor(textareaEl, highlightCodeEl, logger, statusBar) {
        this.el = textareaEl;
        this.highlightEl = highlightCodeEl;
        this.logger = logger;
        this.statusBar = statusBar;
    }

    initWithSample(samplePayload) {
        const stored = loadInputJson();
        if (stored && stored.trim().length > 0) {
            this.el.value = stored;
            this.logger.info("Restored input JSON from localStorage.", undefined);
        } else {
            this.el.value = JSON.stringify(samplePayload, null, 2);
        }

        this._updateHighlight(this.el.value);
    }

    bindPersistence() {
        this.el.addEventListener("input", () => {
            const value = this.el.value;
            saveInputJson(value);
            this._updateHighlight(value);
        });

        this.el.addEventListener("scroll", () => {
            if (!this.highlightEl) return;
            const container = this.highlightEl.parentElement;
            if (!container) return;

            container.scrollTop = this.el.scrollTop;
            container.scrollLeft = this.el.scrollLeft;
        });

        // на всякий случай начальная подсветка
        this._updateHighlight(this.el.value);
    }

    getParsedOrShowError(traceId) {
        const raw = this.el.value.trim();
        if (!raw) {
            this.statusBar.set("error", "Please provide a non-empty JSON payload.");
            this.logger.warn("Empty payload provided.", traceId);
            return null;
        }

        try {
            return JSON.parse(raw);
        } catch (err) {
            this.statusBar.set("error", "Client-side JSON parsing failed: " + err);
            this.logger.error("Failed to parse JSON input.", traceId, err);
            return null;
        }
    }

    formatJson(traceId) {
        const raw = this.el.value.trim();
        if (!raw) {
            this.statusBar.set("error", "Nothing to format: input is empty.");
            this.logger.warn("Format requested on empty input.", traceId);
            return;
        }

        try {
            const parsed = JSON.parse(raw);
            const formatted = JSON.stringify(parsed, null, 2);
            this.el.value = formatted;
            saveInputJson(formatted);
            this._updateHighlight(formatted);

            this.statusBar.set("success", "Input JSON formatted.");
            this.logger.info("Input JSON formatted.", traceId);
        } catch (err) {
            this.statusBar.set("error", "Cannot format: input is not valid JSON.");
            this.logger.error("Failed to format input JSON.", traceId, err);
        }
    }

    _updateHighlight(text) {
        if (!this.highlightEl) return;

        if (!window.Prism || !Prism.languages || !Prism.languages.json) {
            this.highlightEl.textContent = text;
            return;
        }

        this.highlightEl.innerHTML = Prism.highlight(text, Prism.languages.json, "json");
    }
}