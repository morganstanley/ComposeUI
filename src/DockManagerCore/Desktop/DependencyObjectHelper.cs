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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DockManagerCore.Desktop
{
    public static class DependencyObjectHelper
    {
        public static T FindVisualParent<T>(this DependencyObject control_) where T : DependencyObject
        {
            if (control_ != null)
            {
                var parent = VisualTreeHelper.GetParent(control_);
                if (parent is T) return (T)parent;
                return FindVisualParent<T>(parent);
            }
            return null;
        }

        public static T FindLogicalParent<T>(this DependencyObject control_) where T : DependencyObject
        {
            if (control_ != null)
            {
                var parent = LogicalTreeHelper.GetParent(control_);
                if (parent is T) return (T)parent;
                return FindLogicalParent<T>(parent);
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent_) where T : DependencyObject
        {
            if (parent_ != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent_); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent_, i);
                    if (child == null) continue;
                    
                    if (child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent_, bool expandContentPresenter) where T : DependencyObject
        {
            if (parent_ != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent_); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent_, i);
                    if (child == null) continue;

                    if (child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    } 
                }
            }
        }

        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject parent_, bool includeCurrentControl_ = false) where T : DependencyObject
        {
            if (parent_ != null)
            {
                if (includeCurrentControl_ && parent_ is T)
                {
                    yield return (T)parent_;
                }
                foreach (object child in LogicalTreeHelper.GetChildren(parent_))
                {
                    if (child == null) continue;
                    if (child is T)
                    {
                        yield return (T)child;
                    }
                    DependencyObject d = child as DependencyObject;
                    if (d == null) continue;
                    foreach (T childOfChild in FindLogicalChildren<T>(d))
                    {
                        yield return childOfChild;
                    }

                }
            }
        }

        public static T FindVisualChildByName<T>(this DependencyObject parent_, string name_) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent_); i++)
            {
                var child = VisualTreeHelper.GetChild(parent_, i);
                string controlName = child.GetValue(Control.NameProperty) as string;
                if (controlName == name_)
                {
                    return child as T;
                }

                T result = FindVisualChildByName<T>(child, name_);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static List<BindingBase> GetBindingObjects(Object element_)
        {
            List<BindingBase> bindings = new List<BindingBase>();
            List<FieldInfo> propertiesAll = new List<FieldInfo>();
            Type currentLevel = element_.GetType();
            while (currentLevel != typeof(object))
            {
                propertiesAll.AddRange(currentLevel.GetFields());
                currentLevel = currentLevel.BaseType;
            }
            var propertiesDp = propertiesAll.Where(x => x.FieldType == typeof(DependencyProperty));

            foreach (var property in propertiesDp)
            {
                BindingBase b = BindingOperations.GetBindingBase(element_ as DependencyObject, property.GetValue(element_) as DependencyProperty);
                if (b != null)
                {
                    bindings.Add(b);
                }
            }

            return bindings;
        }


        public static IEnumerable GetBindingSources(this DependencyObject parent_, string propertyName_)
        {

            List<BindingBase> bindings = GetBindingObjects(parent_);
            Predicate<Binding> condition =
                b =>
                {
                    return b != null &&
                        (b.Path is PropertyPath)
                        && b.Path.Path == propertyName_;
                };

            foreach (BindingBase bindingBase in bindings)
            {
                if (bindingBase is Binding)
                {
                    if (condition(bindingBase as Binding))
                        yield return parent_;
                }
                else if (bindingBase is MultiBinding)
                {
                    MultiBinding mb = bindingBase as MultiBinding;
                    foreach (Binding b in mb.Bindings)
                    {
                        if (condition(b))
                            yield return parent_;
                    }
                }
                else if (bindingBase is PriorityBinding)
                {
                    PriorityBinding pb = bindingBase as PriorityBinding;
                    foreach (Binding b in pb.Bindings)
                    {
                        if (condition(b))
                            yield return parent_;
                    }
                }
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent_);
            if (childrenCount > 0)
            {
                for (int i = 0; i < childrenCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent_, i);
                    foreach (object element in GetBindingSources(child, propertyName_))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}
