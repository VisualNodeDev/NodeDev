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
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
exports.__esModule = true;
var jsx_runtime_1 = require("react/jsx-runtime");
var react_1 = require("react");
var reactflow_1 = __importStar(require("reactflow"));
//import CustomNode from "./CustomNode";
require("reactflow/dist/style.css");
var initialNodes = [
    {
        id: "1",
        type: "input",
        data: { label: "Node 1" },
        position: { x: 250, y: 5 }
    },
    { id: "2", data: { label: "Node 2" }, position: { x: 100, y: 100 } },
    { id: "3", data: { label: "Node 3" }, position: { x: 400, y: 100 } },
    {
        id: "4",
        //type: "custom",
        data: { label: "Node 4" },
        position: { x: 400, y: 200 }
    }
];
var initialEdges = [
    { id: "e1-2", source: "1", target: "2", animated: true },
    { id: "e1-3", source: "1", target: "3" }
];
var nodeTypes = {
//custom: CustomNode
};
var BasicFlow = function () {
    var _a = (0, reactflow_1.useNodesState)(initialNodes), nodes = _a[0], onNodesChange = _a[2];
    var _b = (0, reactflow_1.useEdgesState)(initialEdges), edges = _b[0], setEdges = _b[1], onEdgesChange = _b[2];
    var onConnect = (0, react_1.useCallback)(function (params) { return setEdges(function (els) { return (0, reactflow_1.addEdge)(params, els); }); }, [setEdges]);
    return ((0, jsx_runtime_1.jsx)(reactflow_1["default"], __assign({ nodes: nodes, edges: edges, onNodesChange: onNodesChange, onEdgesChange: onEdgesChange, onConnect: onConnect, nodeTypes: nodeTypes }, { children: (0, jsx_runtime_1.jsx)(reactflow_1.Background, {}) })));
};
exports["default"] = BasicFlow;
