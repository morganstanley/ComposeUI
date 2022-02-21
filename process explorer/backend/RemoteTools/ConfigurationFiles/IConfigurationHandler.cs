/* Morgan Stanley makes this available to you under the Apache License, Version 2.0 (the "License"). You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. See the NOTICE file distributed with this work for additional information regarding copyright ownership. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License. */

using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace ProcessExplorer.Entities.ConfigurationFiles
{
    public interface IConfigurationHandler
    {
        public virtual void AddConfig(CurrentConfigurations configs, ConfigFileInfo configFile) => configs.ConfigFiles.Add(configFile);
        public virtual void RemoveConfig(CurrentConfigurations configs, ConfigFileInfo configFile) => configs.ConfigFiles.Remove(configFile);
        public virtual void AddLog(CurrentConfigurations configs, LogFileInfo logFile) => configs.LogFiles.Add(logFile);
        public virtual void RemoveLog(CurrentConfigurations configs, LogFileInfo logFile) => configs.LogFiles.Remove(logFile);
        public virtual void UpdateLoglevel(CurrentConfigurations configs, LogFileInfo logFile, LogLevel logLevel)
        {
            var index = configs.LogFiles.FindIndex(index => index.LogLevel.Equals(logLevel));
            if (index >= -1)
                configs.LogFiles[index].LogLevel = logLevel;
        }
        public virtual void UpdateLoglevel(CurrentConfigurations configs, LogLevel logLevel) 
            => configs.LogFiles = configs.LogFiles.Select(log => { log.LogLevel = logLevel; return log; }).ToList();
        public virtual void ChangeLogFileExportType(CurrentConfigurations configs, LogFileInfo logFile, ExportType export)
            => configs.LogFiles = configs.LogFiles.Select( log => { log.ExportType = export; return log; }).ToList();
        public virtual void ChangeConfigFileExportType(CurrentConfigurations configs, ConfigFileInfo configFile, ExportType export)
            => configs.ConfigFiles = configs.ConfigFiles.Select(config => { config.ExportType = export; return config; }).ToList();
        public virtual void SendEmailWithConfigsAndLogs(CurrentConfigurations configs, SmtpClient client, string from, string recipent)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(from);
                mail.To.Add(recipent);
                mail.Subject = "Files";
                mail.Body = string.Format("Please see attached the config file \n \n Best regards, {0}", Environment.UserDomainName);
                foreach (var item in configs.ConfigFiles)
                {
                    if (item.ExportType == ExportType.SendViaEmail)
                    {
                        mail.Attachments.Add(new Attachment(string.Format("{0}/{1}", item.Path, item.Name)));
                    }
                }
                foreach (var item in configs.LogFiles)
                {
                    if (item.ExportType == ExportType.SendViaEmail)
                    {
                        mail.Attachments.Add(new Attachment(string.Format("{0}/{1}", item.Path, item.Name)));
                    }
                }
                client.EnableSsl = true;
                client.Port = 587;
                client.Send(mail);
            }
            catch (Exception)
            {
                throw new Exception(nameof(IConfigurationHandler));
            }
        }
    }
}
