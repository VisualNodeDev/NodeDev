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

    AddNode() {
        this.Canvas.add(new draw2d.shape.basic.Rectangle({ width: 80, height: 40, x: 50, y: 300, resizeable: false }));
    }

    Destroy() {
        delete window['Canvas_' + this.id];
    }
}


window.InitializeCanvas = function(dotnet, id) {
    Canvas[id] = new GraphCanvas(dotnet, id);
    window['Canvas_' + id] = Canvas[id];
}
