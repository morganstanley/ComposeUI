import typescript from '@rollup/plugin-typescript';

export default {
  input: 'src/index.ts',
  output: {
    dir: 'output',
    format: 'cjs',
    name: "composeMessaging"
  },
  plugins: [typescript()]
};