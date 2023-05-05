module.exports = {
    entry: [
        './js/index.js'
    ],
    mode: 'development',
    output: {
        sourceMapFilename: "NodeDev.min.js.map",
        path: __dirname + "/wwwroot/dist",
        publicPath: '/',
        filename: 'NodeDev.min.js'
    },
    module: {
        rules: [
            {
                test: /\.css$/i,
                use: ["style-loader", "css-loader"],
            }, {
                test: /\.map/i,
                use: ["source-map-loader"]
            }, {
                test: /\.js$/,
                use: ["source-map-loader"],
                enforce: "pre"
            }
        ]
    },
    resolve: {
        extensions: ['', '.js', '.jsx', '.css', '.map', '.tsx']
    },
    devtool: 'source-map',
    devServer: {
        historyApiFallback: true,
        contentBase: './'
    }
};