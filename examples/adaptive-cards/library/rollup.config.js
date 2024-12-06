// @ts-ignore
import typescript from '@rollup/plugin-typescript';

 const config = [{
  input: 'src/index.ts',
  output: {
    file: 'dist/index.js',
    format: 'cjs',
    sourcemap: "inline",
    exports: "named"
  },
  plugins: [typescript()],
  external: ['adaptivecards'],
}];

export default config;