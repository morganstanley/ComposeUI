using System.CommandLine;
using System.CommandLine.Parsing;

namespace Shell
{
    internal static class WebWindowOptionsParser
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

        public static WebWindowOptions Parse(string[] args)
        {
            Parser parser = new Parser(rootCommand);
            ParseResult parseResult = parser.Parse(args);

            WebWindowOptions options = new WebWindowOptions
            {
                Title = parseResult.GetValueForOption(titleOption),
                Url = parseResult.GetValueForOption(urlOption),
                Width = parseResult.GetValueForOption(widthOption),
                Height = parseResult.GetValueForOption(heightOption)
            };

            return options;
        }
    }
}
