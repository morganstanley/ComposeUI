// @ts-ignore
import typescript from '@rollup/plugin-typescript';
import json from '@rollup/plugin-json';


 const config = [{
  input: 'src/index.ts',
  output: {
    file: 'dist/index.js',
    format: 'iife',
    //sourcemap: "inline",
    exports: "named"
  },
  plugins: [json(),typescript()],
  external: ['adaptivecards'],
}];

export default config;