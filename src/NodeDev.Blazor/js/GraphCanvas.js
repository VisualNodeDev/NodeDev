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
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
exports.__esModule = true;
var jsx_runtime_1 = require("react/jsx-runtime");
var react_1 = require("react");
var reactflow_1 = __importStar(require("reactflow"));
var NodeWithMultipleHandles_1 = __importDefault(require("./NodeWithMultipleHandles"));
require("reactflow/dist/style.css");
require("../css/styles.css");
var Utility = __importStar(require("./Utility"));
var initialNodes = [];
var initialEdges = [];
var nodeTypes = {
    NodeWithMultipleHandles: NodeWithMultipleHandles_1["default"]
};
function BasicFlow(props) {
    var _a = (0, reactflow_1.useNodesState)(initialNodes), nodes = _a[0], setNodes = _a[1], onNodesChange = _a[2];
    var _b = (0, reactflow_1.useEdgesState)(initialEdges), edges = _b[0], setEdges = _b[1], onEdgesChange = _b[2];
    var onConnect = (0, react_1.useCallback)(function (params) { return setEdges(function (els) { return (0, reactflow_1.addEdge)(params, els); }); }, [setEdges]);
    props.CanvasInfos.AddNodes = function (newNodes) {
        if (newNodes.length === undefined)
            newNodes = [newNodes];
        for (var i = 0; i < newNodes.length; i++) {
            nodes.push({
                id: newNodes[i].id,
                data: { label: newNodes[i].name, inputs: newNodes[i].inputs, outputs: newNodes[i].outputs }, position: { x: newNodes[i].x, y: newNodes[i].y },
                type: 'NodeWithMultipleHandles'
            });
        }
        setNodes(nodes.map(function (x) { return x; })); // the 'map' is a patch, the nodes are not updated otherwise
    };
    var nodeMoveTimeoutId = {};
    function nodesChanged(changes) {
        onNodesChange(changes);
        var _loop_1 = function (i) {
            var change = changes[i];
            if (change.type === 'select') {
                props.CanvasInfos.dotnet.invokeMethodAsync(change.selected ? 'OnNodeSelectedInClient' : 'OnNodeUnselectedInClient', change.id);
            }
            else if (change.type === 'position' && change.position) {
                nodeMoveTimeoutId[change.id] = Utility.limitFunctionCall(nodeMoveTimeoutId[change.id], function () {
                    change = change;
                    props.CanvasInfos.dotnet.invokeMethodAsync('OnNodeMoved', change.id, change.position.x, change.position.y);
                }, 250);
            }
        };
        for (var i = 0; i < changes.length; i++) {
            _loop_1(i);
        }
    }
    return ((0, jsx_runtime_1.jsx)(reactflow_1["default"], __assign({ nodes: nodes, edges: edges, onNodesChange: nodesChanged, onEdgesChange: onEdgesChange, onConnect: onConnect, nodeTypes: nodeTypes }, { children: (0, jsx_runtime_1.jsx)(reactflow_1.Background, {}) })));
}
exports["default"] = BasicFlow;
;
//# sourceMappingURL=GraphCanvas.js.map