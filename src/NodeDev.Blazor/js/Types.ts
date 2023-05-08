import { Connection, Node } from 'reactflow'

export interface NodeCreationInfo {
    id: string;
    name: string;
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
    inputs: NodeCreationInfo_Connection[];
    outputs: NodeCreationInfo_Connection[];
    isValidConnection: (connection: Connection) => boolean;
    onGenericTypeSelectionMenuAsked: (nodeId: string, connectionId: string, x: number, y: number) => void;
    onTextboxValueChanged: (nodeId: string, connectionId: string, value: string) => void;
}

export interface CanvasInfos {
    id: string;
    dotnet: any;
    AddNodes: (props: NodeCreationInfo[]) => void;
    UpdateConnectionType: (type: { nodeId: string, id: string, type: string, isGeneric: boolean, color: string, allowTextboxEdit: boolean, textboxValue: string | undefined }) => void;
    Destroy: () => void;
}