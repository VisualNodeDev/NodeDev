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
    var id = _a.id, data = _a.data;
    var linkIcon = '<path d=\"M0 0h24v24H0z\" fill=\"none\"/><path d=\"M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z\"/>';
    function getConnection(inputOrOutput, type) {
        function onGenericTypeSelectionMenuAsked(event) {
            data.onGenericTypeSelectionMenuAsked(id, inputOrOutput.id, event.clientX, event.clientY);
        }
        return (0, jsx_runtime_1.jsxs)("div", __assign({ className: 'nodeConnection_' + type }, { children: [(0, jsx_runtime_1.jsx)("div", __assign({ style: { paddingRight: 10, paddingLeft: 10 } }, { children: inputOrOutput.name })), (0, jsx_runtime_1.jsx)(reactflow_1.Handle, { type: type, position: type == 'source' ? reactflow_1.Position.Right : reactflow_1.Position.Left, id: inputOrOutput.id, style: { background: inputOrOutput.color }, isConnectable: true, isValidConnection: data.isValidConnection }), inputOrOutput.isGeneric ? (0, jsx_runtime_1.jsx)("svg", { onClick: onGenericTypeSelectionMenuAsked, viewBox: "0 0 24 24", className: type == 'source' ? 'generic_source_link' : 'generic_target_link', dangerouslySetInnerHTML: { __html: linkIcon } }) : (0, jsx_runtime_1.jsx)(jsx_runtime_1.Fragment, {})] }), inputOrOutput.id);
    }
    return ((0, jsx_runtime_1.jsxs)(jsx_runtime_1.Fragment, { children: [(0, jsx_runtime_1.jsx)("div", { children: data.name }), data.inputs.map(function (x) { return getConnection(x, 'target'); }), data.outputs.map(function (x) { return getConnection(x, 'source'); })] }));
});
//# sourceMappingURL=NodeWithMultipleHandles.js.map