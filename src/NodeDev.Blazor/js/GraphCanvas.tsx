import { useCallback, useRef, useState } from "react";
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
    ReactFlowProvider,
    NodeRemoveChange,
    BackgroundVariant
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
    var [_Nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    var [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
    const onConnect = useCallback(
        (params: Edge | Connection) => setEdges((els) => addEdge(params, els)),
        [setEdges]
    );
    const reactFlowWrapper = useRef(null);
    const { project } = useReactFlow();
    const connectingNodeId = useRef<OnConnectStartParams>(null);

    const [backgroundId1, setBackgroundId1] = useState();
    const [backgroundId2, setBackgroundId2] = useState();

    if (!backgroundId1) {
        let id = (window as any).id;
        if (!id)
            id = 1;
        setBackgroundId1(id + 1);
        setBackgroundId2(id + 2);
        (window as any).id = id + 3;
    }

    function getNodes() {
        let nodes: Node<Types.NodeData>[] = [];
        setNodes(nds => nodes = nds);
        return nodes;
    }
    function getEdges() {
        let edges: Edge[] = [];
        setEdges(edg => edges = edg);
        return edges;
    }
    function onTextboxValueChanged(nodeId: string, connectionId: string, value: string) {
        setNodes((nds) =>
            nds.map((node) => {
                if (node.id === nodeId) {
                    // find the connection
                    let connection = node.data.inputs.find(x => x.id === connectionId);
                    if (!connection)
                        connection = node.data.outputs.find(x => x.id === connectionId);
                    if (!connection)
                        return node;

                    connection.textboxValue = value;

                    node.data = {
                        ...node.data
                    };
                }
                return node;
            })
        );
        props.CanvasInfos.dotnet.invokeMethodAsync('OnTextboxValueChanged', nodeId, connectionId, value);
    }
    function onGenericTypeSelectionMenuAsked(nodeId: string, connectionId: string, x: number, y: number) {
        props.CanvasInfos.dotnet.invokeMethodAsync('OnGenericTypeSelectionMenuAsked', nodeId, connectionId, x, y);
    }
    function onOverloadSelectionMenuAsked(nodeId: string) {
        props.CanvasInfos.dotnet.invokeMethodAsync('OnOverloadSelectionRequested', nodeId);
    }
    function isValidConnection(connection: Connection) {
        if (!connection.source || !connection.target)
            return false;

        let nodes = getNodes();

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
            else if (change.type == 'remove') {
                props.CanvasInfos.dotnet.invokeMethodAsync('OnNodeRemoved', change.id);
                setNodes(nds => nds.filter(x => x.id != (change as any).id));
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

    function connectStart(event: any, params: OnConnectStartParams) {
        (connectingNodeId as any).current = params;
    }
    function connectEnd(event: MouseEvent) {
        const targetIsPane = (event as any).target.classList.contains('react-flow__pane');

        if (targetIsPane && connectingNodeId.current) {
            // we need to remove the wrapper bounds, in order to get the correct position
            const { top, left } = (reactFlowWrapper.current as any).getBoundingClientRect();
            let positionInFlow = project({ x: event.clientX - left, y: event.clientY - top });
            props.CanvasInfos.dotnet.invokeMethodAsync('OnPortDroppedOnCanvas', connectingNodeId.current.nodeId, connectingNodeId.current.handleId, event.clientX - left, event.clientY - top, positionInFlow.x, positionInFlow.y);
        }
    }

    props.CanvasInfos.UpdateConnectionType = function (type: { nodeId: string, id: string, type: string, isGeneric: boolean, color: string, allowTextboxEdit: boolean, textboxValue: string | undefined }) {
        setNodes((nds) =>
            nds.map((node) => {
                if (node.id === type.nodeId) {

                    // update all edges
                    setEdges((edges) =>
                        edges.map(edge => {

                            if ((edge.source == type.nodeId && edge.sourceHandle == type.id) || (edge.target == type.nodeId && edge.targetHandle == type.id))
                                edge.className = 'stroke_color_' + type.color;

                            return edge;
                        })
                    );

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
    function getNodeData(nodeInfo: Types.NodeCreationInfo) {
        return {
            name: nodeInfo.name,
            titleColor: nodeInfo.titleColor,
            hasOverloads: nodeInfo.hasOverloads,
            inputs: nodeInfo.inputs,
            outputs: nodeInfo.outputs,
            isValidConnection: isValidConnection,
            onGenericTypeSelectionMenuAsked: onGenericTypeSelectionMenuAsked,
            onTextboxValueChanged: onTextboxValueChanged,
            onOverloadSelectionMenuAsked: onOverloadSelectionMenuAsked
        } as Types.NodeData;
    }
    function getEdgesForNodesIfNecessary(edges: Edge[], edgesToAdd: Edge[], info: Types.NodeCreationInfo) {
        for (let j = 0; j < info.inputs.length; j++) {
            let input = info.inputs[j];
            if (!input.connections)
                continue;
            for (let j = 0; j < input.connections.length; j++) {
                let id = input.id + '_' + input.connections[j].connectionId;
                if (edges.concat(edgesToAdd).find(x => x.id === id))
                    continue;
                edgesToAdd.push({
                    id: id,
                    target: info.id,
                    targetHandle: input.id,
                    source: input.connections[j].nodeId,
                    sourceHandle: input.connections[j].connectionId,
                    className: 'stroke_color_' + input.color
                });
            }
        }
        for (let j = 0; j < info.outputs.length; j++) {
            let output = info.outputs[j];
            if (!output.connections)
                continue;
            for (let j = 0; j < output.connections.length; j++) {
                let id = output.connections[j].connectionId + '_' + output.id;
                if (edges.concat(edgesToAdd).find(x => x.id === id))
                    continue;
                edgesToAdd.push({
                    id: id,
                    target: output.connections[j].nodeId,
                    targetHandle: output.connections[j].connectionId,
                    source: info.id,
                    sourceHandle: output.id,
                    className: 'stroke_color_' + output.color
                });
            }
        }
    }
    props.CanvasInfos.AddNodes = function (newNodes: Types.NodeCreationInfo[]) {

        if (newNodes.length === undefined)
            newNodes = [newNodes] as any;

        let edges = getEdges();
        let nodesToAdd: Node<Types.NodeData>[] = [];
        let edgesToAdd: Edge[] = [];
        for (let i = 0; i < newNodes.length; i++) {
            nodesToAdd.push({
                id: newNodes[i].id,
                data: getNodeData(newNodes[i]),
                position: { x: newNodes[i].x, y: newNodes[i].y },
                type: 'NodeWithMultipleHandles'
            });

            getEdgesForNodesIfNecessary(edges, edgesToAdd, newNodes[i]);
        }

        setNodes(nds => nds.concat(nodesToAdd));
        setEdges(edg => edg.concat(edgesToAdd));
    };
    props.CanvasInfos.UpdateNodes = function (props: Types.UpdateNodesParameters) {
        let nodes: Node<Types.NodeData>[];
        setNodes(nds => {
            for (let i = 0; i < props.nodes.length; i++) {
                let info = props.nodes[i];
                let node = nds.find(x => x.id === info.id);
                if (node)
                    node.data = getNodeData(info);
            }
            return nodes = nds.map(x => x);
        });
        setEdges(edg => {

            let newEdges : Edge[] = [];
            // find the new edges to add

            for (let i = 0; i < props.nodes.length; i++)
                getEdgesForNodesIfNecessary(edg, newEdges, props.nodes[i]);

            return edg.filter(edge => {
                // find if the edge is still connected to valid nodes
                let sourceNode = nodes.find(x => x.id === edge.source);
                let targetNode = nodes.find(x => x.id === edge.target);

                if (!sourceNode || !targetNode) // should never happen since we didn't remove any node
                    return false; // just remove that edge I guess ?

                let sourceConnection = sourceNode.data.outputs.find(x => x.id === edge.sourceHandle);
                let targetConnection = targetNode.data.inputs.find(x => x.id === edge.targetHandle);

                return sourceConnection && targetConnection; // keep the edge if both source and target are still valid
            }).concat(newEdges);
        });
    };
    props.CanvasInfos.UpdateNodeBaseInfo = function (info: Types.UpdateNodeBaseInfoParameters) {
        setNodes(nds =>
            nds.map(node => {
                if (node.id !== info.id)
                    return node;

                node.data.name = info.name;
                node.data.titleColor = info.titleColor;
                node.data.hasOverloads = info.hasOverloads;
                node.data = {
                    ...node.data
                };

                return node;
            })
        );
    };

    (window as any)['Canvas_' + props.CanvasInfos.id] = { ...props.CanvasInfos };

    return (
        <div style={{ height: '100%', flexGrow: '1' }} ref={reactFlowWrapper}>
            <ReactFlow
                nodes={_Nodes}
                edges={edges}
                nodeTypes={nodeTypes}
                onNodesChange={nodesChanged}
                onEdgesChange={onEdgesChange}
                onConnect={nodeConnected}
                onEdgesDelete={edgeDeleted}
                onConnectEnd={connectEnd as any}
                onConnectStart={connectStart as any}
            >
                <Background id={backgroundId1} gap={10} color="#f1f1f1" variant={BackgroundVariant.Lines} />
                <Background id={backgroundId2} gap={100} offset={1} color="#ccc" variant={BackgroundVariant.Lines} />
            </ReactFlow>
        </div>
    );
};

