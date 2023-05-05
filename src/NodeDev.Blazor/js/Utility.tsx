
export function limitFunctionCall(timeoutId: number, fn: () => void, limit: number) {
    clearTimeout(timeoutId);
    return setTimeout(fn, limit);
}