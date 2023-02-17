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
using System.CommandLine;
using System.IO;
using System.Security.Policy;

namespace Shell
{
    /// <summary>
    /// Interaction logic for MainWebWindow.xaml
    /// </summary>
    public partial class MainWebWindow : Window
    {
        public MainWebWindow(MainWebWindowOptions webWindowOptions)
        {
            InitializeComponent();

            Title = webWindowOptions.Title ?? webWindowOptions.DefaultTitle;
            Width = webWindowOptions.Width ?? webWindowOptions.DefaultWidth;
            Height = webWindowOptions.Height ?? webWindowOptions.DefaultHeight;
            webView.Source = new Uri(webWindowOptions.Url ?? webWindowOptions.DefaultUrl);
        }
    }
}