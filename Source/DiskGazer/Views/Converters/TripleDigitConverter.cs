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
	/// Convert int to double divided by 1024.
	/// </summary>
	[ValueConversion(typeof(int), typeof(double))]
	public class TripleDigitConverter : IValueConverter
	{
		private const double _tripleDigitFactor = 1024D;

		/// <summary>
		/// Divide an int by 1024 and then truncate the double.
		/// </summary>
		/// <param name="value">Int</param>
		/// <param name="targetType"></param>
		/// <param name="parameter">Number of scale (decimals) (optional)</param>
		/// <param name="culture"></param>
		/// <returns>Double</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int num;
			if ((value == null) || !int.TryParse(value.ToString(), out num))
				return DependencyProperty.UnsetValue;

			double divider = 1D;
			if (parameter != null)
			{
				int numScale;
				if (int.TryParse(parameter.ToString(), out numScale))
					divider = Math.Pow(10D, numScale);
			}

			return Math.Truncate((double)num * divider / _tripleDigitFactor) / divider;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double num;
			if ((value == null) || !double.TryParse(value.ToString(), out num))
				return DependencyProperty.UnsetValue;

			return (int)Math.Truncate(num * _tripleDigitFactor);
		}
	}
}