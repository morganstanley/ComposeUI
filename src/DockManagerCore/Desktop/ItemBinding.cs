using System;
using System.Windows;
using System.Windows.Data;

namespace DockManagerCore.Desktop
{
	/// <summary>
	/// Class that provides information about a binding.
	/// </summary>
	public class ItemBinding
	{
		/// <summary>
		/// Returns or sets the binding to the underlying item that will be set as the binding for the <see cref="TargetProperty"/>
		/// </summary>
		public Binding Binding
		{
			get;
			set;
		}

		/// <summary>
		/// Returns or sets the base type for the item to which the TargetProperty will be bound.
		/// </summary>
		/// <remarks>
		/// <para>This property defaults to null which means that it can be applied to any item. This property 
		/// is intended to be used in a situation where the source collection can items of different types and 
		/// the binding only applies to items of a given source type.</para>
		/// </remarks>
		public Type SourceType
		{
			get;
			set;
		}

		/// <summary>
		/// Returns or sets the base type for the container on which this binding may be applied.
		/// </summary>
		public Type TargetContainerType
		{
			get;
			set;
		}

		/// <summary>
		/// Returns or sets the property that will be set to the specified <see cref="Binding"/>
		/// </summary>
		public DependencyProperty TargetProperty
		{
			get;
			set;
		}

		internal bool CanApply(DependencyObject container_, object item_)
		{
			if (TargetProperty == null || Binding == null)
				return false;

			if (container_ != null && TargetContainerType != null && !TargetContainerType.IsAssignableFrom(container_.GetType()))
				return false;

			if (item_ != null && SourceType != null && !SourceType.IsAssignableFrom(item_.GetType()))
				return false;

			return true;
		}
	}
}
