
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
    connections?: string[];
    color: string;
    type: string;
    isGeneric: boolean;
    allowTextboxEdit: boolean;
    textboxValue?: string;
}

export interface CanvasInfos {
    dotnet: any;
    AddNodes: (props: NodeCreationInfo[]) => void;
    Destroy: () => void;
}