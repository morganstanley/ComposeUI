using System;
using System.Windows;
using System.Windows.Data;
using DockManagerCore.Desktop;

namespace DockManagerCore.Services
{
    public class ContentPaneFactory:PaneFactory
    {  
        static ContentPaneFactory()
        {
            ContainerTypeProperty.OverrideMetadata(typeof(ContentPaneFactory), new FrameworkPropertyMetadata(typeof(ContentPane)));
            DockManager.ActivePaneChanged += OnActivePaneChanged;
            DockManager.FloatingWindowLoaded += OnFloatingWindowLoaded;
        }

        private static void OnFloatingWindowLoaded(object sender_, FloatingWindowLoadedEventArgs floatingWindowLoadedEventArgs_)
        { 
            //todo:restrict size
        }

        private static void OnActivePaneChanged(object sender_, ActivePaneChangedEventArgs activePaneChangedEventArgs_)
        {
            HandleActivePaneChanged(sender_, activePaneChangedEventArgs_.OldPane, activePaneChangedEventArgs_.NewPane);
        }
         
        protected override void OnItemInserted(DependencyObject container, object item, int index)
        {
            AddPane((ContentPane)container);
        }


        protected override void OnItemMoved(DependencyObject container, object item, int oldIndex, int newIndex)
        { 
        }

        protected override void OnItemRemoved(DependencyObject container, object oldItem)
        {
            RemovePane((ContentPane)container);
        }

        /// <summary>
        /// Invoked when the <see cref="ContainerFactory.ContainerType"/> is about to be changed to determine if the specified type is allowed.
        /// </summary>
        /// <param name="elementType_">The new element type</param>
        protected sealed override void ValidateContainerType(Type elementType_)
        {
            if (!typeof(ContentPane).IsAssignableFrom(elementType_))
                throw new ArgumentException("ContainerType must be a ContentPane or a derived class.");

            base.ValidateContainerType(elementType_);
        }
         

        private void RemovePane(ContentPane contentPane)
        {
            contentPane.ExecuteCommand(ContentPaneCommands.Close);
        }


        private void AddPane(ContentPane contentPane)
        {
            WindowViewModel model = contentPane.DataContext as WindowViewModel;
            if (model != null)
            {
                contentPane.Name = model.ID;
                contentPane.SizeToContent = model.InitialParameters.SizingMethod;
                contentPane.SetBinding(ContentPane.IconProperty, new Binding("Icon") { Converter = new IconImageSourceConverter(), Mode = BindingMode.OneTime });
                contentPane.SetBinding(ContentPane.CaptionProperty, new Binding("Title"));
                contentPane.SetBinding(ContentPane.ContentProperty, new Binding("Content") { Mode = BindingMode.OneWay });
                contentPane.CustomItems = model.HeaderItems;
                if (contentPane.SizeToContent == SizingMethod.Custom)
                {
                    contentPane.PaneHeight = model.InitialParameters.Height;
                    contentPane.PaneWidth = model.InitialParameters.Width;
                    contentPane.ClearValue(FrameworkElement.HeightProperty);
                    contentPane.ClearValue(FrameworkElement.WidthProperty);
                }
                var floatingWindow = new FloatingWindow(new PaneContainer(contentPane));
                floatingWindow.Show();
            }
        }
    }
}
