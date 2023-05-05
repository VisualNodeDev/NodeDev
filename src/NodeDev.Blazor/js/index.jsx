import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import GraphCanvas from "./GraphCanvas"
import "reactflow/dist/style.css";
import * as Types from './Types'

let Canvas = {};

(window)["InitializeCanvas"] = function (dotnet, id) {

    let info = Canvas[id] = {
        dotnet: dotnet,
        AddNodes: function (props) { }
    } ;
    (window)['Canvas_' + id] = Canvas[id];

    createRoot(document.getElementById(id)).render(
        <StrictMode>
            <GraphCanvas CanvasInfos={info}></GraphCanvas>
        </StrictMode>
    );

}
