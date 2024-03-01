// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.CodeDom;
using System.Diagnostics;
using System.IO;
using AvalonDock.Layout;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using AvalonDock.Layout.Serialization;
using System.Reflection.PortableExecutable;
using System.Windows.Navigation;
using System.Xml.XPath;
using AvalonDock;

namespace MorganStanley.ComposeUI.Shell.Layout;

public static class DockingHelper
{
    public static TContent CreateDockingWindow<TContent>(params object[] constructorArgs)
        where TContent : LayoutAnchorable
    {
        var content = App.Current.CreateInstance<TContent>(constructorArgs);
        var mainWindow = App.Current.MainWindow!;
        content.AddToLayout(mainWindow.DockManager, AnchorableShowStrategy.Most);
        content.Float();

        return content;
    }

    public static void SaveLayout(Stream stream)
    {
        var mainWindow = App.Current.MainWindow;

        if (mainWindow == null)
            return;

        var document = new XDocument();
        var root = new XElement(XmlConstants.DocumentElementName);
        document.Add(root);
        root.Add(SaveMainWindowPosition());
        root.Add(SaveDockManager());
        document.Save(stream);

        XElement SaveMainWindowPosition()
        {
            var xml = new XDocument();
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            using (var writer = xml.CreateWriter())
            {
                new XmlSerializer(typeof(WindowPosition), new XmlRootAttribute(XmlConstants.MainWindowElementName))
                    .Serialize(writer, new WindowPosition(mainWindow), ns);
            }

            return xml.Root!;
        }

        XElement SaveDockManager()
        {
            var xml = new XDocument();

            using (var writer = xml.CreateWriter())
            {
                var serializer = new XmlLayoutSerializer(mainWindow.DockManager);
                serializer.Serialize(writer);
            }

            return xml.Root!;
        }
    }

    public static void LoadLayout(Stream stream)
    {
        var mainWindow = App.Current.MainWindow;

        if (mainWindow == null)
            return;

        var document = XDocument.Load(stream);
        LoadMainWindowPosition(document.Root!.XPathSelectElement(XmlConstants.MainWindowElementName));
        LoadDockManager(document.Root!.XPathSelectElement(XmlConstants.LayoutRootElementName));

        void LoadMainWindowPosition(XElement? xml)
        {
            if (xml == null)
                return;

            var position = (WindowPosition)new XmlSerializer(
                typeof(WindowPosition),
                new XmlRootAttribute(XmlConstants.MainWindowElementName)).Deserialize(xml.CreateReader())!;

            mainWindow.WindowState = position.WindowState;
            mainWindow.Left = position.Left;
            mainWindow.Top = position.Top;
            mainWindow.Width = position.Width;
            mainWindow.Height = position.Height;
        }

        void LoadDockManager(XElement? xml)
        {
            if (xml == null)
                return;

            var serializer = new XmlLayoutSerializer(mainWindow.DockManager);
            serializer.Deserialize(xml.CreateReader());
        }
    }

    public class XmlConstants
    {
        public const string DocumentElementName = "Layout";
        public const string MainWindowElementName = "MainWindow";
        public const string LayoutRootElementName = "LayoutRoot";
    }

    [Serializable]
    public class WindowPosition
    {
        public WindowPosition() { }

        public WindowPosition(Window window)
        {
            Left = window.Left;
            Top = window.Top;
            Width = window.Width;
            Height = window.Height;
            WindowState = window.WindowState;
        }

        [XmlAttribute]
        public double Left { get; set; }

        [XmlAttribute]
        public double Top { get; set; }

        [XmlAttribute]
        public double Width { get; set; }

        [XmlAttribute]
        public double Height { get; set; }

        [XmlAttribute]
        public WindowState WindowState { get; set; }
    }
}
