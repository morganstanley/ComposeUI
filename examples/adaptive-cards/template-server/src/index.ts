// Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
import express, { Request, Response } from "express";
import templateRoutes from "./routes/template";
import cors from "cors";

const app = express();
const PORT = process.env.PORT || 3000;

const allowedOrigins = ["http://localhost:6060", "http://127.0.0.1:6060"];
const options: cors.CorsOptions = {
  origin: allowedOrigins,
};

app.use(cors(options));
app.use(express.json());
app.use("/template", templateRoutes);

app.get("/", (req: Request, res: Response) => {
  res.send("Welcome to the Adaptive Card template API!");
});

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});
