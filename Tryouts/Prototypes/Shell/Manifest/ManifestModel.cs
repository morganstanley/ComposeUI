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
        public string AppName { get; set; } = string.Empty; //set?
        public string Url { get; set; }  = string.Empty; //set?


        public static JsonSerializerOptions JsonSerializerOptions = new()
        {
            Converters =
            {
                new JsonStringEnumConverter()
            },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}



