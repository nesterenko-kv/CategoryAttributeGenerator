const STORAGE_KEY_INPUT_JSON = "categoryAttributeGenerator.inputJson";

export function loadInputJson() {
    try {
        const value = window.localStorage.getItem(STORAGE_KEY_INPUT_JSON);
        return value ?? "";
    } catch (err) {
        this.logger.warn("Failed to load input JSON.", err);
        return "";
    }
}

export function saveInputJson(value) {
    try {
        window.localStorage.setItem(STORAGE_KEY_INPUT_JSON, value ?? "");
    } catch (err) {
        this.logger.warn("Failed to save input JSON.", err);
    }
}