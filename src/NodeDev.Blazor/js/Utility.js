"use strict";
exports.__esModule = true;
exports.findNode = exports.limitFunctionCall = void 0;
function limitFunctionCall(timeoutId, fn, limit) {
    clearTimeout(timeoutId);
    return setTimeout(fn, limit);
}
exports.limitFunctionCall = limitFunctionCall;
function findNode(nodes, id) {
    for (var i = 0; i < nodes.length; i++)
        if (nodes[i].id == id)
            return nodes[i];
    return null;
}
exports.findNode = findNode;
//# sourceMappingURL=Utility.js.map