using System.Collections.Generic;
using System.Windows;

namespace DockManagerCore.Services
{
    internal class GroupManager
    {
        static List<FloatingWindow> grouped = new List<FloatingWindow>();

        public static void Add(FloatingWindow window_)
        {
            grouped.Add(window_);
        }

        public static bool Remove(FloatingWindow window_)
        {
            return grouped.Remove(window_);
        }

        public static void Clear()
        {
            grouped.Clear();
        }

        public static bool Contains(FloatingWindow window_)
        {
            return grouped.Contains(window_);
        }

        public static List<FloatingWindow> GetWindows()
        {
            return grouped;
        }

        public static void MoveWindows(Vector v_)
        {
            foreach (FloatingWindow window in grouped)
            {
                window.Top += v_.Y;
                window.Left += v_.X;
            }
        }

    }
}
