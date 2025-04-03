// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore
import typescript from "@rollup/plugin-typescript";
import json from "@rollup/plugin-json";
import commonjs from '@rollup/plugin-commonjs';
import url from "@rollup/plugin-url";
import { nodeResolve } from "@rollup/plugin-node-resolve";
import nodePolyfills from "rollup-plugin-polyfill-node"
import css from "rollup-plugin-import-css";

const config = [
  {
    external: ['AdaptiveCards', 'ACData', 'AEL', 'markdownit', 'DOMPurify'],
    input: "src/index.ts",
    output: {
      file: "dist/index.js",
      format: "umd",
      name: "window",
      extend: true,
      globals: {
        'adaptivecards': 'AdaptiveCards',
        'adaptivecards-templating': 'ACData',
        'markdown-it': 'markdownit',
        'dompurify': 'DOMPurify',
        'adaptive-expressions': 'AEL'
      }
    },
    plugins: [
      json(),
      url(),
      css(),
      commonjs(),
      nodePolyfills(),
      nodeResolve({ extensions: ['.js', '.ts'] }),
      typescript({ tsconfig: "./tsconfig.json" })
    ],
  }
];

export default config;
