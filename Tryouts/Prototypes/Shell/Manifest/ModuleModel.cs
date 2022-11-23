using System;
using System.Collections.Generic;
using System.Text;

namespace Manifest
{
    [Serializable]
    internal sealed class ModuleModel
    {
        public string AppName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
