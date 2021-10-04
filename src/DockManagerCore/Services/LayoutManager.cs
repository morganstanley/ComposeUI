using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using DockManagerCore.Utilities;

namespace DockManagerCore.Services
{
    public static class LayoutManager
    {
        #region Save Layout
        public static string SaveLayout()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                SaveLayout(stream);
                stream.Position = 0L;
                StreamReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            } 
        }

        public static void SaveLayout(Stream stream)
        {
            XmlTextWriter writer = new XmlTextWriter(stream, Encoding.UTF8)
            {
                Formatting = Formatting.Indented
            };
            writer.WriteStartDocument();
            writer.WriteStartElement("dockManager"); 
            writer.WriteStartElement("rootContainers");
            List<ContentPane> leafPanes = new List<ContentPane>();
            foreach (var floatingWindow in DockManager.GetAllFloatingWindows())
            {
                var paneContainer = floatingWindow.PaneContainer;
                if (paneContainer == null) continue;
                writer.WriteStartElement("rootContainer");
                if (string.IsNullOrEmpty(floatingWindow.Name))
                {
                    floatingWindow.Name = "window" + Guid.NewGuid().ToString("N");
                }
                writer.WriteAttributeString("name", floatingWindow.Name);
                XmlHelper.WriteAttribute(writer, "lastActivatedTime", floatingWindow.LastActivatedTime); 
                writer.WriteAttributeString("location", "Floating");
                XmlHelper.WriteAttributeWithConverter(writer, "floatingLocation", floatingWindow.RestoreBounds.Location);
                XmlHelper.WriteAttributeWithConverter(writer, "floatingSize", floatingWindow.RestoreBounds.Size); 
                SavePaneContainer(writer, paneContainer, leafPanes);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteStartElement("contentPanes");
            foreach (var contentPane in leafPanes)
            {
                writer.WriteStartElement("contentPane");
                writer.WriteAttributeString("name", contentPane.Name);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }

        private static void SavePaneContainer(XmlTextWriter writer_, PaneContainer container_, List<ContentPane> leafPanes_)
        {
            if (string.IsNullOrEmpty(container_.Name) || container_.Name == PaneContainer.DefaultName)
            {
                container_.Name = "paneContainer" + Guid.NewGuid().ToString("N");
            }
            writer_.WriteStartElement("paneContainer");
            writer_.WriteAttributeString("name", container_.Name); 
            writer_.WriteAttributeString("state", container_.WindowState.ToString());
            writer_.WriteAttributeString("isLocked", XmlConvert.ToString(container_.IsLocked)); 
            //writer_.WriteAttributeString("lockButtonState", container_.LockButtonState.ToString());
            writer_.WriteAttributeString("showHeader", XmlConvert.ToString(container_.IsHeaderVisible));
            //writer_.WriteAttributeString("hideButtonState", container_.HideButtonState.ToString());
            foreach (ContentPaneWrapper item in container_.Host.Items)
            {
                var contentPane = item.Pane;
                writer_.WriteStartElement("contentPane");
                if (string.IsNullOrEmpty(contentPane.Name))
                {
                    contentPane.Name = "contentPane" + Guid.NewGuid().ToString("N");
                }
                writer_.WriteAttributeString("name", contentPane.Name); 
                var dockingGrid = item.DockingGrid;
                XmlHelper.WriteAttribute(writer_, "lastActivatedTime", contentPane.LastActivatedTime);  
                XmlHelper.WriteAttributeWithConverter(writer_, "lastFloatingSize", contentPane.LastFloatingSize);
                if (item.DockingGrid.Root != null)
                {
                    SaveSplitter(writer_, dockingGrid.Root, leafPanes_);
                }
                else
                {
                    leafPanes_.Add(contentPane);
                }

                writer_.WriteEndElement();
            }
            writer_.WriteEndElement();
        }

        private static void SaveSplitter(XmlTextWriter writer_, SplitPanes splitPanes_, List<ContentPane> leafPanes_)
        {
            if (splitPanes_.Self != null)
            {
                SavePaneContainer(writer_, splitPanes_.Self, leafPanes_);
            }
            else
            {
                writer_.WriteStartElement("splitPane");
                writer_.WriteAttributeString("orientation", splitPanes_.Orientation.ToString());
                XmlHelper.WriteAttributeWithConverter(writer_, "firstLength", splitPanes_.FirstLength); 
                XmlHelper.WriteAttributeWithConverter(writer_, "secondLength", splitPanes_.SecondLength); 
                if (splitPanes_.First != null)
                {
                     SaveSplitter(writer_, splitPanes_.First, leafPanes_); 
                }
                if (splitPanes_.Second != null)
                {
                    SaveSplitter(writer_, splitPanes_.Second, leafPanes_); 
                }
                writer_.WriteEndElement();
            }

        }

        #endregion

        #region LoadLayout
        public static void LoadLayout(Stream stream_)
        {
            Dictionary<string, ContentPane> panes = new Dictionary<string, ContentPane>();
            foreach (ContentPane pane in DockManager.GetAllPanes())
            {
                if (!string.IsNullOrEmpty(pane.Name))
                {
                    panes[pane.Name] = pane;
                }
            }

            Dictionary<string, ContentPane> panesLoaded = new Dictionary<string, ContentPane>();
            SortedSet<ContentPane> panesSorted = new SortedSet<ContentPane>(ContentPane.Comparer);
            using (BufferedStream stream2 = new BufferedStream(stream_))
            {
                XmlDocument document = new XmlDocument();
                document.Load(stream2);
                XmlNode node = document.SelectSingleNode("dockManager");
                if (node == null)
                {
                    //todo: throw exception
                    return;
                }
                XmlNode node2 = node.SelectSingleNode("rootContainers");
                if (node2 == null)
                {
                    return;
                }
                var rootContainerNodes = node2.SelectNodes("rootContainer");
                if (rootContainerNodes == null) return;
                XmlNode node3 = node.SelectSingleNode("contentPanes");
                if (node3 == null) return;
                var contentPaneNodes = node3.SelectNodes("contentPane");
                if (contentPaneNodes == null) return; 
                foreach (XmlNode contentPaneNode in contentPaneNodes)
                {
                    string name = XmlHelper.ReadAttribute(contentPaneNode, "name"); 
                    ContentPane pane;
                    if (!panes.TryGetValue(name, out pane))
                    {
                        pane = new ContentPane {Name = name};
                    }
                    pane.Detach(); 
                    panesLoaded[name] = pane;
                }
                List<FloatingWindow> windows = DockManager.GetAllFloatingWindows();
                foreach (var floatingWindow in windows)
                {
                    if (floatingWindow.PaneContainer.GetAllPanes().Count == 0)
                    {
                        floatingWindow.PaneContainer.Close(true);
                    }
                }
                 
                Dictionary<XmlNode, PaneContainer> rootContainers = new Dictionary<XmlNode, PaneContainer>();
                foreach (XmlNode rootContainerNode in rootContainerNodes)
                {
                    string location = XmlHelper.ReadAttribute(rootContainerNode, "location");
                    if (location != "Floating") continue;
                    var paneContainerNode = rootContainerNode.SelectSingleNode("paneContainer");
                    if (paneContainerNode == null)
                    {
                        continue;
                    }
                    PaneContainer rootPaneContainer = LoadPaneContainer(paneContainerNode, panesLoaded); 
                    rootContainers.Add(rootContainerNode, rootPaneContainer);
                }

                foreach (var contentPane in panesLoaded.Values)
                {
                    panesSorted.Add(contentPane);
                }

                SortedSet<FloatingWindow> windowsSorted = new SortedSet<FloatingWindow>(FloatingWindow.Comparer);
                foreach (var rootContainer in rootContainers)
                {
                    var rootContainerNode = rootContainer.Key;
                    var rootPaneContainer = rootContainer.Value;
                    DateTime lastActivatedTime = XmlHelper.ReadAttribute(rootContainerNode, "lastActivatedTime", DateTime.MinValue);
                    Size floatingSize = XmlHelper.ReadAttributeWithConverter(rootContainerNode, "floatingSize", Size.Empty);
                    Point floatingLocation = XmlHelper.ReadAttributeWithConverter(rootContainerNode, "floatingLocation", new Point());
                    if (floatingSize.Width > 0 && floatingSize.Height > 0)
                    {
                        rootPaneContainer.PaneHeight = floatingSize.Height;
                        rootPaneContainer.PaneWidth = floatingSize.Width;
                    }
                    FloatingWindow window = new FloatingWindow(rootPaneContainer);
                    window.Name = XmlHelper.ReadAttribute(rootContainerNode, "name");
                    window.LastActivatedTime = lastActivatedTime;
                    window.Left = floatingLocation.X;
                    window.Top = floatingLocation.Y;
                    windowsSorted.Add(window);
                }

                foreach (var floatingWindow in windowsSorted)
                {
                    floatingWindow.Show();
                    floatingWindow.WindowState = floatingWindow.PaneContainer.WindowState;
                }
 
                foreach (var contentPane in panesSorted)
                {
                    if (contentPane.LastActivatedTime == new DateTime())
                    {
                        continue;
                    }
                    contentPane.ActivateInternal(false);
                } 
            }
        }

        private static PaneContainer LoadPaneContainer(XmlNode paneContainerNode_, Dictionary<string, ContentPane> panesLoaded_)
        {
            PaneContainer paneContainer = new PaneContainer();
            paneContainer.Name = XmlHelper.ReadAttribute(paneContainerNode_, "name");
            paneContainer.WindowState = XmlHelper.ReadEnumAttribute(paneContainerNode_, "state", WindowState.Normal);
            //paneContainer.LockButtonState = XmlHelper.ReadEnumAttribute(paneContainerNode_, "lockButtonState", WindowButtonState.Normal);
            paneContainer.IsLocked = XmlHelper.ReadAttribute(paneContainerNode_, "isLocked", false);
            //paneContainer.HideButtonState = XmlHelper.ReadEnumAttribute(paneContainerNode_, "hideButtonState", WindowButtonState.Normal);
            paneContainer.IsHeaderVisible = XmlHelper.ReadAttribute(paneContainerNode_, "showHeader", true);
            var contentPaneNodes2 = paneContainerNode_.SelectNodes("contentPane");
            if (contentPaneNodes2 != null)
            {
                foreach (XmlNode contentPaneNode in contentPaneNodes2)
                {
                    string name = XmlHelper.ReadAttribute(contentPaneNode, "name");
                    ContentPane pane;
                    if (!panesLoaded_.TryGetValue(name, out pane))
                    {
                        pane = new ContentPane { Name = name };  
                    }
                    pane.LastActivatedTime = XmlHelper.ReadAttribute(contentPaneNode, "lastActivatedTime", DateTime.MinValue);
                    pane.LastFloatingSize = XmlHelper.ReadAttributeWithConverter(contentPaneNode, "lastFloatingSize", Size.Empty);
                    paneContainer.AddPane(pane);
                    var dockingGrid = pane.Wrapper.DockingGrid;
                    var splitPaneNode = contentPaneNode.SelectSingleNode("splitPane"); 
                    if (splitPaneNode != null)
                    {
                        dockingGrid.Load(LoadSplitter(splitPaneNode, panesLoaded_)); 
                    }
                    var rootPane = dockingGrid.RootPane;
                    if (!panesLoaded_.ContainsKey(rootPane.Name))
                    {
                        panesLoaded_.Add(rootPane.Name, rootPane);
                    }
                }
            }

            return paneContainer;
        }

        private static SplitPanes LoadSplitter(XmlNode splitPaneNode_, Dictionary<string, ContentPane> panesLoaded_)
        {
            List<object> children = new List<object>();
            foreach (var childNode in splitPaneNode_.ChildNodes)
            {
                XmlElement element = childNode as XmlElement;
                if (element == null) continue;
                if (element.Name == "paneContainer")
                {
                    children.Add(LoadPaneContainer(element, panesLoaded_));
                }
                else if (element.Name == "splitPane")
                {
                    children.Add(LoadSplitter(element, panesLoaded_));
                }
            } 
            if (children.Count == 1)
            {
                return new SplitPanes(children[0] as PaneContainer);
            }
             if (children.Count == 2)
            {
                Orientation orientation = XmlHelper.ReadEnumAttribute(splitPaneNode_, "orientation", Orientation.Vertical);
                GridLength firstLength = XmlHelper.ReadAttributeWithConverter(splitPaneNode_, "firstLength", new GridLength(1, GridUnitType.Star));
                GridLength secondLength = XmlHelper.ReadAttributeWithConverter(splitPaneNode_, "secondLength", new GridLength(1, GridUnitType.Star));

                var first = children[0] as SplitPanes;
                if (first == null)
                {
                    first = new SplitPanes(children[0] as PaneContainer);
                }
                var second = children[1] as SplitPanes;
                if (second == null)
                {
                    second = new SplitPanes(children[1] as PaneContainer);
                }
                 return new SplitPanes(first, second, orientation)
                     {
                         FirstLength = firstLength,
                         SecondLength = secondLength,
                     } ;

            }
            return null;
        }
        #endregion 

    }

    internal class ContentPaneInfo
    {
        public ContentPane Pane { get; set; }
        public Size Size { get; set; }
        public SplitPanes ParentSplitPanes { get; set; }
    }
}
