// @ts-ignore
import typescript from "@rollup/plugin-typescript";
import json from "@rollup/plugin-json";
import dts from "rollup-plugin-dts";
//import resolve from '@rollup/plugin-node-resolve';
import copy from "rollup-plugin-copy";
import url from "@rollup/plugin-url";

const config = [
  {
    input: "src/index.ts",
    output: {
      file: "dist/index.js",
      format: "umd",
      name: "toastNotification",
      sourcemap: "inline",
    },
    plugins: [
      json(),
      typescript(),
      copy({
        targets: [{ src: "src/img/*", dest: "dist/img/" }],
      }),
      url(),
    ],
  },
  {
    input: "src/index.ts",
    output: {
      file: "dist/index.d.ts",
      format: "umd",
    },
    plugins: [dts()],
  },
];

export default config;
