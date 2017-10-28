var webpack = require('webpack');
var HtmlWebpackPlugin = require('html-webpack-plugin');
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var helpers = require('./helpers');

module.exports = {
	entry: {
		'polyfills': './wwwsource/polyfills.ts',
		'vendor': './wwwsource/vendor.ts',
		'app': './wwwsource/omc.app/main.ts'
	},

	resolve: {
		extensions: ['', '.ts', '.js']
	},

	module: {
		//rules: [
		//	{
		//		test: /\.less$/,
		//		use: [
		//		  'style-loader',
		//		  { loader: 'css-loader', options: { importLoaders: 1 } },
		//		  'less-loader'
		//	]}
		//],
		loaders: [
			{
				test: /\.ts$/,
				loaders: ['awesome-typescript-loader', 'angular2-template-loader']
			},
			{
				test: /\.html$/,
				loader: 'html'
			},
			{
				test: /\.(png|jpe?g|gif|svg|woff|woff2|ttf|eot|ico)$/,
				loader: 'file?name=assets/[name].[hash].[ext]'
			},
			{
				test: /\.less$/,
				loader: 'style!css!less'
			},
			{
				test: /\.css$/,
				exclude: helpers.root('wwwsource', 'omc.app'),
				loader: ExtractTextPlugin.extract('style', 'css?sourceMap')
			},
			{
				test: /\.css$/,
				include: helpers.root('wwwsource', 'omc.app'),
				loader: 'raw'
			}
		]
	},

	plugins: [
		new webpack.optimize.CommonsChunkPlugin({
			name: ['app', 'vendor', 'polyfills']
		}),

		new HtmlWebpackPlugin({
			template: 'wwwsource/index.html'
		})
	]
};
