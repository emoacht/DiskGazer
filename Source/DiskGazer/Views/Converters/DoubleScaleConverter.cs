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
	/// Convert double to the number of scale (decimals) of double.
	/// </summary>
	[ValueConversion(typeof(double), typeof(int))]
	public class DoubleScaleConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double num;
			if ((value == null) || !double.TryParse(value.ToString(), out num))
				return DependencyProperty.UnsetValue;

			return num.Scale();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}