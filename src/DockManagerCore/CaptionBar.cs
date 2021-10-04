using System.Windows;

namespace DockManagerCore
{
    public static class CaptionBar
    {

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached("State", typeof(CaptionBarState), typeof(CaptionBar), new PropertyMetadata(CaptionBarState.Active, null, CoerceStateChanged));

        public static void SetState(UIElement element, CaptionBarState value)
        {
            element.SetValue(StateProperty, value);
        }

        public static CaptionBarState GetState(UIElement element)
        {
            return (CaptionBarState) element.GetValue(StateProperty);
        }
         

        private static object CoerceStateChanged(DependencyObject dependencyObject_, object baseValue_)
        {
            CaptionBarState newState = (CaptionBarState) baseValue_;
            CaptionBarState oldState = GetState(dependencyObject_ as UIElement);
            switch (newState)
            {
                case CaptionBarState.Unselected:
                    if (oldState != CaptionBarState.Grouped)
                    {
                        return newState; 
                    }
                    return oldState; 
                case CaptionBarState.Active:
                    if (oldState != CaptionBarState.Grouped)
                    {
                        return newState; 
                    }
                    return oldState;
                case CaptionBarState.Grouped:
                    return newState;
                case CaptionBarState.UnGrouped:
                    return CaptionBarState.Active; 
                default:
                    return newState; 
            }
        }
    }

    public enum CaptionBarState
    {
        Unselected,
        Active,
        Grouped, 
        UnGrouped,
    }
     
}
