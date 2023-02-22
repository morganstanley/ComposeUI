import { nodeResolve } from '@rollup/plugin-node-resolve';
import typescript from '@rollup/plugin-typescript';
import pkg from "./package.json" assert { type: "json" };

const moduleName = pkg.name.replace(/^@.*\//, "");
const inputFileName = "src/index.ts";
const author = pkg.author;
const banner = `
  /**
   * @license
   * author: ${author}
   * ${moduleName}.js v${pkg.version}
   * Released under the ${pkg.license} license.
   */
`;

const config = [
    {
        input: inputFileName,
        output: [
            {
                file: "dist/esm/index.js",
                format: "es",
                sourcemap: "inline",
                banner: banner,
                exports: "named",
            },
        ],
        external: [
            ...Object.keys(pkg.dependencies || {}),
            ...Object.keys(pkg.devDependencies || {}),
        ],
        plugins: [
            typescript({tsconfig: "./tsconfig.build.json"}),
            nodeResolve({
                browser: false,
            }),
        ],
    }
];

export default config;
