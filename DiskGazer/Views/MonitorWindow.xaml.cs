using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using DiskGazer.ViewModels;

namespace DiskGazer.Views
{
	public partial class MonitorWindow : Window
	{
		public MonitorWindow()
		{
			InitializeComponent();
		}

		private MainWindowViewModel mainWindowViewModel;

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			this.MinWidth = this.ActualWidth;

			mainWindowViewModel = this.Owner.DataContext as MainWindowViewModel;
			if (mainWindowViewModel != null)
			{
				this.TextBoxInnerStatus.SetBinding(
					TextBox.TextProperty,
					new Binding("InnerStatus")
					{
						Source = mainWindowViewModel,
						Mode = BindingMode.OneWay,
					});
			}
		}
	}
}
