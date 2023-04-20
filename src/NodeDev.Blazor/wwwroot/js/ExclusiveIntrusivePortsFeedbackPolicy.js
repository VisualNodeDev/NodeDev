
draw2d.policy.port.ExclusiveIntrusivePortsFeedbackPolicy = draw2d.policy.port.PortFeedbackPolicy.extend(
    /** @lends draw2d.policy.port.PortFeedbackPolicy.prototype */
    {

        NAME: "draw2d.policy.port.ExclusiveIntrusivePortsFeedbackPolicy",

        /**
         */
        init: function (attr, setter, getter) {
            this._super(attr, setter, getter)
            this.connectionLine = null
            this.tweenable = null
            this.growFactor = 2
        },


        /**
         *
         * Called by the framework if the related shape has init a drag&drop
         * operation
         *
         * @param {draw2d.Canvas} canvas The host canvas
         * @param {draw2d.Figure} figure The related figure
         * @param {Number} x the x-coordinate of the mouse up event
         * @param {Number} y the y-coordinate of the mouse up event
         * @param {Boolean} shiftKey true if the shift key has been pressed during this event
         * @param {Boolean} ctrlKey true if the ctrl key has been pressed during the event
         */
        onDragStart: function (canvas, figure, x, y, shiftKey, ctrlKey) {
            let allPorts = canvas.getAllPorts();

            for (let i = 0; i < allPorts.data.length; ++i) {

                let port = allPorts.data[i];

                let canConnect = port.type === figure.type && port.portType !== figure.portType;
                port.setSemanticGroup(canConnect || figure == port ? 'yes' : 'no');
            }

            return true;
        },

    })
