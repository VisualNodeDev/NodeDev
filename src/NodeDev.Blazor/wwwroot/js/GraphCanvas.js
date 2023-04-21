// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.

let Canvas = {};


class GraphCanvas {

    Dotnet;
    Canvas;

    constructor(dotnet, id) {
        this.Dotnet = dotnet;

        this.Canvas = new draw2d.Canvas(id);
        this.Canvas.setScrollArea("html")
        this.Canvas.Graph = this;

        this.Canvas.installEditPolicy(new draw2d.policy.canvas.SnapToGridEditPolicy())
        this.Canvas.installEditPolicy(new draw2d.policy.connection.DragConnectionCreatePolicy({
            createConnection: this.createConnection.bind(this)
        }));
        draw2d.Configuration.factory.createInputPort = function () {
            let port = new draw2d.InputPort();
            port.uninstallEditPolicy("draw2d.policy.port.IntrusivePortsFeedbackPolicy")
            port.installEditPolicy(new draw2d.policy.port.ExclusiveIntrusivePortsFeedbackPolicy())
            port.installEditPolicy(new draw2d.policy.port.IntrusivePortsFeedbackPolicy())
            return port
        }
        draw2d.Configuration.factory.createOutputPort = function () {
            let port = new draw2d.OutputPort();
            port.uninstallEditPolicy("draw2d.policy.port.IntrusivePortsFeedbackPolicy")
            port.installEditPolicy(new draw2d.policy.port.ExclusiveIntrusivePortsFeedbackPolicy())
            port.installEditPolicy(new draw2d.policy.port.IntrusivePortsFeedbackPolicy())
            return port
        }

        // bind events :
        this.Canvas.on("select", this.OnNodeSelected.bind(this));
        this.Canvas.on("unselect", this.OnNodeUnselected.bind(this));
        this.Canvas.on("figure:add", this.OnFigureAdded.bind(this));
        this.Canvas.on("figure:remove", this.OnFigureRemoved.bind(this));
    }

    AddNodes(infos) {
        if (infos.length === undefined)
            infos = [infos];
        // create nodes without the links
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

        // links them all together
        for (let i = 0; i < infos.length; ++i) {
            for (let j = 0; j < infos[i].inputs.length; ++j) {
                let inputInfo = infos[i].inputs[j];
                for (let k = 0; k < inputInfo.connections.length; ++k) {

                    let input = this.getPort(inputInfo.id);
                    let output = this.getPort(inputInfo.connections[k]);

                    let connection = this.createConnection(input, output);
                    connection.fromNet = true; // prevent the event to be fired
                    this.Canvas.add(connection);
                }
            }
        }
        for (let i = 0; i < infos.length; ++i) {
            for (let j = 0; j < infos[i].outputs.length; ++j) {
                let outputInfo = infos[i].outputs[j];
                for (let k = 0; k < outputInfo.connections.length; ++k) {

                    let output = this.getPort(outputInfo.id);
                    let input = this.getPort(outputInfo.connections[k]);

                    let found = input.connections.data.find(x => x.targetPort.id == outputInfo.id);
                    if (!found) {
                        let connection = this.createConnection(input, output);
                        connection.fromNet = true; // prevent the event to be fired
                        this.Canvas.add(connection);
                    }
                }
            }
        }
    }

    createConnection(sourcePort, targetPort) {

        var conn = new draw2d.Connection({
            router: new draw2d.layout.connection.ManhattanConnectionRouter(),
            stroke: 2,
            color: sourcePort ? sourcePort.bgColor : "#00a8f0",
            lineColor: sourcePort ? sourcePort.bgColor : "#00a8f0",
            radius: 20,
            outlineColor: "#30ff30",
            source: sourcePort,
            target: targetPort
        });

        return conn;
    }

    getPort(id) {
        for (let i = 0; i < this.Canvas.figures.data.length; ++i) {
            let port = this.Canvas.figures.data[i].getPorts().data.find(x => x.id == id);
            if (port)
                return port;
        }
    }
    getNode(id) {
        for (let i = 0; i < this.Canvas.figures.data.length; ++i) {
            if (this.Canvas.figures.data[i].id == id)
                return this.Canvas.figures.data[i];
        }
        throw "unable to find node id:" + id;
    }

    // ------------------------------------- events
    OnNodeSelected(emitter, event) {
        if (!(event.figure instanceof draw2d.Connection))
            this.Dotnet.invokeMethodAsync('OnNodeSelectedInClient', event.figure.id);
    }
    OnNodeUnselected(emitter, event) {
        if (!(event.figure instanceof draw2d.Connection))
            this.Dotnet.invokeMethodAsync('OnNodeUnselectedInClient', event.figure.id);
    }
    OnFigureAdded(emitter, event) {
        if (event.figure instanceof draw2d.Connection && !event.figure.fromNet) {
            this.Dotnet.invokeMethodAsync('OnConnectionAdded', event.figure.sourcePort.nodeId, event.figure.sourcePort.id, event.figure.targetPort.nodeId, event.figure.targetPort.id);
            event.figure.lineColor = event.figure.sourcePort.bgColor;
            event.figure.repaint();
        }
    }
    OnFigureRemoved(emitter, event) {
        if (event.figure instanceof draw2d.Connection)
            this.Dotnet.invokeMethodAsync('OnConnectionRemoved', event.figure.sourcePort.nodeId, event.figure.sourcePort.id, event.figure.targetPort.nodeId, event.figure.targetPort.id);
        else
            this.Dotnet.invokeMethodAsync('OnNodeRemoved', event.figure.id);
    }
    onNodeMove(emitter, event) {
        this.nodeMoveTimeoutId = this.limitFunctionCall(this.nodeMoveTimeoutId, () => {
            this.Dotnet.invokeMethodAsync('OnNodeMoved', emitter.id, event.x, event.y);
        }, 250);
    }
    OnPortDroppedOnCanvas(port, x, y) {
        this.Dotnet.invokeMethodAsync('OnPortDroppedOnCanvas', port.nodeId, port.id, x, y);
    }

    limitFunctionCall(timeoutId, fn, limit) {
        clearTimeout(timeoutId);
        return setTimeout(fn, limit);
    }
    Destroy() {
        delete window['Canvas_' + this.id];
    }
}


window.InitializeCanvas = function (dotnet, id) {
    Canvas[id] = new GraphCanvas(dotnet, id);
    window['Canvas_' + id] = Canvas[id];
}
