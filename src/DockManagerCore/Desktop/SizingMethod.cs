namespace DockManagerCore.Desktop
{
    /// <summary>
    /// Used to specify the sizing behaviour of panes.
    /// </summary>
    public enum SizingMethod
    {
        /// <summary>
        /// Pane will adjust its size to the contained objects
        /// </summary>
        SizeToContent,

        /// <summary>
        /// Pane size can be set programmatically
        /// </summary>
        Custom
    }
}
