/**
 * Handles showing output JSON and copying it to clipboard.
 */
export class OutputPanel {
    /**
     * @param {HTMLElement} element
     * @param {import("../core/logger.js").ConsoleLogger} logger
     * @param {StatusBar} statusBar
     */
    constructor(element, logger, statusBar) {
        this.el = element;
        this.logger = logger;
        this.statusBar = statusBar;
    }

    setJson(data) {
        this.el.textContent = JSON.stringify(data, null, 2);
    }

    setRaw(text) {
        this.el.textContent = text ?? "";
    }
    
    async copyToClipboard(traceId) {
        if (!this.el) return;

        const text = this.el.textContent || "";
        if (!text.trim()) {
            this.statusBar.set("error", "Nothing to copy: output is empty.");
            this.logger.warn("Copy requested with empty output.", traceId);
            return;
        }

        if (!navigator.clipboard || !navigator.clipboard.writeText) {
            this.statusBar.set(
                "error",
                "Clipboard API is not available in this browser or context."
            );
            this.logger.warn("Clipboard API not available.", traceId);
            return;
        }

        try {
            await navigator.clipboard.writeText(text);
            this.statusBar.set("success", "Output JSON copied to clipboard.");
            this.logger.info("Output JSON copied to clipboard.", traceId);
        } catch (err) {
            this.statusBar.set("error", "Failed to copy output to clipboard.");
            this.logger.error("Failed to copy output JSON.", traceId, err);
        }
    }
}