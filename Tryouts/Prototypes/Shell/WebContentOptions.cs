using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell
{
    public sealed class WebContentOptions
    {
        public string Title = string.Empty;
        public Uri Uri { get; set; } = new Uri("about:blank");
        public Uri? IconUri { get; set; } = null;
    }
}
