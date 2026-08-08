// Unwraps the plain-text error body backend controllers return on a failed request
// (e.g. BadRequestObjectResult(ex.Message)) so it can be shown to the user directly,
// instead of a generic guess. Falls back when the response has no such body.
export function extractErrorMessage(error: unknown, fallback: string): string {
    if (typeof error === "object" && error !== null && "response" in error) {
        const data = (error as { response?: { data?: unknown } }).response?.data;
        if (typeof data === "string" && data.length > 0) {
            return data;
        }
    }
    return fallback;
}
