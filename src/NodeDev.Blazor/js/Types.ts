import { Connection } from 'reactflow'

export interface NodeCreationInfo {
    id: string;
    name: string;
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
    inputs: NodeCreationInfo_Connection[];
    outputs: NodeCreationInfo_Connection[];
    isValidConnection: (connection: Connection) => boolean;
    onGenericTypeSelectionMenuAsked: (nodeId: string, connectionId: string, x: number, y: number) => void;
}

export interface CanvasInfos {
    dotnet: any;
    AddNodes: (props: NodeCreationInfo[]) => void;
    UpdateConnectionType: (type: { nodeId: string, id: string, type: string, isGeneric: boolean, color: string, allowTextboxEdit: boolean, textboxValue: string | undefined }) => void;
    Destroy: () => void;
}