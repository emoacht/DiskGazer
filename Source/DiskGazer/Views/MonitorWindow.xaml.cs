﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			this.Loaded += OnLoaded;
		}

		private MainWindowViewModel _mainWindowViewModel;

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_mainWindowViewModel = this.Owner.DataContext as MainWindowViewModel;
			if (_mainWindowViewModel is not null)
			{
				this.TextBoxInnerStatus.SetBinding(
					TextBox.TextProperty,
					new Binding("InnerStatus")
					{
						Source = _mainWindowViewModel,
						Mode = BindingMode.OneWay,
					});
			}
		}
	}
}