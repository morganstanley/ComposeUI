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
    internal class MainWebWindowOptionsParser
    {
        private Option<string> titleOption = new Option<string>("--title", description: "Set title for window");
        private Option<string> urlOption = new Option<string>("--url", description: "Set url for webview. default: about:blank");
        private Option<int> widthOption = new Option<int>("--width", description: "Set width for window");
        private Option<int> heightOption = new Option<int>("--height", description: "Set height for window");
        private RootCommand rootCommand = new RootCommand();

        public MainWebWindowOptionsParser() {
            rootCommand.Add(titleOption);
            rootCommand.Add(urlOption);
            rootCommand.Add(widthOption);
            rootCommand.Add(heightOption);
        }

        public MainWebWindowOptions Parse(string[] args)
        {
            Parser parser = new Parser(rootCommand);
            ParseResult parseResult = parser.Parse(args);

            MainWebWindowOptions options = new MainWebWindowOptions
            {
                Title = parseResult.GetValueForOption(titleOption) ?? "Compose Web Container",
                Url = parseResult.GetValueForOption(urlOption) ?? "about:blank",
                Width = parseResult.GetValueForOption(widthOption),
                Height = parseResult.GetValueForOption(heightOption)
            };

            return options;
        }
    }
}
