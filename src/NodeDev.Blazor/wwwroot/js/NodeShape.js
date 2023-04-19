
NodeShape = draw2d.shape.layout.VerticalLayout.extend({

    NAME: "NodeShape",

    init: function (attr) {
        this._super($.extend({ bgColor: "#dbddde", color: "#d7d7d7", stroke: 1, radius: 3 }, attr));

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
            this.addEntity(attr.inputs[i].name, true);

        for (let i = 0; i < attr.outputs.length; ++i)
            this.addEntity(attr.outputs[i].name, false);

    },


    /**
     * @method
     * Add an entity to the db shape
     * 
     * @param {String} txt the label to show
     * @param {Number} [optionalIndex] index where to insert the entity
     */
    addEntity: function (txt, isInput) {
        var label = new draw2d.shape.basic.Label({
            text: txt,
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

        port.setName(portType + '_' + txt);

        this.add(label);
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