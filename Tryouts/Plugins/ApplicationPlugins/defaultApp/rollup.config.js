import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';



export default {
    input: 'app.js',
    output: {
        file: 'dist/bundle.js',
        format: 'iife',
        name: 'defaultApp'
    },
    plugins: [
        resolve(),
        commonjs()
    ]
};