using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DockManagerCore.Desktop
{
  /// <summary>
  /// Adorner class which shows textbox over the text block when the Edit mode is on.
  /// </summary>
  public class EditableTextBlockAdorner : Adorner
  {
    private readonly VisualCollection m_collection;

    private readonly TextBox m_textBox;

    private readonly TextBlock m_textBlock;

    public EditableTextBlockAdorner(EditableTextBlock adornedElement_)
      : base(adornedElement_)
    {
      m_collection = new VisualCollection(this);
      m_textBox = new TextBox();
      m_textBlock = adornedElement_;
      Binding binding = new Binding("Text") { Source = adornedElement_ };
      m_textBox.SetBinding(TextBox.TextProperty, binding);
      m_textBox.AcceptsReturn = true;
      m_textBox.MaxLength = adornedElement_.MaxLength;
      m_textBox.KeyUp += OnTextBox_KeyUp;
      m_collection.Add(m_textBox);
    }

    void OnTextBox_KeyUp(object sender_, KeyEventArgs e_)
    {
      if (e_.Key == Key.Enter)
      {
        m_textBox.Text = m_textBox.Text.Replace("\r\n", string.Empty);
        BindingExpression expression = m_textBox.GetBindingExpression(TextBox.TextProperty);
        if (null != expression)
        {
          expression.UpdateSource();
        }
      }
    }

    protected override Visual GetVisualChild(int index_)
    {
      return m_collection[index_];
    }

    protected override int VisualChildrenCount => m_collection.Count;

    protected override Size ArrangeOverride(Size finalSize_)
    {
      m_textBox.Arrange(new Rect(0, 0, m_textBlock.DesiredSize.Width + 50, m_textBlock.DesiredSize.Height * 1.5));
      Keyboard.Focus(m_textBox);
      return finalSize_;
    }

    protected override void OnRender(DrawingContext drawingContext_)
    {
      drawingContext_.DrawRectangle(null, new Pen
                                             {
                                               Brush = Brushes.Gold,
                                               Thickness = 2
                                             }, new Rect(0, 0, m_textBlock.DesiredSize.Width + 50, m_textBlock.DesiredSize.Height * 1.5));
    }

    public event KeyboardFocusChangedEventHandler TextBoxLostFocus
    {
      add => m_textBox.LostKeyboardFocus += value;
      remove => m_textBox.LostKeyboardFocus -= value;
    }

    public event KeyEventHandler TextBoxKeyUp
    {
      add => m_textBox.KeyUp += value;
      remove => m_textBox.KeyUp -= value;
    }

    public Style AdornerTextBoxStyle
    {
        get => (Style)GetValue(AdornerTextBoxStyleProperty);
        set => SetValue(AdornerTextBoxStyleProperty, value);
    }

    public static readonly DependencyProperty AdornerTextBoxStyleProperty =
        DependencyProperty.Register("AdornerTextBoxStyle", typeof(Style), typeof(EditableTextBlockAdorner),
        new FrameworkPropertyMetadata(null, AdornerTextBoxStylePropertyChanged));

    private static void AdornerTextBoxStylePropertyChanged(DependencyObject sender_, DependencyPropertyChangedEventArgs args_)
    {
        var adorner = sender_ as EditableTextBlockAdorner;
        if (adorner != null)
        {
            adorner.m_textBox.Style = adorner.AdornerTextBoxStyle;
        }
    }
  }
}
