"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
exports.__esModule = true;
var jsx_runtime_1 = require("react/jsx-runtime");
var react_1 = require("react");
var client_1 = require("react-dom/client");
var GraphCanvas_1 = __importDefault(require("./GraphCanvas"));
require("reactflow/dist/style.css");
var Canvas = {};
window["InitializeCanvas"] = function (dotnet, id) {
    var info = Canvas[id] = {
        dotnet: dotnet,
        AddNodes: function (props) { },
        Destroy: function () { delete window['Canvas_' + id]; }
    };
    window['Canvas_' + id] = Canvas[id];
    (0, client_1.createRoot)(document.getElementById(id)).render((0, jsx_runtime_1.jsx)(react_1.StrictMode, { children: (0, jsx_runtime_1.jsx)(GraphCanvas_1["default"], { CanvasInfos: info }) }));
};
//# sourceMappingURL=index.js.map