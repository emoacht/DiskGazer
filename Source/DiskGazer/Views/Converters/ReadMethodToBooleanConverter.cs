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
	/// Convert ReadMethod to Boolean.
	/// </summary>
	[ValueConversion(typeof(ReadMethod), typeof(bool))]
	public class ReadMethodToBooleanConverter : IValueConverter
	{
		/// <summary>
		/// Return true when source ReadMethod name matches target ReadMethod name.
		/// </summary>
		/// <param name="value">Source ReadMethod</param>
		/// <param name="targetType"></param>
		/// <param name="parameter">Target ReadMethod name string</param>
		/// <param name="culture"></param>
		/// <returns>Boolean</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is ReadMethod) || (parameter == null))
				return DependencyProperty.UnsetValue;

			return ((ReadMethod)value).ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// If true, return ReadMethod whose name is the same as target ReadMethod name.
		/// </summary>
		/// <param name="value">Source Boolean</param>
		/// <param name="targetType"></param>
		/// <param name="parameter">Target ReadMethod name string</param>
		/// <param name="culture"></param>
		/// <returns>ReadMethod</returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool) || !(bool)value || (parameter == null))
				return DependencyProperty.UnsetValue;

			ReadMethod method;
			if (!Enum.TryParse(parameter.ToString(), true, out method))
				return DependencyProperty.UnsetValue;

			return method;
		}
	}
}