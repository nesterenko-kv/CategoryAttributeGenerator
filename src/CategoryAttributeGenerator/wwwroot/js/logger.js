/**
 * Very small console-based logger with optional trace id.
 */
export class ConsoleLogger {
    /**
     * @param {"debug"|"info"|"warn"|"error"} level
     * @param {string} message
     * @param {string | undefined} traceId
     * @param {unknown} [details]
     */
    log(level, message, traceId, details) {
        const prefixParts = [];
        if (traceId) prefixParts.push(`[traceId: ${traceId}]`);
        prefixParts.push(`[${level.toUpperCase()}]`);
        const prefix = prefixParts.join(" ");

        // eslint-disable-next-line no-console
        const target = console[level] || console.log;
        if (details !== undefined) {
            target(prefix + " " + message, details);
        } else {
            target(prefix + " " + message);
        }
    }

    debug(message, traceId, details) {
        this.log("debug", message, traceId, details);
    }

    info(message, traceId, details) {
        this.log("info", message, traceId, details);
    }

    warn(message, traceId, details) {
        this.log("warn", message, traceId, details);
    }

    error(message, traceId, details) {
        this.log("error", message, traceId, details);
    }
}
