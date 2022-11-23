using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;

namespace Manifest
{
    internal class ManifestParser
    {
        internal ManifestModel manifest { get; set; }

        public ManifestParser()
        {
            OpenManifestFile("./Manifest/exampleManifest.json");
        }

        public void OpenManifestFile(string manifestFile)
        {
            using (StreamReader r = new StreamReader(manifestFile))
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
