using System.ComponentModel.DataAnnotations;

namespace Shell
{
    public sealed class WebWindowOptions
    {
        [Display(Description = "Set the height of the window. Default: 450")]
        public double? Height { get; set; }
        
        [Display(Description = $"Set the title of the window. Default: {DefaultTitle}")]
        public string? Title { get; set; }

        [Display(Description = $"Set the url for the web view. Default: {DefaultUrl}")]
        public string? Url { get; set; }

        [Display(Description = $"Set the icon url for the window.")]
        public string? IconUrl { get; set; }

        [Display(Description = $"Set the width of the window. Default: 800")]
        public double? Width { get; set; }

        public const double DefaultHeight = 450;
        public const string DefaultTitle = "Compose Web Container";
        public const string DefaultUrl = "about:blank";
        public const double DefaultWidth = 800;
    }
}
