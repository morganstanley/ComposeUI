/* 
 *  Morgan Stanley makes this available to you under the Apache License,
 *  Version 2.0 (the "License"). You may obtain a copy of the License at
 *       http://www.apache.org/licenses/LICENSE-2.0.
 *  See the NOTICE file distributed with this work for additional information
 *  regarding copyright ownership. Unless required by applicable law or agreed
 *  to in writing, software distributed under the License is distributed on an
 *  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 *  or implied. See the License for the specific language governing permissions
 *  and limitations under the License.
 *  
 */

import { nodeResolve } from '@rollup/plugin-node-resolve';
import typescript from '@rollup/plugin-typescript';
import pkg from "./package.json" with { type: "json" };

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
