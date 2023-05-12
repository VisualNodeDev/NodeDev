import { Connection, Node } from 'reactflow'

export interface NodeCreationInfo {
    id: string;
    name: string;
    hasOverloads: boolean;
    titleColor: string;
    x: number;
    y: number;
    inputs: NodeCreationInfo_Connection[];
    outputs: NodeCreationInfo_Connection[];
}

export interface NodeCreationInfo_Connection {
    id: string;
    name: string;
    connections?: { connectionId: string, nodeId: string }[];
    color: string;
    type: string | 'exec';
    isGeneric: boolean;
    allowTextboxEdit: boolean;
    textboxValue?: string;
}

export interface NodeData {
    name: string;
    titleColor: string;
    hasOverloads: boolean;
    inputs: NodeCreationInfo_Connection[];
    outputs: NodeCreationInfo_Connection[];
    isValidConnection: (connection: Connection) => boolean;
    onGenericTypeSelectionMenuAsked: (nodeId: string, connectionId: string, x: number, y: number) => void;
    onOverloadSelectionMenuAsked: (nodeId: string) => void;
    onTextboxValueChanged: (nodeId: string, connectionId: string, value: string) => void;
}
export interface UpdateNodesParameters {
    nodes: NodeCreationInfo[];
}

export interface UpdateNodeBaseInfoParameters {
    id: string;
    name: string;
    titleColor: string;
    hasOverloads: boolean;
}

export interface CanvasInfos {
    id: string;
    dotnet: any;
    AddNodes: (props: NodeCreationInfo[]) => void;
    UpdateNodeBaseInfo: (props: UpdateNodeBaseInfoParameters) => void;
    UpdateConnectionType: (type: { nodeId: string, id: string, type: string, isGeneric: boolean, color: string, allowTextboxEdit: boolean, textboxValue: string | undefined }) => void;
    UpdateNodes : (nodes: UpdateNodesParameters) => void;
    Destroy: () => void;
}