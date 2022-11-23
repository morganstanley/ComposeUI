using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Shell
{
    [Serializable]
    internal sealed class ManifestModel
    {
        public ModuleModel[]? Modules { get; set; }

        public static JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}



