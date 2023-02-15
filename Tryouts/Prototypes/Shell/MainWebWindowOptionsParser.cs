using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Shell
{
    internal static class MainWebWindowOptionsParser
    {
        private static Option<string?> titleOption = new Option<string?>("--title", description: "Set title for window");
        private static Option<string?> urlOption = new Option<string?>("--url", description: "Set url for webview. default: about:blank");
        private static Option<double?> widthOption = new Option<double?>("--width", description: "Set width for window");
        private static Option<double?> heightOption = new Option<double?>("--height", description: "Set height for window");
        private static RootCommand rootCommand = new RootCommand
        {
            titleOption,
            urlOption,
            widthOption,
            heightOption
        };

        public static MainWebWindowOptions Parse(string[] args)
        {
            Parser parser = new Parser(rootCommand);
            ParseResult parseResult = parser.Parse(args);

            MainWebWindowOptions options = new MainWebWindowOptions
            {
                Title = parseResult.GetValueForOption(titleOption) ?? "Compose Web Container",
                Url = parseResult.GetValueForOption(urlOption) ?? "about:blank",
                Width = parseResult.GetValueForOption(widthOption) ?? 800d,
                Height = parseResult.GetValueForOption(heightOption) ?? 450d
            };

            return options;
        }
    }
}
