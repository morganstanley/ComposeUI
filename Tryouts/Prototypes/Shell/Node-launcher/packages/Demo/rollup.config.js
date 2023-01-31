import { nodeResolve } from '@rollup/plugin-node-resolve';

export default {
    input: 'example.js',
    output: {
        file: 'output/bundle.umd.js',
        format: 'umd',
        name: "demo"
    },
    plugins: [
        nodeResolve()
        
    ]
};