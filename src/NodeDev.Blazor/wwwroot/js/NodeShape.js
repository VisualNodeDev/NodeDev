
NodeShape = draw2d.shape.layout.VerticalLayout.extend({

    NAME: "NodeShape",

    init: function (attr) {
        this._super($.extend({ bgColor: "#dbddde", color: "#d7d7d7", stroke: 1, radius: 3 }, attr));

        this.on('added', this.onAdded.bind(this));

        this.classLabel = new draw2d.shape.basic.Label({
            text: attr.LabelText,
            stroke: 1,
            fontColor: "#5856d6",
            bgColor: "#f7f7f7",
            radius: this.getRadius(),
            padding: 10,
            resizeable: true,
            editor: new draw2d.ui.LabelInplaceEditor()
        });

        this.add(this.classLabel);

        for (let i = 0; i < attr.inputs.length; ++i)
            this.addInputOrOutput(attr.inputs[i], true);

        for (let i = 0; i < attr.outputs.length; ++i)
            this.addInputOrOutput(attr.outputs[i], false);

    },
    onAdded: function (emitter, event) {
        this.on('move', this.canvas.Graph.onNodeMove.bind(this.canvas.Graph));
    },
    /**
     * @method
     * Add an entity to the db shape
     * 
     * @param {String} txt the label to show
     * @param {Number} [optionalIndex] index where to insert the entity
     */
    addInputOrOutput: function (info, isInput) {
        var label = new draw2d.shape.basic.Label({
            text: info.name,
            stroke: 0,
            radius: 0,
            bgColor: null,
            padding: { left: 10, top: 3, right: 10, bottom: 5 },
            fontColor: "#4a4a4a",
            resizeable: true,
            editor: new draw2d.ui.LabelEditor()
        });

        //        label.installEditor(new draw2d.ui.LabelEditor());
        var portType = isInput ? "input" : "output";
        var port = label.createPort(portType);

        port.id = info.id;
        port.nodeId = this.id;
        port.bgColor = new draw2d.util.Color(info.color);
        port.setName(portType + '_' + info.name);
        port.portType = portType;
        port.isGeneric = info.isGeneric;
        port.type = info.type;

        port.on('dragend', (emitter, event) => {
            if (!event.target)
                emitter.canvas.Graph.OnPortDroppedOnCanvas(port, event.x, event.y);
        });

        this.add(label);

        // if the port is generic, add a decoration to choose the type
        if (info.isGeneric) {
            var decoration = new draw2d.shape.icon.Link({ width: 10, height: 10 });
            port.add(decoration, isInput ? new draw2d.layout.locator.LeftLocator({ margin: 10 }) : new draw2d.layout.locator.RightLocator({ margin: 2 }));

            let that = this;
            decoration.on('click', (emitter, event) => emitter.canvas.Graph.OnGenericTypeSelectionMenuAsked(that.id, port.id, event.x, event.y));
        }
    },
    UpdateConnectionType: function (info, port) {

        if (port.isGeneric && !info.isGeneric) {
            for (let i = 0; i < port.children.data.length; ++i) {
                let child = port.children.data[i];
                if (child.figure instanceof draw2d.shape.icon.Link) {
                    port.remove(child);
                    port.canvas.remove(child.figure);
                    break;
                }
            }
        }

        port.bgColor = new draw2d.util.Color(info.color);
        port.isGeneric = info.isGeneric;
        port.type = info.type;
        for (let i = 0; i < port.connections.data.length; ++i) {
            let connection = port.connections.data[i];
            connection.lineColor = port.bgColor;

            connection.repaint();
        }


        port.repaint();
    },
    /**
     * @method
     * Remove the entity with the given index from the DB table shape.<br>
     * This method removes the entity without care of existing connections. Use
     * a draw2d.command.CommandDelete command if you want to delete the connections to this entity too
     * 
     * @param {Number} index the index of the entity to remove
     */
    removeEntity: function (index) {
        this.remove(this.children.get(index + 1).figure);
    },

    /**
     * @method
     * Returns the entity figure with the given index
     * 
     * @param {Number} index the index of the entity to return
     */
    getEntity: function (index) {
        return this.children.get(index + 1).figure;
    },


    /**
     * @method
     * Set the name of the DB table. Visually it is the header of the shape
     * 
     * @param name
     */
    setName: function (name) {
        this.classLabel.setText(name);

        return this;
    }
});