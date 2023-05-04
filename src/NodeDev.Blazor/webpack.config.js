module.exports = {
    entry: [
        './js/index.js'
    ],
    output: {
        path: __dirname + "/wwwroot/dist",
        publicPath: '/',
        filename: 'NodeDev.min.js'
    },
    module: {
        rules: [
            {
                test: /\.css$/i,
                use: ["style-loader", "css-loader"],
            },
        ]
    },
    resolve: {
        extensions: ['', '.js', '.jsx', '.css']
    },
    devServer: {
        historyApiFallback: true,
        contentBase: './'
    }
};