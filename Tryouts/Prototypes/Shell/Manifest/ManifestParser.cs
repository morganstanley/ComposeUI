using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;

namespace Shell //todo check nmespace
{
    internal class ManifestParser
    {
        internal ManifestModel manifest { get; set; }

        public ManifestParser()
        {
            OpenManifestFile();
        }

        public void OpenManifestFile()
        {
            using (StreamReader r = new StreamReader("./Manifest/exampleManifest.json"))
            {
                try
                {
                    string fileContent = r.ReadToEnd();
                    manifest = JsonSerializer.Deserialize<ManifestModel>(fileContent, ManifestModel.JsonSerializerOptions);
                }
                catch (Exception e)
                {

                    throw;
                }
                
                r.Close();
            }
        }
    }
}
