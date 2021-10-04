using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DockManagerCore
{
    public class ContentPaneWrapper : TabItem, IDisposable
    {
        static ContentPaneWrapper()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentPaneWrapper),
                new FrameworkPropertyMetadata(typeof(ContentPaneWrapper)));
        }

        public static readonly RoutedEvent CloseTabEvent =
            EventManager.RegisterRoutedEvent("CloseTab", RoutingStrategy.Direct,
                typeof(RoutedEventHandler), typeof(ContentPaneWrapper));

        public event RoutedEventHandler CloseTab
        {
            add => AddHandler(CloseTabEvent, value);
            remove => RemoveHandler(CloseTabEvent, value);
        }

        public event MouseButtonEventHandler ClickTab;
        private Button closeButton;
        private Border border;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (closeButton != null)
            {
                closeButton.Click -= closeButton_Click; 
            }
            closeButton = GetTemplateChild("PART_Close") as Button;
            if (closeButton != null)
            {
                closeButton.Click += closeButton_Click;
            }


            if (border != null)
            {
                border.MouseDown -= header_Click;
            }
            border = GetTemplateChild("Bd") as Border;
            if (border != null)
            {
                border.MouseDown += header_Click;
            }  
        }


        public bool IsSingleTab
        {
            get => (bool)GetValue(IsSingleTabProperty);
            internal set => SetValue(IsSingleTabPropertyKey, value);
        }

        private static readonly DependencyPropertyKey IsSingleTabPropertyKey
        = DependencyProperty.RegisterReadOnly("IsSingleTab", typeof(bool), typeof(ContentPaneWrapper), new PropertyMetadata(true));

        public static readonly DependencyProperty IsSingleTabProperty
            = IsSingleTabPropertyKey.DependencyProperty;


        void closeButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseTabEvent, this));
        }

        void header_Click(object sender, MouseButtonEventArgs e)
        {
            var copy = ClickTab;
            if (copy != null)
            { 
                copy(this, e);
            }
        }
 
        public ContentPaneWrapper()
        { 
        }

        
        public ContentPaneWrapper(ContentPane pane_)
            : this()
        {
            DockingGrid = new DockingGrid(this, pane_); 
            Content = DockingGrid;
            Pane = pane_;
            Pane.Wrapper = this; 
        }

       
 
        internal DockingGrid DockingGrid { get; private set; }

        public ContentPaneWrapperHost Host
        {
            get => (ContentPaneWrapperHost)GetValue(HostProperty);
            internal set => SetValue(HostPropertyKey, value);
        }

        private static readonly DependencyPropertyKey HostPropertyKey
        = DependencyProperty.RegisterReadOnly("Host", typeof(ContentPaneWrapperHost), typeof(ContentPaneWrapper), new PropertyMetadata(null));

        public static readonly DependencyProperty HostProperty
            = HostPropertyKey.DependencyProperty;
 

        public ContentPane Pane
        {
            get => (ContentPane)GetValue(PaneProperty);
            internal set => SetValue(PanePropertyKey, value);
        }

        private static readonly DependencyPropertyKey PanePropertyKey
        = DependencyProperty.RegisterReadOnly("Pane", typeof(ContentPane), typeof(ContentPaneWrapper), new PropertyMetadata(null, PaneChanged));

        public static readonly DependencyProperty PaneProperty
            = PanePropertyKey.DependencyProperty;

        private static void PaneChanged(DependencyObject dependencyObject_, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs_)
        {
            ContentPaneWrapper wrapper = (ContentPaneWrapper) dependencyObject_;
            if (wrapper != null)
            {
                ContentPane pane = dependencyPropertyChangedEventArgs_.NewValue as ContentPane;
                if (pane != null)
                {
                    pane.Wrapper = wrapper;
                    ContentPaneWrapperHost host = wrapper.Parent as ContentPaneWrapperHost;
                    if (host != null && host.ActiveWrapper == wrapper && host.ParentContainer != null)
                    {
                        host.ParentContainer.ActivePane = pane;
                    }
                }

            }
        } 

        public void Dispose()
        {
            //todo: implement it
            Pane.Dispose();
            DockingGrid.Dispose();

        }
    }
}
