using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DiskGazer.Views.Converters
{
	/// <summary>
	/// Conversion from int to double divided by 1024
	/// </summary>
	[ValueConversion(typeof(int), typeof(double))]
	public class TripleDigitConverter : IValueConverter
	{
		private const double TripleDigitFactor = 1024D;

		/// <summary>
		/// Divides an int by 1024 and then truncate the double.
		/// </summary>
		/// <param name="value">Int</param>
		/// <param name="targetType"></param>
		/// <param name="parameter">Number of scale (decimals) (optional)</param>
		/// <param name="culture"></param>
		/// <returns>Double</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value is not int source) || !int.TryParse(value?.ToString(), out source))
				return DependencyProperty.UnsetValue;

			double divider = 1D;
			if (parameter is not null)
			{
				if (int.TryParse(parameter.ToString(), out int scaleNumber))
					divider = Math.Pow(10D, scaleNumber);
			}

			return Math.Truncate((double)source * divider / TripleDigitFactor) / divider;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value is not double target) || !double.TryParse(value?.ToString(), out target))
				return DependencyProperty.UnsetValue;

			return (int)Math.Truncate(target * TripleDigitFactor);
		}
	}
}