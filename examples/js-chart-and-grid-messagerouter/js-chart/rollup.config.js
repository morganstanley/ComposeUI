import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';



export default {
    input: 'chart.js',
    output: {
        file: 'dist/bundle.js',
        format: 'iife'
        ,name: 'chart'
    },
    plugins: [
        resolve(),
        commonjs()
    ]
};
