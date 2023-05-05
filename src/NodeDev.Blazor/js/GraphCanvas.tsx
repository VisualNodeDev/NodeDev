import { useCallback } from "react";
import ReactFlow, {
    Node,
    addEdge,
    Background,
    Edge,
    Connection,
    useNodesState,
    useEdgesState
} from "reactflow";

//import CustomNode from "./CustomNode";

import "reactflow/dist/style.css";
import * as Types from './Types'

const initialNodes: Node[] = [];

const initialEdges: Edge[] = [];

const nodeTypes = {
    //custom: CustomNode
};

export default function BasicFlow(props: { CanvasInfos: Types.CanvasInfos }) {
    const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
    const onConnect = useCallback(
        (params: Edge | Connection) => setEdges((els) => addEdge(params, els)),
        [setEdges]
    );

    props.CanvasInfos.AddNodes = function (newNodes: Types.NodeCreationInfo[]) {

        if (newNodes.length === undefined)
            newNodes = [newNodes] as any;

        for (let i = 0; i < newNodes.length; i++)
            nodes.push({ id: newNodes[i].id, data: { label: newNodes[i].name }, position: { x: newNodes[i].x, y: newNodes[i].y } });

        setNodes(nodes);
    }

    return (
        <ReactFlow
            nodes={nodes}
            edges={edges}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            nodeTypes={nodeTypes}
        >
            <Background />
        </ReactFlow>
    );
};

