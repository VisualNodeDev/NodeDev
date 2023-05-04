import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import GraphCanvas from "./GraphCanvas"
import "reactflow/dist/style.css";

let Canvas: any = {};

(window as any)["InitializeCanvas"] = function (dotnet: any, id: string) {
    //Canvas[id] = new GraphCanvas(dotnet, id);
    //(window as any)['Canvas_' + id] = Canvas[id];

    createRoot(document.getElementById(id) as HTMLElement).render(
        <StrictMode>
            <GraphCanvas></GraphCanvas>
        </StrictMode>
    );

}
