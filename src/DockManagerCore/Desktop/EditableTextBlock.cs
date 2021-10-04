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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace DockManagerCore.Desktop
{
  public class EditableTextBlock : TextBlock
  {
    public bool IsInEditMode
    {
      get => (bool)GetValue(IsInEditModeProperty);
      set => SetValue(IsInEditModeProperty, value);
    }

    public event EventHandler DoneEdit;

    private EditableTextBlockAdorner m_adorner;

    // Using a DependencyProperty as the backing store for IsInEditMode.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsInEditModeProperty =
        DependencyProperty.Register("IsInEditMode",
        typeof(bool), typeof(EditableTextBlock),
        new FrameworkPropertyMetadata(false,
      FrameworkPropertyMetadataOptions.AffectsRender |
      FrameworkPropertyMetadataOptions.AffectsParentMeasure, IsInEditModeUpdate));

    /// <summary>
    /// Determines whether [is in edit mode update] [the specified obj].
    /// </summary>
    /// <param name="obj_">The obj.</param>
    /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
    private static void IsInEditModeUpdate(DependencyObject obj_, DependencyPropertyChangedEventArgs e)
    {            
      EditableTextBlock textBlock = obj_ as EditableTextBlock;
      if (null != textBlock)
      {
        if (!textBlock.IsVisible)
        {
          return;
        }
        
        //Get the adorner layer of the uielement (here TextBlock)
        AdornerLayer layer = AdornerLayer.GetAdornerLayer(textBlock);
        
        //If the IsInEditMode set to true means the user has enabled the edit mode then
        //add the adorner to the adorner layer of the TextBlock.
        if (textBlock.IsInEditMode)
        {          
          if (null == textBlock.m_adorner)
          {
            textBlock.m_adorner = new EditableTextBlockAdorner(textBlock);

            //Events wired to exit edit mode when the user presses Enter key or leaves the control.
            textBlock.m_adorner.TextBoxKeyUp += textBlock.TextBoxKeyUp;
            textBlock.m_adorner.TextBoxLostFocus += textBlock.TextBoxLostFocus;
          }
          layer.Add(textBlock.m_adorner);
        }
        else
        {
          //Remove the adorner from the adorner layer.
          Adorner[] adorners = layer.GetAdorners(textBlock);
          if (adorners != null)
          {
            foreach (Adorner adorner in adorners)
            {
              if (adorner is EditableTextBlockAdorner)
              {
                layer.Remove(adorner);
              }
            }
          }          
          
          //Update the textblock's text binding.
          BindingExpression expression = textBlock.GetBindingExpression(TextProperty);
          if (null != expression)
          {
            expression.UpdateTarget();
          }

          BindingExpression expression2 = textBlock.GetBindingExpression(IsInEditModeProperty);
          if (null != expression2)
          {
            expression2.UpdateTarget();
          }

          if (e.OldValue != e.NewValue)
          {
            textBlock.InvokeDoneEdit();
          }
        }
      }
    }

    /// <summary>
    /// Gets or sets the length of the max.
    /// </summary>
    /// <value>The length of the max.</value>
    public int MaxLength
    {
      get => (int)GetValue(MaxLengthProperty);
      set => SetValue(MaxLengthProperty, value);
    }

    // Using a DependencyProperty as the backing store for MaxLength.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.Register("MaxLength", typeof(int), typeof(EditableTextBlock), new UIPropertyMetadata(0));

    private void TextBoxLostFocus(object sender_, RoutedEventArgs e_)
    {
      IsInEditMode = false;
    }

    /// <summary>
    /// release the edit mode when user presses enter.
    /// </summary>
    /// <param name="sender_">The sender.</param>
    /// <param name="e_">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
    private void TextBoxKeyUp(object sender_, KeyEventArgs e_)
    {
      if (e_.Key == Key.Enter)
      {
        IsInEditMode = false;
      }
    }

    private void InvokeDoneEdit()
    {
      var copy = DoneEdit;
      if (copy != null)
      {
        copy(this, EventArgs.Empty);
      }
    }
  }
}