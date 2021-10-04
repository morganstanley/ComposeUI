

using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DockManagerCore;
using DockManagerCore.Desktop;
using DockManagerCore.Services;
using Microsoft.VisualBasic;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace DockManagerCoreExample
{
    /// <summary>
    /// Interaction logic for SelectorWindow.xaml
    /// </summary>
    public partial class SelectorWindow : Window
    {

        static int dogCount = 1;
        static int benderCount = 1;
        private static int comboCount = 1;
        private static int treeCount = 1;
        private static int graphCount = 1;
        private static int listCount = 1;

        private Window win;

        public static string LayoutStorage = @"D:\MSDE\dev";

        public SelectorWindow()
        {

            InitializeComponent();
            //ConsoleManager.Show();

            win = new BarButton(this);
            win.Topmost = true;
            /*win.Left = Screen.AllScreens[1].WorkingArea.Left;*/
            win.Left = 0;
            LocationChanged += SelectorWindow_LocationChanged;
            Show();

            win.Top = 300;
            win.Show();
            Hide();
            Windows = new ObservableCollection<WindowViewModel>();
            var contentPaneFactory = new ContentPaneFactory();
            contentPaneFactory.ItemsSource = Windows;
        }

        void SelectorWindow_LocationChanged(object sender, EventArgs e)
        {
            Rectangle rect = Screen.AllScreens[0].WorkingArea;

            /*Left = rect.Left;*/
            Top = 300;
            Left = 0;
            Width = rect.Width;
            Height = 100;

        }

        protected virtual void OnSelectorBarClick(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }


        protected override void OnMouseLeave(MouseEventArgs e)
        {

            Hide();
            win.Top = 300;
            win.Show();
        }
  

        private void dogButton_Click(object sender, RoutedEventArgs e)
        { 
            
            if (paneFactoryMode)
            {
                var model =
                    new WindowViewModel(new InitialWindowParameters {SizingMethod = SizingMethod.Custom, Height = 500, Width = 500})
                        {
                            Content = new DogWindow(),
                            Title = "DogWindow" + dogCount++,
                            
                        };
                Windows.Add(model);
            }
            else
            {
                var pane = new ContentPane
                {
                        Caption = "DogWindow" + dogCount++,
                        Content = new DogWindow(),
                        SizeToContent = SizingMethod.Custom,
                        PaneHeight = 200,
                        PaneWidth = 200
                    };
                PaneContainer dogPaneContainer = new PaneContainer(pane);
                dogPaneContainer.Show();
                Window w = GetWindow(pane);
                //w.MinHeight = 20;
                //w.MinWidth = 20;
                dogPaneContainer.MinHeight = 50;
                dogPaneContainer.MinWidth = 50; 
            }
        }

        private void benderButton_Click(object sender, RoutedEventArgs e)
        {
            if (paneFactoryMode)
            {
                var model =
                    new WindowViewModel(new InitialWindowParameters())
                    {
                        Content = new BenderWindow(),
                        Title = "BenderWindow" + benderCount++,
                        //Icon = IconImageSourceConverter.UriToIcon(new Uri("/DockManagerCoreExample;component/bender.jpg", UriKind.RelativeOrAbsolute))
                    };
                Windows.Add(model);
            }
            else
            {
                PaneContainer benderPaneContainer = new PaneContainer(new ContentPane
                {
                    Caption = "BenderWindow" + benderCount++,
                    Content = new BenderWindow(),
                    Icon = new BitmapImage(new Uri("/DockManagerCoreExample;component/bender.jpg", UriKind.RelativeOrAbsolute))
                });
                benderPaneContainer.Show(); 
            }


        }

        private void comboButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (paneFactoryMode)
            {
                var model =
                    new WindowViewModel(new InitialWindowParameters())
                    {
                        Content = new DemoApp1(),
                        Title = "ComboBox" + comboCount++, 
                    };
                model.HeaderItems.Add(new TextBox { Text = "Test text", Margin = new Thickness(2, 0, 2, 0) });

                var button = new Button
                {
                    Content = "Close",
                    Margin = new Thickness(2, 0, 2, 0)
                };
                //button.Click += (sender_, args_) => contentPane.ExecuteCommand(ContentPaneCommands.Close);
                model.HeaderItems.Add(button);

                button = new Button
                {
                    Content = "Rename",
                    Margin = new Thickness(2, 0, 2, 0)
                };
               // button.Click += (sender_, args_) => contentPane.ExecuteCommand(ContentPaneCommands.Rename);

                
                model.HeaderItems.Add(button);
                Windows.Add(model);
            }
            else
            {
                var contentPane = new ContentPane { Caption = "ComboBox" + comboCount++, Content = new DemoApp1() }; 
                PaneContainer paneContainerApp1 = new PaneContainer(contentPane);
                contentPane.CustomItems.Add(new TextBox { Text = "Test text", Margin = new Thickness(2, 0, 2, 0) });

                var button = new Button
                {
                    Content = "Close",
                    Margin = new Thickness(2, 0, 2, 0)
                };
                button.Click += (sender_, args_) => contentPane.ExecuteCommand(ContentPaneCommands.Close);
                contentPane.CustomItems.Add(button);

                button = new Button
                {
                    Content = "Rename",
                    Margin = new Thickness(2, 0, 2, 0)
                };
                button.Click += (sender_, args_) => contentPane.ExecuteCommand(ContentPaneCommands.Rename);

                contentPane.CustomItems.Add(button);
                button = new Button
                {
                    Content = "Change Size",
                    Margin = new Thickness(2, 0, 2, 0)
                };
                button.Click += (sender_, args_) =>
                    {
                        paneContainerApp1.Width = 200;
                        paneContainerApp1.Height = 200;
                    };
                 
                contentPane.CustomItems.Add(button);

                button = new Button
                {
                    Content = "Invalid Change Size",
                    Margin = new Thickness(2, 0, 2, 0)
                };
                button.Click += (sender_, args_) =>
                {
                    paneContainerApp1.Width = 20;
                    paneContainerApp1.Height = 20;
                };

                contentPane.CustomItems.Add(button);

                paneContainerApp1.MinWidth = 100;
                paneContainerApp1.MinHeight = 100;
                paneContainerApp1.Show(); 
            }
           
        }
  

        private void graphButton_Click(object sender, RoutedEventArgs e)
        {
            if (paneFactoryMode)
            {
                var model =
                    new WindowViewModel(new InitialWindowParameters())
                    {
                        Content = new DemoApp2(),
                        Title = "Graph" + graphCount++, 
                    };
                model.HeaderItems.Add(new CheckBox { IsChecked = true });
                Windows.Add(model);
            }
            else
            {
                var contentPane = new ContentPane { Caption = "Graph" + graphCount++, Content = new DemoApp2() };
                contentPane.CustomItems.Add(new CheckBox { IsChecked = true });
                PaneContainer paneContainerApp2 = new PaneContainer(contentPane);
                paneContainerApp2.Show(); 
            }

        }

        private void treeButton_Click(object sender, RoutedEventArgs e)
        {
            if (paneFactoryMode)
            {
                var model =
                    new WindowViewModel(new InitialWindowParameters())
                    {
                        Content = new DemoApp3(),
                        Title = "Trees" + treeCount++,
                    }; 
                Windows.Add(model);
            }
            else
            {
                PaneContainer paneContainerApp3 = new PaneContainer(new ContentPane { Caption = "Trees" + treeCount++, Content = new DemoApp3() });
                paneContainerApp3.Show(); 
            }

        }

        private void listButton_Click(object sender, RoutedEventArgs e)
        {
            if (paneFactoryMode)
            {
                var model =
                    new WindowViewModel(new InitialWindowParameters())
                        {
                            Content = new DemoApp4(),
                            Title = "Lists" + listCount++,
                        };
                Windows.Add(model);
            }
            else
            {
                 PaneContainer paneContainerApp4 = new PaneContainer(new ContentPane { Caption = "Lists" + listCount++, Content = new DemoApp4() });
                paneContainerApp4.Show(); 
            }

        }

        private void windowsFormButton_Click(object sender, RoutedEventArgs e)
        {
            var form = new Form1 {TopLevel = false, FormBorderStyle = FormBorderStyle.None};
            var formHoster = new WindowsFormsHost {Child = form};
            var dp = new DockPanel { LastChildFill = true, Width = form.ClientSize.Width , Height = form.ClientSize.Height};
            dp.Children.Add(formHoster);

            if (paneFactoryMode)
            {
                var model =
                    new WindowViewModel(new InitialWindowParameters())
                    {
                        Content =dp,
                        Title = "Lists" + listCount++,
                    };
                Windows.Add(model);
            }
            else
            {
                PaneContainer paneContainerApp4 = new PaneContainer(new ContentPane { Caption = "Lists" + listCount++, Content = dp });
                paneContainerApp4.Show();
            }
        }

        
        private void BtnExecuteCommand_OnClick(object sender_, RoutedEventArgs e_)
        {
            ContentPaneInfo info = cboPanes.SelectedItem as ContentPaneInfo;
            if (info == null) return;
            RoutedCommand command = cboPaneCommands.SelectedItem as RoutedCommand;
            if (command == null) return;
            info.Pane.ExecuteCommand(command);
        }


        private void BtnExecuteCommand2_OnClick(object sender_, RoutedEventArgs e_)
        {
            PaneContainerInfo info = cboContainers.SelectedItem as PaneContainerInfo;
            if (info == null) return;
            RoutedCommand command = cboContainerCommands.SelectedItem as RoutedCommand;
            if (command == null) return;
            info.Container.ExecuteCommand(command);
        }

        private void CboPanes_OnDropDownOpened(object sender, EventArgs e)
        {
            cboPanes.Items.Clear();
            foreach (ContentPane pane in DockManager.GetAllPanes())
            {
                cboPanes.Items.Add(new ContentPaneInfo(pane));
            } 
        }
        private void CboContainers_OnDropDownOpened(object sender, EventArgs e)
        {
            cboContainers.Items.Clear();
            foreach (PaneContainer containers in DockManager.GetAllContainers())
            {
                cboContainers.Items.Add(new PaneContainerInfo(containers));
            }
        }

        private bool paneFactoryMode; 
        private void FlagUseContentPane_OnChecked(object sender_, RoutedEventArgs e_)
        {
            paneFactoryMode = flagUseContentPane.IsChecked.Value;

        }



        public ObservableCollection<WindowViewModel> Windows
        {
            get => (ObservableCollection<WindowViewModel>)GetValue(WindowsProperty);
            set => SetValue(WindowsProperty, value);
        }

        // Using a DependencyProperty as the backing store for Windows.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowsProperty =
            DependencyProperty.Register("Windows", typeof(ObservableCollection<WindowViewModel>), typeof(SelectorWindow), new PropertyMetadata(null));

        private void BtnSaveLayout_OnClick(object sender, RoutedEventArgs e)
        {
            string layoutName = Interaction.InputBox("Please input layout name to save:", "Save Layout");
            if (!string.IsNullOrEmpty(layoutName))
            {
                string fileName = Path.Combine(LayoutStorage, layoutName + ".xml");
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                using (FileStream f = new FileStream(fileName, FileMode.Create))
                {
                    LayoutManager.SaveLayout(f);
                }
            }
        }
         
        private void BtnLoadLayout_OnClick(object sender, RoutedEventArgs e)
        {
            string layoutName = Interaction.InputBox("Please input layout name to load", "Load Layout");
            if (!string.IsNullOrEmpty(layoutName))
            {
                string fileName = Path.Combine(LayoutStorage, layoutName + ".xml");
                if (!File.Exists(fileName))
                {
                    MessageBox.Show("Invalid layout name", "Load Layout", MessageBoxButton.OK,
                                                   MessageBoxImage.Error);
                    return;
                }
                using (FileStream f = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    LayoutManager.LoadLayout(f);
                }
            }
        }

    }

    internal class ContentPaneInfo
    {
        public ContentPane Pane { get; private set; }
        public ContentPaneInfo(ContentPane pane_)
        {
            Pane = pane_;
        }

        public override string ToString()
        {
            return Pane.ToString();
        }
    }

    internal class PaneContainerInfo
    {
        public PaneContainer Container { get; private set; }
        public PaneContainerInfo(PaneContainer container_)
        {
            Container = container_;
        }

        public override string ToString()
        {
            return Container.ToString();
        }
    }
}
