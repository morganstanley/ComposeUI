import resolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import css from "rollup-plugin-import-css";



export default {
    input: 'chat.js',
    output: {
        file: 'dist/bundle.js',
        format: 'esm',
        name: 'chat'
    },
    plugins: [
        css({output: "bundle.css"}),
        resolve(),
        commonjs()
    ]
};
