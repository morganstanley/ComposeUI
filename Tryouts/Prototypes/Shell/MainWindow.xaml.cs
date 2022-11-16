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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<WebContent> webContentList = new List<WebContent>();

        public MainWindow()
        {
            InitializeComponent();
            addressBar.Text = "http://";
        }

        private void ShowChild_Click(object sender, RoutedEventArgs e)
        {
            var webContent = new WebContent(addressBar.Text);
            webContent.Owner = this;
            webContentList.Add(webContent);
            
            webContent.Show();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            webContentList.ForEach(window => window.Close());
            webContentList.Clear();

            base.OnClosing(e);
        }
    }
}
