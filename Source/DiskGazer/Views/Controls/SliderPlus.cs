using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DiskGazer.Views.Controls
{
	[TemplatePart(Name = "PART_InnerSlider", Type = typeof(Slider))]
	[TemplatePart(Name = "PART_DownButton", Type = typeof(RepeatButton))]
	[TemplatePart(Name = "PART_UpButton", Type = typeof(RepeatButton))]
	public class SliderPlus : Control
	{
		#region Template Part

		private Slider InnerSlider
		{
			get => _innerSlider;
			set
			{
				if (_innerSlider is not null)
					_innerSlider.ValueChanged -= new RoutedPropertyChangedEventHandler<double>(OnSliderValueChanged);

				_innerSlider = value;

				if (_innerSlider is not null)
					_innerSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(OnSliderValueChanged);
			}
		}
		private Slider _innerSlider;

		private RepeatButton DownButton
		{
			get => _downButton;
			set
			{
				if (_downButton is not null)
					_downButton.Click -= new RoutedEventHandler(OnButtonClick);

				_downButton = value;

				if (_downButton is not null)
					_downButton.Click += new RoutedEventHandler(OnButtonClick);
			}
		}
		private RepeatButton _downButton;

		private RepeatButton UpButton
		{
			get => _upButton;
			set
			{
				if (_upButton is not null)
					_upButton.Click -= new RoutedEventHandler(OnButtonClick);

				_upButton = value;

				if (_upButton is not null)
					_upButton.Click += new RoutedEventHandler(OnButtonClick);
			}
		}
		private RepeatButton _upButton;

		#endregion

		#region Property

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
				new PropertyMetadata(
					0D,
					(d, e) =>
					{
						var innerSlider = ((SliderPlus)d).InnerSlider;
						if (innerSlider is null)
							return;

						var buffer = Math.Round((double)e.NewValue);

						if (buffer < innerSlider.Minimum)
							buffer = innerSlider.Minimum;

						if (buffer > innerSlider.Maximum)
							buffer = innerSlider.Maximum;

						((SliderPlus)d)._innerSliderValue = buffer; // This must be changed on ahead.
						innerSlider.Value = buffer;
					}));

		public double Maximum
		{
			get { return (double)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}
		public static readonly DependencyProperty MaximumProperty =
			RangeBase.MaximumProperty.AddOwner(
				typeof(SliderPlus),
				new PropertyMetadata(
					10D,
					(d, e) =>
					{
						var innerSlider = ((SliderPlus)d).InnerSlider;
						if (innerSlider is null)
							return;

						var buffer = Math.Floor((double)e.NewValue);

						var maximumThreshold = Math.Ceiling(innerSlider.Minimum) + 1D;
						if (buffer < maximumThreshold)
							innerSlider.Maximum = maximumThreshold;

						innerSlider.Maximum = buffer;
					}));

		public double Minimum
		{
			get { return (double)GetValue(MinimumProperty); }
			set { SetValue(MinimumProperty, value); }
		}
		public static readonly DependencyProperty MinimumProperty =
			RangeBase.MinimumProperty.AddOwner(
				typeof(SliderPlus),
				new PropertyMetadata(
					0D,
					(d, e) =>
					{
						var innerSlider = ((SliderPlus)d).InnerSlider;
						if (innerSlider is null)
							return;

						var buffer = Math.Ceiling((double)e.NewValue);

						var minimumThreshold = Math.Floor(innerSlider.Maximum) - 1D;
						if (buffer < minimumThreshold)
							innerSlider.Minimum = minimumThreshold;

						innerSlider.Minimum = buffer;
					}));

		public double LargeChange
		{
			get { return (double)GetValue(LargeChangeProperty); }
			set { SetValue(LargeChangeProperty, value); }
		}
		public static readonly DependencyProperty LargeChangeProperty =
			RangeBase.LargeChangeProperty.AddOwner(
				typeof(SliderPlus),
				new PropertyMetadata(
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
				new PropertyMetadata(
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
				new PropertyMetadata(
					1D,
					(d, e) => ((SliderPlus)d).Value = ((SliderPlus)d).Minimum));

		#endregion

		private enum Direction
		{
			Down,
			Up,
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			InnerSlider = this.GetTemplateChild("PART_InnerSlider") as Slider;
			DownButton = this.GetTemplateChild("PART_DownButton") as RepeatButton;
			UpButton = this.GetTemplateChild("PART_UpButton") as RepeatButton;
		}

		private double _innerSliderValue;

		private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (InnerSlider.Value == _innerSliderValue)
				return;

			Value = Math.Round(InnerSlider.Value / SmallChange) * SmallChange;
		}

		private void OnButtonClick(object sender, RoutedEventArgs e)
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