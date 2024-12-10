// @ts-ignore
import typescript from '@rollup/plugin-typescript';
import json from '@rollup/plugin-json';
import dts from 'rollup-plugin-dts';
//import resolve from '@rollup/plugin-node-resolve';
//import commonjs from '@rollup/plugin-commonjs';

 const config = [{
  input: 'src/index.ts',
  output: {
    file: 'dist/index.js',
    format: 'umd',
    name: "toastNotification",
  },
  plugins: [ json(), typescript()]
}, 
 {
    input: 'src/index.ts',
  output: {
    file: 'dist/index.d.ts',
    format: 'umd',
  },
  plugins: [ dts()]
 }];

export default config;