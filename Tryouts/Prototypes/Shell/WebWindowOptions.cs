namespace Shell
{
    public sealed class WebWindowOptions
    {
        public double? Height { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? IconUrl { get; set; }
        public double? Width { get; set; }

        public const double DefaultHeight = 450;
        public const string DefaultTitle = "Compose Web Container";
        public const string DefaultUrl = "about:blank";
        public const double DefaultWidth = 800;
    }
}
