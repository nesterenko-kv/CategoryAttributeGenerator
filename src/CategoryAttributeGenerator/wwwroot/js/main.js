import { ConsoleLogger } from "./logger.js";
import { ApiClient } from "./apiClient.js";
import { CategoryAttributeClient } from "./categoryAttributeClient.js";

document.addEventListener("DOMContentLoaded", () => {
    try {
        const logger = new ConsoleLogger();
        const apiClient = new ApiClient({
            endpoint: "/api/category-attributes",
            logger
        });

        const client = new CategoryAttributeClient({
            endpoint: "/api/category-attributes",
            logger,
            apiClient
        });

        client.init();
    } catch (err) {
        console.error(err);

        const statusEl = document.getElementById("status");
        if (statusEl) {
            statusEl.textContent = "Client initialization error: " + err;
            statusEl.className = "status error";
        }
    }
});