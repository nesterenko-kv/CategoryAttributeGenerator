import { ConsoleLogger } from "./core/logger.js";
import { ApiClient } from "./core/apiClient.js";
import { CategoryAttributePage } from "./ui/categoryAttributePage.js";

document.addEventListener("DOMContentLoaded", () => {
    try {
        const logger = new ConsoleLogger();
        const apiClient = new ApiClient({
            endpoint: "/api/category-attributes",
            logger
        });

        const page = new CategoryAttributePage({
            logger,
            apiClient
        });

        page.init();
    } catch (err) {
        // eslint-disable-next-line no-console
        console.error(err);

        const statusEl = document.getElementById("status");
        if (statusEl) {
            statusEl.textContent = "Client initialization error: " + err;
            statusEl.className = "status error";
        }
    }
});