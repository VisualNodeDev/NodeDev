module.exports = {
    entry: [
        './js/index.jsx'
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
                use: ["map-loader"]
            },
        ]
    },
    resolve: {
        extensions: ['', '.js', '.jsx', '.css', '.map']
    },
    devtool: 'source-map',
    devServer: {
        historyApiFallback: true,
        contentBase: './'
    }
};