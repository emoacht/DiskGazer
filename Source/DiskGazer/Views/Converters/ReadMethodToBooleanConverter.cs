using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using DiskGazer.Models;

namespace DiskGazer.Views.Converters
{
	/// <summary>
	/// Conversion from ReadMethod to Boolean
	/// </summary>
	[ValueConversion(typeof(ReadMethod), typeof(bool))]
	public class ReadMethodToBooleanConverter : IValueConverter
	{
		/// <summary>
		/// Returns true when source ReadMethod name matches target ReadMethod name.
		/// </summary>
		/// <param name="value">Source ReadMethod</param>
		/// <param name="targetType"></param>
		/// <param name="parameter">Target ReadMethod name string</param>
		/// <param name="culture"></param>
		/// <returns>Boolean</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value is not ReadMethod source) || (parameter is null))
				return DependencyProperty.UnsetValue;

			return string.Equals(source.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// If true, returns ReadMethod whose name is the same as target ReadMethod name.
		/// </summary>
		/// <param name="value">Source Boolean</param>
		/// <param name="targetType"></param>
		/// <param name="parameter">Target ReadMethod name string</param>
		/// <param name="culture"></param>
		/// <returns>ReadMethod</returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value is not bool target) || !target || (parameter is null))
				return DependencyProperty.UnsetValue;

			if (!Enum.TryParse(parameter.ToString(), true, out ReadMethod source))
				return DependencyProperty.UnsetValue;

			return source;
		}
	}
}