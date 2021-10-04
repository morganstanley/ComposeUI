using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace DockManagerCore.Desktop
{
    /// <summary>
    /// Base class used to generate instances of objects of a specified type (<see cref="ContainerType"/>) based on a given source collection of items (<see cref="ContainerFactoryBase.ItemsSource"/>).
    /// </summary>
    public abstract class ContainerFactory : ContainerFactoryBase
    {
        /// <summary>
        /// Used to apply a style to the container for an item
        /// </summary>
        /// <param name="container_">The container associated with the item</param>
        /// <param name="item_">The item from the source collection</param>
        protected override void ApplyItemContainerStyle(DependencyObject container_, object item_)
        {
            Style style = ContainerStyle;

            if (null == style && ContainerStyleSelector != null)
                style = ContainerStyleSelector.SelectStyle(item_, container_);

            if (null != style)
            {
                container_.SetValue(AppliedStyleProperty, false);
                container_.SetValue(FrameworkElement.StyleProperty, style);
            }
            else if (true.Equals(container_.GetValue(AppliedStyleProperty)))
            {
                // if we don't get a style now but we applied one previously clear it
                container_.ClearValue(AppliedStyleProperty);
                container_.ClearValue(FrameworkElement.StyleProperty);
            }
        }

        /// <summary>
        /// Invoked when an element needs to be generated for a given item.
        /// </summary>
        /// <returns>The element to represent the item</returns>
        protected override DependencyObject GetContainerForItem(object item_)
        {
            return (DependencyObject)Activator.CreateInstance(ContainerType);
        }

        /// <summary>
        /// Identifies the <see cref="ContainerStyle"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ContainerStyleProperty = DependencyProperty.Register("ContainerStyle",
            typeof(Style), typeof(ContainerFactory), new FrameworkPropertyMetadata(null, OnContainerStyleChanged));

        private static void OnContainerStyleChanged(DependencyObject d_, DependencyPropertyChangedEventArgs e_)
        {
            ContainerFactory ef = (ContainerFactory)d_;
            ef.RefreshContainerStyles();
        }

        /// <summary>
        /// Returns the style to apply to the element created.
        /// </summary>
        /// <seealso cref="ContainerStyleProperty"/>
        [Description("Returns the style to apply to the element created.")]
        [Category("Behavior")]
        [Bindable(true)]
        public Style ContainerStyle
        {
            get => (Style)GetValue(ContainerStyleProperty);
            set => SetValue(ContainerStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ContainerStyleSelector"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ContainerStyleSelectorProperty = DependencyProperty.Register("ContainerStyleSelector",
            typeof(StyleSelector), typeof(ContainerFactory), new FrameworkPropertyMetadata(null, OnContainerStyleChanged));

        /// <summary>
        /// Returns or sets a StyleSelector that can be used to provide a Style for the items.
        /// </summary>
        /// <seealso cref="ContainerStyleSelectorProperty"/>
        [Description("Returns or sets a StyleSelector that can be used to provide a Style for the items.")]
        [Category("Behavior")]
        [Bindable(true)]
        public StyleSelector ContainerStyleSelector
        {
            get => (StyleSelector)GetValue(ContainerStyleSelectorProperty);
            set => SetValue(ContainerStyleSelectorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ContainerType"/> dependency property
        /// </summary>
        public static readonly DependencyProperty ContainerTypeProperty = DependencyProperty.Register("ContainerType",
            typeof(Type), typeof(ContainerFactory), new FrameworkPropertyMetadata(null, OnContainerTypeChanged, CoerceContainerType), ValidateContainerType);

        private static void OnContainerTypeChanged(DependencyObject d_, DependencyPropertyChangedEventArgs e_)
        {
            ContainerFactory ef = (ContainerFactory)d_;
            ef.Reset();
        }

        private static object CoerceContainerType(DependencyObject d_, object newValue_)
        {
            ContainerFactory ef = (ContainerFactory)d_;
            Type newType = (Type)newValue_;

            if (null != newType)
                ef.ValidateContainerType(newType);

            return newValue_;
        }

        private static bool ValidateContainerType(object newValue_)
        {
            Type type = newValue_ as Type;

            if (type == null)
                return true;

            if (type.IsAbstract)
                throw new ArgumentException("ContainerType must be a non-abstract creatable type.");

            if (!typeof(DependencyObject).IsAssignableFrom(type))
                throw new ArgumentException("Element must be a DependencyObject derived type.");

            return true;
        }

        /// <summary>
        /// Returns or sets the type of element to create
        /// </summary>
        /// <seealso cref="ContainerTypeProperty"/>
        [Description("Returns or sets the type of element to create")]
        [Category("Behavior")]
        [Bindable(true)]
        public Type ContainerType
        {
            get => (Type)GetValue(ContainerTypeProperty);
            set => SetValue(ContainerTypeProperty, value);
        }

        /// <summary>
        /// ItemForContainer Attached Dependency Property
        /// </summary>
        private static readonly DependencyProperty AppliedStyleProperty =
            DependencyProperty.RegisterAttached("AppliedStyle", typeof(bool), typeof(ContainerFactory),
                new FrameworkPropertyMetadata(false));

        private void RefreshContainerStyles()
        {
            foreach (DependencyObject container in GetElements())
            {
                ApplyItemContainerStyle(container, GetItemFromContainer(container));
            }
        }

        /// <summary>
        /// Invoked when the <see cref="ContainerType"/> is about to be changed to determine if the specified type is allowed.
        /// </summary>
        /// <param name="elementType_">The new element type</param>
        protected virtual void ValidateContainerType(Type elementType_)
        {
        }
    }
}
