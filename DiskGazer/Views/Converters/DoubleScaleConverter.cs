using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using DiskGazer.Helper;

namespace DiskGazer.Views.Converters
{
	/// <summary>
	/// Convert Double to scale of Double.
	/// </summary>
	[ValueConversion(typeof(double), typeof(int))]
	public class DoubleScaleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return DependencyProperty.UnsetValue;

			double num;
			if (!double.TryParse(value.ToString(), out num))
				return DependencyProperty.UnsetValue;

			return num.Scale();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
