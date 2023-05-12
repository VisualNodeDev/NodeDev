import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { ReactFlowProvider } from "reactflow";
import GraphCanvas from "./GraphCanvas"
import "reactflow/dist/style.css";

import * as Types from './Types'

let Canvas = {} as any;

(window as any)["InitializeCanvas"] = function (dotnet: any, id: any) {

    let info = Canvas[id] = {
        id: id,
        dotnet: dotnet,
        nodes: [] as any[],
        AddNodes: function (props: Types.NodeCreationInfo[]) { },
        Destroy: function () { delete (window as any)['Canvas_' + id]; },
        UpdateConnectionType: function (type: { nodeId: string, id: string, type: string, isGeneric: boolean, color: string, allowTextboxEdit: boolean, textboxValue: string | undefined }) { },
        UpdateNodeBaseInfo: function (props: Types.UpdateNodeBaseInfoParameters) { },
        UpdateNodes: function (props: Types.UpdateNodesParameters) { },
    } as Types.CanvasInfos;
    (window as any)['Canvas_' + id] = Canvas[id];

    createRoot(document.getElementById(id) as HTMLElement).render(
        <StrictMode>
            <ReactFlowProvider>
                <GraphCanvas CanvasInfos={info}></GraphCanvas>
            </ReactFlowProvider>
        </StrictMode>
    );

}
