<!--- Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
--->
# ComposeUI Adaptive-Cards Example Template Service

This is a small server built with typescript and express that provides adaptive card templates in the form of json to be retrieved by the ComposeUI notification library. It is necessary to run the server in the terminal for the notification library to function as expected.

It exposes two endpoints: 

1. http://localhost:3000/template/list : This end-point is provided to retrieve all of the templates provided by the server.
2. http://localhost:3000/template/name/:id : This end-point is provided to retrieve a specific template based on the template id provided.

To run the server, you can use the following command in the terminal:
```
npx lerna run start --stream --scope=@morgan-stanley/composeui-example-adaptivecards-server
```
