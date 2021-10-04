using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DockManagerCore.Desktop
{
    public class HeaderItemsHolder : UserControl
    {
        public HeaderItemsHolder()
        {
            IsVisibleChanged += OnIsVisibleChanged;
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e_)
        {
            e_.Handled = true;
            base.OnMouseDoubleClick(e_);
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //if visibility of the holder changes to false, stop showing header controls in it and show them in the next available candidate
            //if it changes to true, hide header controls in other places and show them here

            var itemsSource = ItemsSource as HeaderItemsCollection;

            if (e.NewValue.Equals(true))
            {
                if (itemsSource != null)
                {
                    if (!itemsSource.IsCurrentParent(this))
                    {
                        itemsSource.SetCurrentParent(this);
                    }
                }
            }
            else
            {
                if (itemsSource != null)
                {
                    if (itemsSource.IsCurrentParent(this))
                    {
                        itemsSource.ReleaseCurrentParent();
                        itemsSource.SetCurrentParent();
                    }
                }
            }
        }

        //private HeaderItemsCollection m_itemsSource;
        //I'm assuming that only HeaderItemsCollections are assigned. I left this property IEnumerable to keep HeaderItemsCollection internal.
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }



        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(HeaderItemsHolder), new UIPropertyMetadata(null, ItemsSourcePropertyChanged));

        private static void ItemsSourcePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as HeaderItemsCollection;
            var newValue = e.NewValue as HeaderItemsCollection;
            var instance = dependencyObject as HeaderItemsHolder;

            if (oldValue == null && newValue == null)
            {
                return;
            }

            if (oldValue != newValue)
            {
                if (oldValue != null)
                {
                    oldValue.RemoveParent(instance);
                }

                if (newValue == null)
                {
                    return;
                }

                newValue.AddNewParent(instance);
            }

            if (!newValue.IsCurrentParent(instance))
            {
                newValue.SetCurrentParent(instance);
            }
        }


        public void ShowItems()
        {
            var itemsSource = ItemsSource as HeaderItemsCollection;

            if (itemsSource != null)
            {
                Content = itemsSource.Control;
            }
        }

        public void HideItems()
        {
            if (Content is FrameworkElement)
            {
                //this allows header items width to be set correctly after reshowing
                Content = null;// new Label { Width = ((FrameworkElement)Content).ActualWidth };
            }
            else
            {
                Content = null;
            }
        }
    } 

}
