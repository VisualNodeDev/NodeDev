"use strict";
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
exports.__esModule = true;
var jsx_runtime_1 = require("react/jsx-runtime");
var react_1 = require("react");
var reactflow_1 = require("reactflow");
exports["default"] = (0, react_1.memo)(function (_a) {
    var data = _a.data;
    function getConnection(inputOrOutput, type) {
        return (0, jsx_runtime_1.jsxs)("div", __assign({ className: 'nodeConnection_' + type }, { children: [(0, jsx_runtime_1.jsx)("div", __assign({ style: { paddingRight: 10, paddingLeft: 10 } }, { children: inputOrOutput.name })), (0, jsx_runtime_1.jsx)(reactflow_1.Handle, { type: type, position: type == 'source' ? reactflow_1.Position.Right : reactflow_1.Position.Left, id: inputOrOutput.id })] }));
    }
    return ((0, jsx_runtime_1.jsxs)(jsx_runtime_1.Fragment, { children: [(0, jsx_runtime_1.jsx)("div", { children: data.name }), data.inputs.map(function (x) { return getConnection(x, 'target'); }), data.outputs.map(function (x) { return getConnection(x, 'source'); })] }));
});
//# sourceMappingURL=NodeWithMultipleHandles.js.map