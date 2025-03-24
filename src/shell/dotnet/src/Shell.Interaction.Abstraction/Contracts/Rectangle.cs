namespace MorganStanley.ComposeUI.Shell.Interaction.Abstraction.Contracts;

/// <summary>
/// Represents a rectangle defined by its position (X, Y) and its dimensions (Width, Height).
/// </summary>
public class Rectangle
{
    /// <summary>
    /// Gets or sets the X-coordinate of the rectangle.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y-coordinate of the rectangle.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the rectangle.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the rectangle.
    /// </summary>
    public double Height { get; set; }
}