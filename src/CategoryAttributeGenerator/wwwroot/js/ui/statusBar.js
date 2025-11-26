export class StatusBar {
    /**
     * @param {HTMLElement} element
     */
    constructor(element) {
        this.el = element;
    }

    set(type, message) {
        if (!this.el) return;

        this.el.textContent = message;

        const base = "status";
        switch (type) {
            case "success":
                this.el.className = `${base} success`;
                break;
            case "error":
                this.el.className = `${base} error`;
                break;
            default:
                this.el.className = base;
                break;
        }
    }
}