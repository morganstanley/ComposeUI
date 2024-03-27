import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';


export default {
    input: 'output/index.js',
    output: {
        file: 'dist/connectifi-iife-bundle.js',
        format: 'iife',
        name: 'connectifi'
    },
    plugins: [
        resolve(),
        commonjs()
    ]
};
