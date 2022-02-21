/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */
using Microsoft.Extensions.Logging;

namespace ProcessExplorer.Entities.ConfigurationFiles
{
    public enum ExportType
    {
        Open,
        SendViaEmail,
        None
    }

    public abstract class Info
    {
        public string? Name;
        public string? Path;
        public ExportType ExportType = ExportType.None;
        public string? EmailAddress;

        public string? GetName () => Name;
        public string? GetLogPath() => Path;
        public ExportType GetExportType() => ExportType;
        public string? GetEmailAddress() => EmailAddress;

        public void SetName(string name) => Name = name;
        public void SetLogPath(string path) => Path = path;
        public void SetExportType(ExportType exportType) => ExportType = exportType;
        public void SetEmailAddress(string email) => EmailAddress = email;
    }

    public class LogFileInfo : Info
    {
        public LogFileInfo(string name, string logPath, LogLevel logLevel = LogLevel.Debug, ExportType exportType = ExportType.None)
        {
            Name = name;
            Path = logPath;
            LogLevel = logLevel;
            ExportType = exportType;
        }
        public LogLevel LogLevel { get; set; }
    }

    public class ConfigFileInfo : Info
    {
        public ConfigFileInfo(string name, string path, ExportType exportType = ExportType.None)
        {
            Name = name;
            Path = path;
            ExportType = exportType;
        }
    }
    public class CurrentConfigurations : IConfigurationHandler
    {
        public CurrentConfigurations()
        {
            ConfigFiles = new List<ConfigFileInfo>();
            LogFiles = new List<LogFileInfo>();
        }
        public List<ConfigFileInfo> ConfigFiles { get; set; }
        public List<LogFileInfo> LogFiles { get; set; }

    }
}
