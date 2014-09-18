using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DiskGazer.Views.Controls
{
	/// <summary>
	/// Interaction logic for SliderPlus.xaml
	/// </summary>
	public partial class SliderPlus : UserControl
	{
		public SliderPlus()
		{
			InitializeComponent();
		}


		#region Dependency Property
		
		public double Value
		{
			get { return (double)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}
		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(
				"Value",
				typeof(double),
				typeof(SliderPlus),
				new FrameworkPropertyMetadata(
					0D,
					(d, e) =>
					{
						var buff = Math.Round((double)e.NewValue);
						var innerSlider = ((SliderPlus)d).InnerSlider;

						if (buff < innerSlider.Minimum)
							buff = innerSlider.Minimum;

						if (buff > innerSlider.Maximum)
							buff = innerSlider.Maximum;

						((SliderPlus)d).innerSliderValue = buff; // This must be changed on ahead.
						innerSlider.Value = buff;
					}));

		public double Maximum
		{
			get { return (double)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}
		public static readonly DependencyProperty MaximumProperty =
			RangeBase.MaximumProperty.AddOwner(
				typeof(SliderPlus),
				new FrameworkPropertyMetadata(
					10D,
					(d, e) =>
					{
						var buff = Math.Floor((double)e.NewValue);
						var innerSlider = ((SliderPlus)d).InnerSlider;

						var maximumThreshold = Math.Ceiling(innerSlider.Minimum) + 1D;
						if (buff < maximumThreshold)
							innerSlider.Maximum = maximumThreshold;

						innerSlider.Maximum = buff;
					}));

		public double Minimum
		{
			get { return (double)GetValue(MinimumProperty); }
			set { SetValue(MinimumProperty, value); }
		}
		public static readonly DependencyProperty MinimumProperty =
			RangeBase.MinimumProperty.AddOwner(
				typeof(SliderPlus),
				new FrameworkPropertyMetadata(
					0D,
					(d, e) =>
					{
						var buff = Math.Ceiling((double)e.NewValue);
						var innerSlider = ((SliderPlus)d).InnerSlider;

						var minimumThreshold = Math.Floor(innerSlider.Maximum) - 1D;
						if (buff < minimumThreshold)
							innerSlider.Minimum = minimumThreshold;

						innerSlider.Minimum = buff;
					}));

		public double LargeChange
		{
			get { return (double)GetValue(LargeChangeProperty); }
			set { SetValue(LargeChangeProperty, value); }
		}
		public static readonly DependencyProperty LargeChangeProperty =
			RangeBase.LargeChangeProperty.AddOwner(
				typeof(SliderPlus),
				new FrameworkPropertyMetadata(
					10D,
					null,
					(d, baseValue) => Math.Ceiling((double)baseValue)));

		public double SmallChange
		{
			get { return (double)GetValue(SmallChangeProperty); }
			set { SetValue(SmallChangeProperty, value); }
		}
		public static readonly DependencyProperty SmallChangeProperty =
			RangeBase.SmallChangeProperty.AddOwner(
				typeof(SliderPlus),
				new FrameworkPropertyMetadata(
					1D,
					null,
					(d, baseValue) => Math.Ceiling((double)baseValue)));

		public double ButtonFrequency
		{
			get { return (double)GetValue(ButtonFrequencyProperty); }
			set { SetValue(ButtonFrequencyProperty, value); }
		}
		public static readonly DependencyProperty ButtonFrequencyProperty =
			DependencyProperty.Register(
				"ButtonFrequency",
				typeof(double),
				typeof(SliderPlus),
				new FrameworkPropertyMetadata(
					1D,
					(d, e) => ((SliderPlus)d).Value = ((SliderPlus)d).Minimum));

		#endregion


		private double innerSliderValue;

		private void InnerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (InnerSlider.Value == innerSliderValue)
				return;

			Value = Math.Round(InnerSlider.Value / SmallChange) * SmallChange;
		}


		private enum Direction
		{
			Down,
			Up,
		}


		private void OnClick(object sender, RoutedEventArgs e)
		{
			var direction = e.Source.Equals(DownButton) ? Direction.Down : Direction.Up;
			SetValue(direction);
		}

		private void SetValue(Direction direction)
		{
			switch (direction)
			{
				case Direction.Down:
					if (Value > Minimum)
					{
						var num = Value - ButtonFrequency;
						Value = (num > Minimum) ? num : Minimum;
					}
					break;

				case Direction.Up:
					if (Value < Maximum)
					{
						var num = Value + ButtonFrequency;
						Value = (num < Maximum) ? num : Maximum;
					}
					break;
			}
		}
	}
}
