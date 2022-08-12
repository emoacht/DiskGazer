using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DiskGazer.Views.Controls
{
	public class TextBoxDoubleFormat : TextBox
	{
		#region Property

		public string StringFormat
		{
			get { return (string)GetValue(StringFormatProperty); }
			set { SetValue(StringFormatProperty, value); }
		}
		public static readonly DependencyProperty StringFormatProperty =
			DependencyProperty.Register(
				"StringFormat",
				typeof(string),
				typeof(TextBoxDoubleFormat),
				new PropertyMetadata(string.Empty)); // String.Empty means not specified.

		public int ScaleNumber
		{
			get { return (int)GetValue(ScaleNumberProperty); }
			set { SetValue(ScaleNumberProperty, value); }
		}
		public static readonly DependencyProperty ScaleNumberProperty =
			DependencyProperty.Register(
				"ScaleNumber",
				typeof(int),
				typeof(TextBoxDoubleFormat),
				new PropertyMetadata(
					-1, // -1 means not specified.
					(d, e) =>
					{
						var expression = ((TextBoxDoubleFormat)d).GetBindingExpression(TextBox.TextProperty);
						if (expression != null)
							expression.UpdateTarget();
					}));

		public bool LeavesBlankIfZero
		{
			get { return (bool)GetValue(LeavesBlankIfZeroProperty); }
			set { SetValue(LeavesBlankIfZeroProperty, value); }
		}
		public static readonly DependencyProperty LeavesBlankIfZeroProperty =
			DependencyProperty.Register(
				"LeavesBlankIfZero",
				typeof(bool),
				typeof(TextBoxDoubleFormat),
				new PropertyMetadata(false));

		#endregion

		static TextBoxDoubleFormat()
		{
			TextBox.TextProperty.OverrideMetadata(
				typeof(TextBoxDoubleFormat),
				new FrameworkPropertyMetadata(
					string.Empty,
					null,
					CoerceFormat));
		}

		public TextBoxDoubleFormat()
		{ }

		private static object CoerceFormat(DependencyObject d, object baseValue)
		{
			if (!double.TryParse(baseValue?.ToString(), out double num))
				return baseValue;

			if ((num == 0D) && ((TextBoxDoubleFormat)d).LeavesBlankIfZero)
				return string.Empty;

			int scaleNumber = ((TextBoxDoubleFormat)d).ScaleNumber;
			if (0 <= scaleNumber)
				return string.Format($"{{0:f{scaleNumber}}}", num);

			var stringFormat = ((TextBoxDoubleFormat)d).StringFormat;
			if (!string.IsNullOrEmpty(stringFormat))
				return string.Format(stringFormat, num);

			return baseValue;
		}
	}
}