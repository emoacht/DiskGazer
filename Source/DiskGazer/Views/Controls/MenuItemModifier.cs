using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiskGazer.Views.Controls
{
	/// <summary>
	/// Modifier for <see cref="System.Windows.Controls.MenuItem"/>
	/// </summary>
	/// <remarks>
	/// This attached property must be attached to a FrameworkElement directly hosted by a MenuItem.
	/// In the hierarchy of MenuItems, this attached property will modify the Popup which is to be
	/// created under the MenuItem which seemingly hosts that FrameworkElement.
	/// </remarks>
	public class MenuItemModifier
	{
		public static bool GetIsEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsEnabledProperty);
		}
		public static void SetIsEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(IsEnabledProperty, value);
		}
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsEnabled",
				typeof(bool),
				typeof(MenuItemModifier),
				new PropertyMetadata(false, OnChanged));

		private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((FrameworkElement)d).Loaded += OnLoaded;
		}

		private static void OnLoaded(object sender, RoutedEventArgs e)
		{
			var element = (FrameworkElement)sender;

			Grid targetGrid = null;
			Brush targetBrush = null;

			DependencyObject ancestor = element;
			while (ancestor is not null)
			{
				ancestor = VisualTreeHelper.GetParent(ancestor);
				switch (ancestor)
				{
					case Grid grid when (targetGrid is null):
						targetGrid = grid;
						break;

					case Border border when (border.Background is SolidColorBrush { Color.A: > 0 }) && (targetBrush is null):
						targetBrush = border.Background;
						break;
				}

				if ((targetGrid is not null) &&
					(targetBrush is not null))
				{
					var presenter = targetGrid.Children
						.OfType<ContentPresenter>()
						.SingleOrDefault(x => ReferenceEquals(x.Content, element));

					Trace.Assert(presenter is not null, "FrameworkElement must be directly hosted by MenuItem.");

					int index = Grid.GetColumn(presenter);

					for (int i = 0; i < targetGrid.ColumnDefinitions.Count; i++)
					{
						if (i == index)
							continue;

						targetGrid.ColumnDefinitions[i].MinWidth = 0;
						targetGrid.ColumnDefinitions[i].Width = new GridLength(0);
					}

					targetGrid.Background = targetBrush;
					element.Loaded -= OnLoaded;
					break;
				}
			}
		}
	}
}