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
