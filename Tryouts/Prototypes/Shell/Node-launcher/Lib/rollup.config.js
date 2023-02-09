import { nodeResolve } from '@rollup/plugin-node-resolve';
import typescript from '@rollup/plugin-typescript';

export default {
    input: 'src/index.ts',
    output: {
      
        file: 'output/bundle.mjs',
        format: 'es'
    },
    plugins: [typescript(), nodeResolve()]
};