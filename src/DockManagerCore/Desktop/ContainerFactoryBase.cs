/*
 * Morgan Stanley makes this available to you under the Apache License,
 * Version 2.0 (the "License"). You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0.
 *
 * See the NOTICE file distributed with this work for additional information
 * regarding copyright ownership. Unless required by applicable law or agreed
 * to in writing, software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace DockManagerCore.Desktop
{
  /// <summary>
  /// Abstract base class used to generate instances of elements based on fr given source collection of items (<see cref="ItemsSource"/>).
  /// </summary>
  [ContentProperty("ItemBindings")]
  public abstract class ContainerFactoryBase : Freezable, ISupportInitialize
  {		
    private ICollectionView m_currentView;
    private readonly Dictionary<object, DependencyObject> m_generatedElements;
    private bool m_isInitializing;
    private readonly ObservableCollection<ItemBinding> m_itemBindings;

    /// <summary>
    /// Initializes a new <see cref="ContainerFactoryBase"/>
    /// </summary>
    protected ContainerFactoryBase()
    {
      m_generatedElements = new Dictionary<object, DependencyObject>();
      m_itemBindings = new ObservableCollection<ItemBinding>();
      m_itemBindings.CollectionChanged += OnItemBindingsChanged;
    }

    #region Base class overrides
		
    /// <summary>
    /// Creates an instance of the class
    /// </summary>
    /// <returns></returns>
    protected override Freezable CreateInstanceCore()
    {
      return (ContainerFactoryBase)Activator.CreateInstance(GetType());
    }		
	
    /// <summary>
    /// Invoked when the object is to be frozen.
    /// </summary>
    /// <param name="isChecking">True if the ability to freeze is being checked or false when the object is being attempted to be made frozen</param>
    /// <returns>Returns false since this object cannot be frozen.</returns>
    protected override bool FreezeCore(bool isChecking)
    {
      return false;
    }		

    #endregion //Base class overrides
		
    /// <summary>
    /// Returns the collection of bindings that will be used to associated properties of the items from the <see cref="ItemsSource"/> with properties on the generated containers.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Bindable(true)]
    public ObservableCollection<ItemBinding> ItemBindings => m_itemBindings;

    /// <summary>
    /// Identifies the <see cref="ItemsSource"/> dependency property
    /// </summary>
    public static readonly DependencyProperty ItemsSourceProperty = 
      DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ContainerFactoryBase), new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ContainerFactoryBase ef = (ContainerFactoryBase)d;
      ef.OnItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
    }

    private void OnItemsSourceChanged(IEnumerable oldItems_, IEnumerable newItems_)
    {
      if (m_currentView != null)
      {
        m_currentView.CollectionChanged -= OnCollectionChanged;
        m_currentView = null;
        ClearItems();
      }

      if (null != newItems_)
      {
        m_currentView = CollectionViewSource.GetDefaultView(newItems_);
        Debug.Assert(m_currentView != null);
        m_currentView.CollectionChanged += OnCollectionChanged;
        ReinitializeElements();
      }
    }

    /// <summary>
    /// Returns or sets the collection of items used to generate elements.
    /// </summary>
    /// <seealso cref="ItemsSourceProperty"/>
    public IEnumerable ItemsSource
    {
      get => (IEnumerable)GetValue(ItemsSourceProperty);
      set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Returns a boolean indicating if the object is being initialized.
    /// </summary>
    protected bool IsInitializing => m_isInitializing;

    /// <summary>
    /// ItemForContainer Attached Dependency Property
    /// </summary>
    internal static readonly DependencyProperty ItemForContainerProperty =
      DependencyProperty.RegisterAttached("ItemForContainer", typeof(object), typeof(ContainerFactoryBase),
                                          new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Returns the data item for which a given container is associated.
    /// </summary>
    /// <param name="container">The container to evaluate</param>
    /// <returns>The item associated with the specified container.</returns>
    public static object GetItemForContainer(DependencyObject container)
    {
      return container.GetValue(ItemForContainerProperty);
    }

    /// <summary>
    /// Used to apply a style to the container for an item
    /// </summary>
    /// <param name="container">The container associated with the item</param>
    /// <param name="item">The item from the source collection</param>
    protected virtual void ApplyItemContainerStyle(DependencyObject container, object item)
    {

    }		

    /// <summary>
    /// Used to clear any settings applied to a container in the <see cref="PrepareContainerForItem"/>
    /// </summary>
    /// <param name="container">The container element </param>
    /// <param name="item">The item from the source collection</param>
    protected virtual void ClearContainerForItem(DependencyObject container, object item)
    {
    }

    /// <summary>
    /// Invoked when an element needs to be generated for a given item.
    /// </summary>
    /// <returns>The element to represent the item</returns>
    protected abstract DependencyObject GetContainerForItem(object item);

    /// <summary>
    /// Returns an enumerable list of elements that have been generated
    /// </summary>
    /// <returns></returns>
    protected IEnumerable<DependencyObject> GetElements()
    {
      if (m_currentView != null)
      {
        foreach (object item in m_currentView)
        {
          DependencyObject container;

          if (m_generatedElements.TryGetValue(item, out container))
          {
            yield return container;
          }
        }
      }
    }

    /// <summary>
    /// Returns the item associated with a given container.
    /// </summary>
    /// <param name="container">The container whose underlying item is being requested</param>
    /// <returns>The underlying item</returns>
    protected object GetItemFromContainer(DependencyObject container)
    {
      return container.GetValue(ItemForContainerProperty);
    }

    /// <summary>
    /// Used to determine if the item from the source collection needs to have an container element generated for it.
    /// </summary>
    /// <param name="item">The item to evaluate</param>
    /// <returns>Returns true to indicate that a container is needed</returns>
    protected virtual bool IsItemItsOwnContainer(object item)
    {
      return true;
    }

    /// <summary>
    /// Invoked when an element for an item has been generated.
    /// </summary>
    /// <param name="item">The underlying item for which the element has been generated</param>
    /// <param name="container">The element that was generated</param>
    /// <param name="index">The index at which the item existed</param>
    protected abstract void OnItemInserted(DependencyObject container, object item, int index);

    /// <summary>
    /// Invoked when an item has been moved in the source collection.
    /// </summary>
    /// <param name="item">The item that was moved</param>
    /// <param name="container">The associated element</param>
    /// <param name="oldIndex">The old index</param>
    /// <param name="newIndex">The new index</param>
    protected abstract void OnItemMoved(DependencyObject container, object item, int oldIndex, int newIndex);

    /// <summary>
    /// Invoked when an element created for an item has been removed
    /// </summary>
    /// <param name="oldItem">The item associated with the element that was removed</param>
    /// <param name="container">The element that has been removed</param>
    protected abstract void OnItemRemoved(DependencyObject container, object oldItem);

    /// <summary>
    /// Used to initialize a container for a given item.
    /// </summary>
    /// <param name="container">The container element </param>
    /// <param name="item">The item from the source collection</param>
    protected virtual void PrepareContainerForItem(DependencyObject container, object item)
    {
      for (int i = 0, count = m_itemBindings.Count; i < count; i++)
      {
        ItemBinding itemBinding = m_itemBindings[i];

        if (itemBinding.CanApply(container, item))
        {
          BindingOperations.SetBinding(container, itemBinding.TargetProperty, itemBinding.Binding);
        }
      }
    }

    /// <summary>
    /// Removes all generated elements and rebuilds the elements.
    /// </summary>
    protected void Reset()
    {
      ClearItems();

      ReinitializeElements();
    }

    /// <summary>
    /// Invoked during a verification of the source collection versus the elements generated to ensure the item is in the same location as that source item.
    /// </summary>
    /// <param name="item">The item being verified</param>
    /// <param name="container">The element associated with the item</param>
    /// <param name="index">The index in the source collection where the item exists</param>
    protected virtual void VerifyItemIndex(DependencyObject container, object item, int index)
    {
    }

	
    private void AttachContainerToItem(DependencyObject container, object item)
    {
      // store a reference to the item on the container
      container.SetValue(ItemForContainerProperty, item);

      if (item != container)
        container.SetValue(FrameworkElement.DataContextProperty, item);
    }
		
    private void ClearItems()
    {
      DependencyObject[] elements = new DependencyObject[m_generatedElements.Count];
      m_generatedElements.Values.CopyTo(elements, 0);
      m_generatedElements.Clear();

      foreach (DependencyObject container in elements)
      {
        OnItemRemovedImpl(container, container.GetValue(ItemForContainerProperty));
      }
    }
		
    private void InsertItem(int index, object newItem)
    {
      Debug.Assert(!m_generatedElements.ContainsKey(newItem));

      // create the element and associate it with the new item
      DependencyObject container;

      if (IsItemItsOwnContainerImpl(newItem))
      {
        container = newItem as DependencyObject;
      }
      else
      {
        container = GetContainerForItem(newItem);
      }

      // keep a map between the new item and the element
      m_generatedElements[newItem] = container;

      AttachContainerToItem(container, newItem);

      ApplyItemContainerStyle(container, newItem);

      PrepareContainerForItem(container, newItem);

      OnItemInserted(container, newItem, index);
    }

    private bool IsItemItsOwnContainerImpl(object item)
    {
      if (item is DependencyObject == false)
        return false;

      return IsItemItsOwnContainer(item);
    }

    private void MoveItem(object item, int oldIndex, int newIndex)
    {
      DependencyObject container;

      if (m_generatedElements.TryGetValue(item, out container))
      {
        OnItemMoved(container, item, oldIndex, newIndex);
      }
    }

    private void OnItemBindingsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      Reset();
    }

    private void OnItemRemovedImpl(DependencyObject container, object oldItem)
    {
      OnItemRemoved(container, oldItem);

      container.ClearValue(ItemForContainerProperty);

      ClearContainerForItem(container, oldItem);
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (m_isInitializing)
        return;

      // since its a freezable make sure its not frozen
      WritePreamble();

      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:
          for (int i = 0; i < e.NewItems.Count; i++)
            InsertItem(i + e.NewStartingIndex, e.NewItems[i]);
          break;
        case NotifyCollectionChangedAction.Remove:
          foreach (object newItem in e.OldItems)
            RemoveItem(newItem);
          break;
        case NotifyCollectionChangedAction.Move:
          MoveItem(e.OldItems[0], e.OldStartingIndex, e.NewStartingIndex);
          break;
        case NotifyCollectionChangedAction.Replace:
          foreach (object newItem in e.OldItems)
            RemoveItem(newItem);
          for (int i = 0; i < e.NewItems.Count; i++)
            InsertItem(i + e.NewStartingIndex, e.NewItems[i]);
          break;
        case NotifyCollectionChangedAction.Reset:
          ReinitializeElements();
          break;
      }
    }

    private void ReinitializeElements()
    {
      if (m_currentView == null || m_currentView.IsEmpty)
        ClearItems();
      else
      {
        if (IsInitializing)
          return;

        HashSet<object> oldItems = new HashSet<object>(m_generatedElements.Keys);

        foreach (object item in m_currentView)
        {
          oldItems.Remove(item);
        }

        foreach (object oldItem in oldItems)
        {
          DependencyObject container = m_generatedElements[oldItem];
          m_generatedElements.Remove(oldItem);

          OnItemRemovedImpl(container, oldItem);
        }

        int index = 0;
        foreach (object item in m_currentView)
        {
          DependencyObject container;

          if (!m_generatedElements.TryGetValue(item, out container))
          {
            InsertItem(index, item);
          }
          else
          {
            VerifyItemIndex(container, item, index);
          }

          index++;
        }
      }
    }

    private void RemoveItem(object oldItem)
    {
      Debug.Assert(m_generatedElements.ContainsKey(oldItem));

      DependencyObject container;
      if (m_generatedElements.TryGetValue(oldItem, out container))
      {
        m_generatedElements.Remove(oldItem);

        OnItemRemovedImpl(container, oldItem);
      }
    }

    #region ISupportInitialize Members

    /// <summary>
    /// Invoked when the object is about to be initialized
    /// </summary>
    public void BeginInit()
    {
      Debug.Assert(!m_isInitializing);

      WritePreamble();
      m_isInitializing = true;
    }

    /// <summary>
    /// Invoked when the object initialization is complete
    /// </summary>
    public void EndInit()
    {
      WritePreamble();
      m_isInitializing = false;

      ReinitializeElements();
    }

    #endregion
  }
}