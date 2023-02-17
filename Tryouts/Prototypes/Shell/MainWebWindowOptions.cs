using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell
{
    public sealed class MainWebWindowOptions
    {
        public double? Height { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public double? Width { get; set; }

        public const double DefaultHeight = 450d;
        public const string? DefaultTitle = "Compose Web Container";
        public const string? DefaultUrl = "about:blank";
        public const double DefaultWidth = 800d;
    }
}
