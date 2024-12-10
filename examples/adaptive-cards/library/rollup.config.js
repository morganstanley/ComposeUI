// @ts-ignore
import typescript from '@rollup/plugin-typescript';
import json from '@rollup/plugin-json';
import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';

 const config = [{
  input: 'src/index.ts',
  output: {
    file: 'dist/index.js',
    format: 'iife',
    name: "toastNotification",
    sourcemap: "inline",
    exports: "named",
    inlineDynamicImports: true,
    globals: {
        adaptivecards: 'AdaptiveCards'
    }
  },
  plugins: [ json(), typescript(), resolve(), commonjs()],
  external: ['adaptivecards','markdown-it','sanitize-html'],
}];

export default config;