using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DockManagerCore.Desktop
{
  internal class HeaderItemsCollection : ObservableCollection<object>
  {
    public HeaderItemsCollection()
    {
      FrameworkElementFactory factoryPanel = new FrameworkElementFactory(typeof(StackPanel));
      factoryPanel.SetValue(Panel.IsItemsHostProperty, true);
      factoryPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
      m_itemsControl.ItemsPanel = new ItemsPanelTemplate(factoryPanel);
      m_itemsControl.ItemsSource = this;
    }

    //control that represents items from this collection
    //its parent is not constant
    private readonly ItemsControl m_itemsControl = new ItemsControl();
    private readonly Dictionary<int, WeakReference> m_hosts = new Dictionary<int, WeakReference>();
    private int? m_currentHostKey;

    public ItemsControl Control => m_itemsControl;

    public bool IsCurrentParent(HeaderItemsHolder holder_)
    {
      return m_currentHostKey != null && holder_ == m_hosts[(int)m_currentHostKey].Target;
    }

    public void ReleaseCurrentParent()
    {
      if (m_currentHostKey != null)
      {
        HeaderItemsHolder current = m_hosts[(int) m_currentHostKey].Target as HeaderItemsHolder;
        m_currentHostKey = null;
        if (current != null)
        {
          current.HideItems();
        }
      }
    }

    public void SetCurrentParent() // find and set
    {      
      foreach (int key in m_hosts.Keys)
      {
        if (m_hosts[key] == null)
        {
          continue;
        }
        HeaderItemsHolder target = m_hosts[key].Target as HeaderItemsHolder;
        if (target != null && target.IsVisible)
        {
          m_currentHostKey = key;
          target.ShowItems();
          return;
        }
      }
    }

    public void SetCurrentParent(HeaderItemsHolder headerItemsHolder_) //set given
    {      
      if (m_currentHostKey != null) //we need to release the current one first
      {
        HeaderItemsHolder current = m_hosts[(int) m_currentHostKey].Target as HeaderItemsHolder;
        if (headerItemsHolder_ != current && current != null)
        {
          m_currentHostKey = null;
          current.HideItems();
        }
        else
        {
          return; //already current. no need to do anything.
        }
      }
      foreach (int key in m_hosts.Keys)
      {
        if (m_hosts[key] != null && m_hosts[key].Target == headerItemsHolder_)
        {
          headerItemsHolder_.ShowItems();
          m_currentHostKey = key;
          return;
        }
      }
    }

    public void AddNewParent(HeaderItemsHolder headerItemsHolder_)
    {      
      if (m_hosts.Keys.Any(key_ => m_hosts[key_] != null && m_hosts[key_].Target == headerItemsHolder_))
      {
        return;
      }
      CleanDictionary();
      foreach (int key in m_hosts.Keys)
      {
        if (m_hosts[key] == null || m_hosts[key].Target == null)
        {
          m_hosts[key] = new WeakReference(headerItemsHolder_);
          return;
        }
      }
      int newKey = m_hosts.Count;
      m_hosts[newKey] = new WeakReference(headerItemsHolder_);
    }

    public void RemoveParent(HeaderItemsHolder headerItemsHolder_)
    {      
      foreach (int key in m_hosts.Keys.ToList())
      {
        if (m_hosts[key] != null && m_hosts[key].Target == headerItemsHolder_)
        {          
          m_hosts[key] = null;
          if (m_currentHostKey == key)
          {
            m_currentHostKey = null;
            headerItemsHolder_.HideItems();
          }
          return;
        }
      }      
    }

    private void CleanDictionary()
    {
      //TODO either change keys or throw away all nulls from the dictionary
      foreach (int key in m_hosts.Keys.ToList())
      {
        if (m_hosts[key] != null && m_hosts[key].Target == null)
        {
          m_hosts[key] = null;          
        }
      }
    }
  }
}
