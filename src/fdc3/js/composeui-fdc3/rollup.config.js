import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';


export default {
    input: 'output/index.js',
    output: {
        file: 'dist/fdc3-iife-bundle.js',
        format: 'iife',
        name: 'fdc3'
    },
    plugins: [
        resolve(),
        commonjs()
    ]
};
