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
    var data = _a.data, isConnectable = _a.isConnectable;
    return ((0, jsx_runtime_1.jsxs)(jsx_runtime_1.Fragment, { children: [(0, jsx_runtime_1.jsx)(reactflow_1.Handle, { type: "target", position: reactflow_1.Position.Left, onConnect: function (params) { return console.log('handle onConnect', params); }, isConnectable: isConnectable }), (0, jsx_runtime_1.jsx)("div", { children: data === null || data === void 0 ? void 0 : data.label }), (0, jsx_runtime_1.jsxs)("div", __assign({ style: { display: 'flex', flexDirection: 'row', justifyContent: 'right', position: 'relative' } }, { children: [(0, jsx_runtime_1.jsx)("div", __assign({ style: { paddingRight: 10, paddingLeft: 10 } }, { children: "Output A" })), (0, jsx_runtime_1.jsx)(reactflow_1.Handle, { type: "source", position: reactflow_1.Position.Right, id: "a", isConnectable: isConnectable })] })), (0, jsx_runtime_1.jsxs)("div", __assign({ style: { display: 'flex', flexDirection: 'row', justifyContent: 'right', position: 'relative' } }, { children: [(0, jsx_runtime_1.jsx)("div", __assign({ style: { paddingRight: 10, paddingLeft: 10 } }, { children: "Output B" })), (0, jsx_runtime_1.jsx)(reactflow_1.Handle, { type: "source", position: reactflow_1.Position.Right, id: "b", isConnectable: isConnectable })] }))] }));
});
//# sourceMappingURL=NodeWithMultipleHandles.js.map