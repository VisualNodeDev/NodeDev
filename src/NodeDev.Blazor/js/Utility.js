"use strict";
exports.__esModule = true;
exports.limitFunctionCall = void 0;
function limitFunctionCall(timeoutId, fn, limit) {
    clearTimeout(timeoutId);
    return setTimeout(fn, limit);
}
exports.limitFunctionCall = limitFunctionCall;
//# sourceMappingURL=Utility.js.map