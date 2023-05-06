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
    type: string | 'exec' | 'generic';
    isGeneric: boolean;
    allowTextboxEdit: boolean;
    textboxValue?: string;
}

export interface NodeData {
    name: string;
    inputs: NodeCreationInfo_Connection[];
    outputs: NodeCreationInfo_Connection[];
    isValidConnection: (connection: Connection) => boolean;
}

export interface CanvasInfos {
    dotnet: any;
    AddNodes: (props: NodeCreationInfo[]) => void;
    Destroy: () => void;
}