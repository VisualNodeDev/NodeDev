// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

let Canvas = {};


class GraphCanvas {

    dotnet;
    Canvas;

    constructor(dotnet, id) {
        this.dotnet = dotnet;

        this.Canvas = new draw2d.Canvas(id);

        this.Canvas.installEditPolicy(new draw2d.policy.canvas.SnapToGridEditPolicy())
    }

    AddNodes(infos) {

        for (let i = 0; i < infos.length; ++i) {
            this.Canvas.add(new NodeShape({
                id: infos[i].id,
                LabelText: infos[i].name,
                x: infos[i].x,
                y: infos[i].y,
                inputs: infos[i].inputs,
                outputs: infos[i].outputs
            }));
        }
    }

    Destroy() {
        delete window['Canvas_' + this.id];
    }
}


window.InitializeCanvas = function(dotnet, id) {
    Canvas[id] = new GraphCanvas(dotnet, id);
    window['Canvas_' + id] = Canvas[id];
}
