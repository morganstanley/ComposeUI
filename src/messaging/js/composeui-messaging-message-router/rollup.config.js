import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';


export default {
    input: 'output/index.js',
    output: {
        file: 'dist/messageRouterMessaging-iife-bundle.js',
        format: 'iife',
        name: 'messageRouterMessaging'
    },
    plugins: [
        resolve(),
        commonjs()
    ]
};
