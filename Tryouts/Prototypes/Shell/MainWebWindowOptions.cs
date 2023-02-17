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

        public double DefaultHeight { get; set; } = 450d;
        public string? DefaultTitle { get; set; } = "Compose Web Container";
        public string? DefaultUrl { get; set; } = "about:blank";
        public double DefaultWidth { get; set; } = 800d;
    }
}
