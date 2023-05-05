import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import GraphCanvas from "./GraphCanvas"
import "reactflow/dist/style.css";

import * as Types from './Types'

let Canvas = {} as any;

(window as any)["InitializeCanvas"] = function (dotnet: any, id: any) {

    let info = Canvas[id] = {
        dotnet: dotnet,
        AddNodes: function (props: Types.NodeCreationInfo[]) { },
        Destroy: function () { delete (window as any)['Canvas_' + id]; }
    } ;
    (window as any)['Canvas_' + id] = Canvas[id];

    createRoot(document.getElementById(id) as HTMLElement).render(
        <StrictMode>
            <GraphCanvas CanvasInfos={info}></GraphCanvas>
        </StrictMode>
    );

}
