import { nodeResolve } from '@rollup/plugin-node-resolve';

export default {
    input: 'example.js',
    output: {
        file: 'output/bundle.js',
        format: 'es'
    },
    plugins: [
        nodeResolve()
        
    ]
};