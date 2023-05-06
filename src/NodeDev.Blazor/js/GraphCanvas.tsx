import { useCallback } from "react";
import ReactFlow, {
    Node,
    addEdge,
    Background,
    Edge,
    Connection,
    useNodesState,
    useEdgesState,
    NodeChange,
    applyNodeChanges,
    NodePositionChange,
    Position
} from "reactflow";

import NodeWithMultipleHandles from "./NodeWithMultipleHandles";

import "reactflow/dist/style.css";
import '../css/styles.css';

import * as Types from './Types'
import * as Utility from './Utility'

const initialNodes: Node[] = [];
const initialEdges: Edge[] = [];


const nodeTypes = {
    NodeWithMultipleHandles: NodeWithMultipleHandles
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

        for (let i = 0; i < newNodes.length; i++) {
            nodes.push({
                id: newNodes[i].id,
                data: {
                    name: newNodes[i].name,
                    inputs: newNodes[i].inputs,
                    outputs: newNodes[i].outputs
                } as Types.NodeData,
                position: { x: newNodes[i].x, y: newNodes[i].y },
                type: 'NodeWithMultipleHandles'
            });
        }

        setNodes(nodes.map(x => x)); // the 'map' is a patch, the nodes are not updated otherwise
    }

    let nodeMoveTimeoutId: any = {};
    function nodesChanged(changes: NodeChange[]) {
        onNodesChange(changes);

        for (let i = 0; i < changes.length; i++) {
            let change = changes[i];
            if (change.type === 'select') {
                props.CanvasInfos.dotnet.invokeMethodAsync(change.selected ? 'OnNodeSelectedInClient' : 'OnNodeUnselectedInClient', change.id);
            }
            else if (change.type === 'position' && change.position) {
                nodeMoveTimeoutId[change.id] = Utility.limitFunctionCall(nodeMoveTimeoutId[change.id], () => {
                    change = change as NodePositionChange;
                    props.CanvasInfos.dotnet.invokeMethodAsync('OnNodeMoved', change.id, change.position!.x, change.position!.y);
                }, 250);
            }
        }
    }

    return (
        <ReactFlow
            nodes={nodes}
            edges={edges}
            onNodesChange={nodesChanged}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            nodeTypes={nodeTypes}
        >
            <Background />
        </ReactFlow>
    );
};

