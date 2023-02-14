using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Shell
{
    internal class CommandLineArgumentsParser
    {
        private string[] commandLineArguments;

        public CommandLineArgumentsParser(string[] _commandLineArguments)
        {
            commandLineArguments = _commandLineArguments;  
        }
        public async void CreateDynamicWindowWithOptions()
        {
            var titleOption = new Option<string>("--title", description: "Set title for window");
            var urlOption = new Option<string>("--url", description: "Set url for webview. default: about:blank");
            var widthOption = new Option<int>("--width", description: "Set width for window");
            var heightOption = new Option<int>("--height", description: "Set height for window");

            var rootCommand = new RootCommand();

            rootCommand.Add(titleOption);
            rootCommand.Add(urlOption);
            rootCommand.Add(widthOption);
            rootCommand.Add(heightOption);

            rootCommand.SetHandler(
                (window) => {},
                new WindowBinder(titleOption, urlOption, widthOption, heightOption)
            );

            await rootCommand.InvokeAsync(commandLineArguments);
        }
    }
}
