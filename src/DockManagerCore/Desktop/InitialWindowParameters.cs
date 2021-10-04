using System;
using System.Windows;

namespace DockManagerCore.Desktop
{
    [Serializable]
    public class InitialWindowParameters
    { 

        /// <summary>
        /// Gets or sets if the window can be resized by the user or code.
        /// </summary>
        public SizingMethod SizingMethod { get; set; }

        /// <summary>
        /// Gets or sets the width of the window when it is first shown.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the window when it is first shown.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets if the window is allowed to be larger than a single display.
        /// </summary>
        public bool EnforceSizeRestrictions { get; set; }

        /// <summary>
        /// Gets or sets if the window is always on top. would only take effect when UseDockManager is false
        /// </summary>
        public bool Topmost { get; set; }

        /// <summary>
        /// Gets or sets if the window is shown on the taskbar.
        /// </summary>
        public bool ShowInTaskbar { get; set; }

        /// <summary>
        /// Gets or sets if the window can be moved partially offscreen.
        /// </summary>
        public bool AllowPartiallyOffscreen { get; set; }

        /// <summary>
        /// Gets or sets whether the window's header is initially visible.
        /// </summary>
        public bool IsHeaderVisible { get; set; }

        public bool Transient { get; set; }
        /// <summary>
        /// Gets or sets the resources applied to the window.
        /// </summary>
        public ResourceDictionary Resources { get; set; }
 

        public InitialWindowParameters()
        { 
            SizingMethod = SizingMethod.SizeToContent;
            Width = double.NaN;
            Height = double.NaN;
            EnforceSizeRestrictions = true;
            Transient = false;
            Topmost = false;
            ShowInTaskbar = true;
            AllowPartiallyOffscreen = true;
            IsHeaderVisible = true; 
        }
    }
}