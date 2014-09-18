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
		#region Dependency Property

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
				new FrameworkPropertyMetadata(String.Empty)); // String.Empty means not specified.

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
				new FrameworkPropertyMetadata(
					-1, // -1 means not specified;
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
				new FrameworkPropertyMetadata(false));

		#endregion


		public TextBoxDoubleFormat() : base() { }

		static TextBoxDoubleFormat()
		{
			TextBox.TextProperty.OverrideMetadata(
				typeof(TextBoxDoubleFormat),
				new FrameworkPropertyMetadata(
					String.Empty,
					null,
					CoerceFormat));
		}

		private static object CoerceFormat(DependencyObject d, object baseValue)
		{
			double num;
			if ((baseValue == null) || !double.TryParse(baseValue.ToString(), out num))
				return baseValue;

			if ((num == 0D) && ((TextBoxDoubleFormat)d).LeavesBlankIfZero)
				return String.Empty;

			int scaleNumber = ((TextBoxDoubleFormat)d).ScaleNumber;
			if (0 <= scaleNumber)
				return String.Format(string.Format("{0}{1}{2}", "{0:f", scaleNumber, "}"), num);

			var stringFormat = ((TextBoxDoubleFormat)d).StringFormat;
			if (!String.IsNullOrEmpty(stringFormat))
				return String.Format(stringFormat, num);
			
			return baseValue;
		}
	}
}
