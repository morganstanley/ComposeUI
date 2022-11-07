// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using LocalCollector.Communicator;
using Microsoft.Extensions.Logging;
using ModuleProcessMonitor.Processes;

namespace ProcessExplorer.Factories;

public static class ProcessAggregatorFactory
{
    public static ICommunicator CreateCommunicator(IProcessInfoAggregator processAggregator)
    {
        return new Infrastructure.Communicator(processAggregator);
    }

    public static IProcessInfoAggregator CreateProcessInfoAggregator(ILogger<IProcessInfoAggregator> logger, IProcessMonitor? processMonitor)
    {
        return new ProcessInfoAggregator(logger, processMonitor);
    }

    public static IProcessInfoAggregator CreateProcessInfoAggregator(ILogger<IProcessInfoAggregator> logger)
    {
        return CreateProcessInfoAggregator(logger, null);
    }
}
