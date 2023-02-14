using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Shell
{
    /// <summary>
    /// Interaction logic for MainWebWindow.xaml
    /// </summary>
    public partial class MainWebWindow : Window
    {
        private string[] commandLineArguments = (Application.Current as App).CommandLineArguments;
        private Dictionary<string, string> commands = new Dictionary<string, string>();

        public MainWebWindow()
        {
            InitializeComponent();

            ParsingCommandLineArguments();
            ConfigureWindow();
        }

        private void ParsingCommandLineArguments()
        {
            Array.ForEach(this.commandLineArguments, item => {
                item = item.TrimStart('-');
                string[] command = item.Split("=");

                commands.Add(command[0], command[1]);
            });
        }

        private void ConfigureWindow()
        {
            string commandLineURL;

            if (commands.ContainsKey("url"))
            {
                commandLineURL = commands["url"];
            }
            else
            {
                commandLineURL = "about:blank";
            }
            webView.Source = new Uri(commandLineURL);

            if (commands.ContainsKey("width"))
            {
                Width = int.Parse(commands["width"]);
            }

            if (commands.ContainsKey("height"))
            {
                Height = int.Parse(commands["height"]);
            }

            if (commands.ContainsKey("title"))
            {
                Title = commands["title"];
            }
        }
    }
}
