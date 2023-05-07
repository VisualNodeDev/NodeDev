import { useCallback, useRef } from "react";
import ReactFlow, {
    Node,
    addEdge,
    Background,
    Edge,
    Connection,
    useNodesState,
    useEdgesState,
    NodeChange,
    NodePositionChange,
    useReactFlow,
    OnConnectStartParams,
    ReactFlowProvider
} from "reactflow";

import NodeWithMultipleHandles from "./NodeWithMultipleHandles";

import "reactflow/dist/style.css";
import '../css/styles.css';

import * as Types from './Types'
import * as Utility from './Utility'

const initialNodes: Node<Types.NodeData>[] = [];
const initialEdges: Edge[] = [];


const nodeTypes = {
    NodeWithMultipleHandles: NodeWithMultipleHandles
};

let nodeMoveTimeoutId: any = {};
export default function BasicFlow(props: { CanvasInfos: Types.CanvasInfos }) {
    const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
    const onConnect = useCallback(
        (params: Edge | Connection) => setEdges((els) => addEdge(params, els)),
        [setEdges]
    );
    const reactFlowWrapper = useRef(null);
    const { project } = useReactFlow();
    const connectingNodeId = useRef<OnConnectStartParams>(null);

    props.CanvasInfos.AddNodes = function (newNodes: Types.NodeCreationInfo[]) {

        if (newNodes.length === undefined)
            newNodes = [newNodes] as any;

        for (let i = 0; i < newNodes.length; i++) {
            nodes.push({
                id: newNodes[i].id,
                data: {
                    name: newNodes[i].name,
                    inputs: newNodes[i].inputs,
                    outputs: newNodes[i].outputs,
                    isValidConnection: isValidConnection,
                    onGenericTypeSelectionMenuAsked: onGenericTypeSelectionMenuAsked
                },
                position: { x: newNodes[i].x, y: newNodes[i].y },
                type: 'NodeWithMultipleHandles'
            });

            for (let j = 0; j < newNodes[i].inputs.length; j++) {
                let input = newNodes[i].inputs[j];
                if (!input.connections)
                    continue;
                for (let j = 0; j < input.connections.length; j++) {
                    edges.push({
                        id: input.id + '_' + input.connections[j].connectionId,
                        target: newNodes[i].id,
                        targetHandle: input.id,
                        source: input.connections[j].nodeId,
                        sourceHandle: input.connections[j].connectionId
                    });
                }
            }
        }

        setNodes(nodes.map(x => x)); // the 'map' is a patch, the nodes are not updated otherwise
        setEdges(edges.map(x => x)); // the 'map' is a patch, the nodes are not updated otherwise
    }
    props.CanvasInfos.UpdateConnectionType = function (type: { nodeId: string, id: string, type: string, isGeneric: boolean, color: string, allowTextboxEdit: boolean, textboxValue: string | undefined }) {
        setNodes((nds) =>
            nds.map((node) => {
                if (node.id === type.nodeId) {
                    // find the connection
                    let connection = node.data.inputs.find(x => x.id === type.id);
                    if (!connection)
                        connection = node.data.outputs.find(x => x.id === type.id);
                    if (!connection)
                        return node;

                    connection.type = type.type;
                    connection.isGeneric = type.isGeneric;
                    connection.color = type.color;
                    connection.allowTextboxEdit = type.allowTextboxEdit;
                    connection.textboxValue = type.textboxValue;

                    // it's important that you create a new object here
                    // in order to notify react flow about the change
                    node.data = {
                        ...node.data
                    };
                }

                return node;
            })
        );



    }
    function onGenericTypeSelectionMenuAsked(nodeId: string, connectionId: string, x: number, y: number) {
        props.CanvasInfos.dotnet.invokeMethodAsync('OnGenericTypeSelectionMenuAsked', nodeId, connectionId, x, y);
    }
    function isValidConnection(connection: Connection) {
        if (!connection.source || !connection.target)
            return false;

        let source = Utility.findNode(nodes, connection.source);
        let target = Utility.findNode(nodes, connection.target);

        if (!source || !target)
            return false;

        let sourceOutput = source.data.outputs.find(x => x.id === connection.sourceHandle);
        let targetInput = target.data.inputs.find(x => x.id === connection.targetHandle);

        if (!sourceOutput || !targetInput)
            return false;

        if ((targetInput.isGeneric && sourceOutput.type !== 'exec') || (sourceOutput.isGeneric && targetInput.type !== 'exec'))
            return true;

        if (sourceOutput.type !== targetInput.type)
            return false;

        return true;
    }

    function nodesChanged(changes: NodeChange[]) {
        onNodesChange(changes);

        for (let i = 0; i < changes.length; i++) {
            let change = changes[i];
            if (change.type === 'select')
                props.CanvasInfos.dotnet.invokeMethodAsync(change.selected ? 'OnNodeSelectedInClient' : 'OnNodeUnselectedInClient', change.id);
            else if (change.type === 'position' && change.position) {
                nodeMoveTimeoutId[change.id] = Utility.limitFunctionCall(nodeMoveTimeoutId[change.id], () => {
                    change = change as NodePositionChange;
                    props.CanvasInfos.dotnet.invokeMethodAsync('OnNodeMoved', change.id, change.position!.x, change.position!.y);
                }, 250);
            }
        }
    }
    function nodeConnected(changes: Edge | Connection) {
        onConnect(changes);

        if (changes.sourceHandle) {
            (connectingNodeId as any).current = null;
            props.CanvasInfos.dotnet.invokeMethodAsync('OnConnectionAdded', changes.source, changes.sourceHandle, changes.target, changes.targetHandle);
        }
    }
    function edgeDeleted(edge: Edge[]) {
        for (let i = 0; i < edge.length; i++)
            props.CanvasInfos.dotnet.invokeMethodAsync('OnConnectionRemoved', edge[i].source, edge[i].sourceHandle, edge[i].target, edge[i].targetHandle);
    }
    function nodesDeleted(nodes: Node[]) {
        for (let i = 0; i < nodes.length; i++)
            props.CanvasInfos.dotnet.invokeMethodAsync('OnNodeRemoved', nodes[i].id);
    }


    function connectStart(event: any, params: OnConnectStartParams) {
        (connectingNodeId as any).current = params;
    }
    function connectEnd(event: MouseEvent) {
        const targetIsPane = (event as any).target.classList.contains('react-flow__pane');

        if (targetIsPane && connectingNodeId.current) {
            // we need to remove the wrapper bounds, in order to get the correct position
            const { top, left } = (reactFlowWrapper.current as any).getBoundingClientRect();
            props.CanvasInfos.dotnet.invokeMethodAsync('OnPortDroppedOnCanvas', connectingNodeId.current.nodeId, connectingNodeId.current.handleId, event.clientX - left, event.clientY - top);
        }
    }



    return (
        <div style={{height: '100%', width: '100%'}} ref={reactFlowWrapper}>
            <ReactFlow
                nodes={nodes}
                edges={edges}
                nodeTypes={nodeTypes}
                onNodesDelete={nodesDeleted}
                onNodesChange={nodesChanged}
                onEdgesChange={onEdgesChange}
                onConnect={nodeConnected}
                onEdgesDelete={edgeDeleted}
                onConnectEnd={connectEnd as any}
                onConnectStart={connectStart as any}
            >
                <Background />
            </ReactFlow>
        </div>
    );
};

