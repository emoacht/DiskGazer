using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;

using DiskGazer.Models;
using DiskGazer.ViewModels;

namespace DiskGazer.Views
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel _mainWindowViewModel;

		public MainWindow()
		{
			InitializeComponent();
			this.Loaded += OnLoaded;
			this.SizeChanged += OnSizeChanged;

			_mainWindowViewModel = new MainWindowViewModel(this);
			this.DataContext = _mainWindowViewModel;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var initializeTask = _mainWindowViewModel.InitializeAsync();

			CreateChart();
			ManageColorBar();
			SetMinSize();

			this.SetBinding(
				StatusProperty,
				new Binding("Status")
				{
					Source = _mainWindowViewModel,
					Mode = BindingMode.OneWay,
				});

			this.SetBinding(
				CurrentDiskProperty,
				new Binding("CurrentDisk")
				{
					Source = _mainWindowViewModel,
					Mode = BindingMode.OneWay,
				});

#if !DEBUG
			this.MenuItemOpen.Visibility = Visibility.Collapsed;
#endif
		}

		#region Window management

		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			AdjustChartArea();

			// Binding in XAML will not work when window is maximized.
			this.MenuTop.Width = WindowSupplement.GetClientAreaSize(this).Width;

			if (IndicatesSize)
				IndicateWindowChartSize();
		}

		/// <summary>
		/// Whether to indicate window/chart size
		/// </summary>
		public bool IndicatesSize
		{
			get { return (bool)GetValue(IndicatesSizeProperty); }
			set { SetValue(IndicatesSizeProperty, value); }
		}
		public static readonly DependencyProperty IndicatesSizeProperty =
			DependencyProperty.Register(
				"IndicatesSize",
				typeof(bool),
				typeof(MainWindow),
				new PropertyMetadata(
					false,
					(d, e) =>
					{
						if ((bool)e.NewValue)
							((MainWindow)d).IndicateWindowChartSize();
						else
							((MainWindow)d).ShowStatus(string.Empty);
					}));

		private void IndicateWindowChartSize()
		{
			var rect = WindowSupplement.GetWindowRect(this);
			var innerPlotAreaSize = GetChartInnerPlotAreaSize();

			ShowStatus(string.Format("Window {0}-{1} Chart {2}-{3}",
				rect.Width,
				rect.Height,
				innerPlotAreaSize.Width,
				innerPlotAreaSize.Height));
		}

		protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
		{
			base.OnDpiChanged(oldDpi, newDpi);

			ForceChartRedraw();
		}

		private void SetMinSize()
		{
			this.MinWidth = this.Width;
			this.MinHeight = this.Height;
		}

		private void ForceChartRedraw()
		{
			// Force chart redraw (any other orthodox method does not work). This fires SizeChanged events twice though.
			this.Height += 1;
			this.Height -= 1;
		}

		private void MenuItemFile_SubmenuOpened(object sender, RoutedEventArgs e)
		{
			if (_mainWindowViewModel.Op.IsReady)
			{
				// Fix start button color to prevent the color from being changed by mouse over when screenshot is taken.
				this.ButtonStart.Style = (Style)App.Current.Resources["ButtonStartStill"];
			}
		}

		private void MenuItemFile_SubmenuClosed(object sender, RoutedEventArgs e)
		{
			// When an item in the submenu is clicked, this event seems to be fired after click event of the item.
			// Restore start button color (if fixed).
			ManageStartButton();
		}

		#endregion

		#region Operation

		/// <summary>
		/// Status
		/// </summary>
		public string Status
		{
			get { return (string)GetValue(StatusProperty); }
			set { SetValue(StatusProperty, value); }
		}
		public static readonly DependencyProperty StatusProperty =
			DependencyProperty.Register(
				"Status",
				typeof(string),
				typeof(MainWindow),
				new PropertyMetadata(
					string.Empty,
					(d, e) =>
					{
						var window = (MainWindow)d;
						window.ShowStatus((string)e.NewValue);
						window.ManageStartButton();
					}));

		private void ManageStartButton()
		{
			if (_mainWindowViewModel.Op.IsReady)
			{
				// Ready
				this.ButtonStart.Content = "Start";
				this.ButtonStart.Style = (Style)(App.Current.Resources["ButtonStartReady"]);
				this.ButtonStart.IsEnabled = true;
			}
			else if (_mainWindowViewModel.Op.IsCanceled)
			{
				// Stopping
				this.ButtonStart.Content = "Stop";
				this.ButtonStart.Style = (Style)(App.Current.Resources["ButtonStartReady"]);
				this.ButtonStart.IsEnabled = false;
			}
			else
			{
				// Busy
				this.ButtonStart.Content = "Stop";
				this.ButtonStart.Style = (Style)(App.Current.Resources["ButtonStartBusy"]);
				this.ButtonStart.IsEnabled = true;
			}
		}

		private void ShowStatus(string text)
		{
			const string nameStatus = "MenuItemStatus";

			var menuItemStatus = this.MenuTop.Items.OfType<MenuItem>().FirstOrDefault(x => x.Name == nameStatus);
			if (menuItemStatus is not null) // If MenuItemStatus is already in MenuTop.
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					// Remove MenuItemStatus from MenuTop.
					this.MenuTop.Items.Remove(menuItemStatus);
				}
				else
				{
					// Modify header of MenuItemStatus.
					menuItemStatus.Header = $"[{text}]";
				}
				return;
			}

			if (string.IsNullOrWhiteSpace(text))
				return;

			// Prepare ControlTemplate for MenuItemStatus.
			var templateStatus = new ControlTemplate(typeof(MenuItem));

			var grid = new FrameworkElementFactory(typeof(System.Windows.Controls.Grid));
			grid.SetValue(System.Windows.Controls.Grid.MarginProperty, new Thickness(7, 0, 0, 0));
			templateStatus.VisualTree = grid;

			var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
			presenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
			presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
			grid.AppendChild(presenter);

			// Create MenuItemStatus and add to MenuTop.
			menuItemStatus = new MenuItem
			{
				Name = nameStatus,
				Height = this.MenuTop.Height,
				Template = templateStatus,
				Header = $"[{text}]",
			};

			this.MenuTop.Items.Add(menuItemStatus);
		}

		#endregion

		#region Disk information

		/// <summary>
		/// Current disk
		/// </summary>
		public DiskInfo CurrentDisk
		{
			get { return (DiskInfo)GetValue(CurrentDiskProperty); }
			set { SetValue(CurrentDiskProperty, value); }
		}
		public static readonly DependencyProperty CurrentDiskProperty =
			DependencyProperty.Register(
				"CurrentDisk",
				typeof(DiskInfo),
				typeof(MainWindow),
				new PropertyMetadata(
					null,
					(d, e) => ((MainWindow)d).SetDiskInfo((DiskInfo)e.NewValue)));

		private void SetDiskInfo(DiskInfo info)
		{
			if (info is null)
				return;

			// Prepare outer Grid.
			var outerGrid = this.DiskInfoHost;
			outerGrid.Children.Clear();
			for (int i = 0; i <= 1; i++) // 2 rows
			{
				var row = new RowDefinition { Height = System.Windows.GridLength.Auto };
				outerGrid.RowDefinitions.Add(row);
			}

			// Create inner Grid and add to outer Grid.
			var innerGrid = new System.Windows.Controls.Grid();
			System.Windows.Controls.Grid.SetRow(innerGrid, 1);
			outerGrid.Children.Add(innerGrid);

			// Prepare contents.
			var infoSizeWMI = info.SizeWMI;
			var infoSizePInvoke = info.SizePInvoke;

			var header = new GridElement($"[{info.Name}]");

			var body = new List<List<GridElement>>
			{
				new List<GridElement>
				{
					new GridElement("Physical drive (WMI)"),
					new GridElement(info.PhysicalDrive.ToString(CultureInfo.InvariantCulture), HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Model (WMI)"),
					new GridElement(info.Model, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Vendor (P/Invoke)"),
					new GridElement(info.Vendor, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Product (P/Invoke)"),
					new GridElement(info.Product, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Interface type (WMI)"),
					new GridElement(info.InterfaceType, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Bus type (P/Invoke)"),
					new GridElement(info.BusType, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Media type (WMI Win32_DiskDrive)"),
					new GridElement(info.MediaTypeDiskDrive, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Fixed/Removable (P/Invoke)"),
					new GridElement(info.IsRemovable ? "Removable" : "Fixed", HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Media type (WMI MSFT_PhysicalDisk)"),
					new GridElement(info.MediaTypePhysicalDiskDescription, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Spindle Speed (WMI)"),
					new GridElement(info.SpindleSpeedDescription, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Nominal media rotation rate (P/Invoke)"),
					new GridElement(info.NominalMediaRotationRateDescription, HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Capacity (WMI)"),
					new GridElement($"{infoSizeWMI:n0} Bytes", HorizontalAlignment.Right),
					new GridElement($"({(double)infoSizeWMI / (1024 * 1024):n3} MiB)", HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Capacity (P/Invoke)"),
					new GridElement($"{infoSizePInvoke:n0} Bytes", HorizontalAlignment.Right),
					new GridElement($"({(double)infoSizePInvoke / (1024 * 1024):n3} MiB)", HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Difference"),
					new GridElement($"{(infoSizePInvoke - infoSizeWMI):n0} Bytes", HorizontalAlignment.Right),
					new GridElement($"({(double)(infoSizePInvoke - infoSizeWMI) / (1024 * 1024):n3} MiB)", HorizontalAlignment.Right),
				},
			};

			// Add rows and columns to inner Grid.
			for (int i = 0; i < body.Count; i++) // Rows
			{
				var row = new RowDefinition { Height = System.Windows.GridLength.Auto };
				innerGrid.RowDefinitions.Add(row);
			}
			for (int i = 0; i < body.Max(x => x.Count); i++) // Columns
			{
				var column = new ColumnDefinition { Width = System.Windows.GridLength.Auto };
				innerGrid.ColumnDefinitions.Add(column);
			}

			// Add contents.
			outerGrid.Children.Add(CreateTextBlock(header, 0, 0));

			for (int i = 0; i < innerGrid.RowDefinitions.Count; i++)
			{
				for (int j = 0; j < innerGrid.ColumnDefinitions.Count; j++)
				{
					if (j <= body[i].Count - 1)
						innerGrid.Children.Add(CreateTextBlock(body[i][j], i, j));
				}
			}
		}

		private struct GridElement
		{
			public readonly string Text;
			public readonly HorizontalAlignment Alignment;

			public GridElement(string text, HorizontalAlignment alignment = HorizontalAlignment.Left)
			{
				this.Text = text;
				this.Alignment = alignment;
			}
		}

		private TextBlock CreateTextBlock(GridElement element, int rowIndex, int columnIndex)
		{
			var textBlock = new TextBlock
			{
				Text = element.Text,
				Margin = new Thickness(2),
				HorizontalAlignment = element.Alignment,
			};

			System.Windows.Controls.Grid.SetRow(textBlock, rowIndex);
			System.Windows.Controls.Grid.SetColumn(textBlock, columnIndex);

			return textBlock;
		}

		#endregion

		#region Chart

		private IReadOnlyList<DiskScore> DiskScores => _mainWindowViewModel.DiskScores;

		private Chart _diskChart;

		private const double ChartUnit = 50D;        // Unit length of Y axis
		private const double ChartMaxDefault = 200D; // Default maximum value of Y axis
		private const double ChartMinDefault = 0D;   // Default minimum value of Y axis

		private readonly Color[] _colorBarColors = // Colors for color bar
		{
			Color.FromRgb(255,  0,255),
			Color.FromRgb(255,  0,153),
			Color.FromRgb(255,  0,  0),
			Color.FromRgb(255,153,  0),
			Color.FromRgb(255,255,  0),
			Color.FromRgb(153,255,  0),
			Color.FromRgb(  0,235,  0),
			Color.FromRgb(  0,255,153),
			Color.FromRgb(  0,255,255),
		};

		private int _colorBarIndex = 8; // Index of selected color in color bar

		/// <summary>
		/// Maximum value of Y axis
		/// </summary>
		public double ChartMax
		{
			get { return (double)GetValue(ChartMaxProperty); }
			set { SetValue(ChartMaxProperty, value); }
		}
		public static readonly DependencyProperty ChartMaxProperty =
			DependencyProperty.Register(
				"ChartMax",
				typeof(double),
				typeof(MainWindow),
				new PropertyMetadata(
					ChartMaxDefault,
					(d, e) =>
					{
						var window = (MainWindow)d;

						if ((window.SliderChartMax.IsFocused | window.SliderChartMax.IsMouseOver) &&
							(window.SliderChartMin.Value + ChartUnit > (double)e.NewValue))
							window.SliderChartMin.Value = (double)e.NewValue - ChartUnit;

						if (window.IsChartMaxFixed)
							window.AdjustChartArea();
					},
					(d, baseValue) => Math.Round((double)baseValue / ChartUnit) * ChartUnit));

		/// <summary>
		/// Minimum value of Y axis
		/// </summary>
		public double ChartMin
		{
			get { return (double)GetValue(ChartMinProperty); }
			set { SetValue(ChartMinProperty, value); }
		}
		public static readonly DependencyProperty ChartMinProperty =
			DependencyProperty.Register(
				"ChartMin",
				typeof(double),
				typeof(MainWindow),
				new PropertyMetadata(
					ChartMinDefault,
					(d, e) =>
					{
						var window = (MainWindow)d;

						if ((window.SliderChartMin.IsFocused | window.SliderChartMin.IsMouseOver) &&
							((double)e.NewValue + ChartUnit > window.SliderChartMax.Value))
							window.SliderChartMax.Value = (double)e.NewValue + ChartUnit;

						if (window.IsChartMinFixed)
							window.AdjustChartArea();
					},
					(d, baseValue) => Math.Round((double)baseValue / ChartUnit) * ChartUnit));

		/// <summary>
		/// Whether maximum value of Y axle is fixed
		/// </summary>
		public bool IsChartMaxFixed
		{
			get { return (bool)GetValue(IsChartMaxFixedProperty); }
			set { SetValue(IsChartMaxFixedProperty, value); }
		}
		public static readonly DependencyProperty IsChartMaxFixedProperty =
			DependencyProperty.Register(
				"IsChartMaxFixed",
				typeof(bool),
				typeof(MainWindow),
				new PropertyMetadata(
					false,
					(d, e) => ((MainWindow)d).AdjustChartArea()));

		/// <summary>
		/// Whether minimum value of Y axle is fixed
		/// </summary>
		public bool IsChartMinFixed
		{
			get { return (bool)GetValue(IsChartMinFixedProperty); }
			set { SetValue(IsChartMinFixedProperty, value); }
		}
		public static readonly DependencyProperty IsChartMinFixedProperty =
			DependencyProperty.Register(
				"IsChartMinFixed",
				typeof(bool),
				typeof(MainWindow),
				new PropertyMetadata(
					false,
					(d, e) => ((MainWindow)d).AdjustChartArea()));

		/// <summary>
		/// Whether to slide chart line to current location so as to make it visible
		/// </summary>
		public bool SlidesLine
		{
			get { return (bool)GetValue(SlidesLineProperty); }
			set { SetValue(SlidesLineProperty, value); }
		}
		public static readonly DependencyProperty SlidesLineProperty =
			DependencyProperty.Register(
				"SlidesLine",
				typeof(bool),
				typeof(MainWindow),
				new PropertyMetadata(
					true,
					(d, e) => ((MainWindow)d).DrawChartLine(DrawMode.RefreshPinnedChart)));

		private DpiScale _initialDpi;

		private void CreateChart()
		{
			_initialDpi = VisualTreeHelper.GetDpi(this);

			var labelFont = GetLabelFont(1); // Axis label font

			var primaryBackColor = System.Drawing.Color.FromArgb(10, 10, 10);   // Primary background color of inner plot area
			var secondaryBackColor = System.Drawing.Color.FromArgb(60, 60, 60); // Secondary background color of inner plot area
			var majorLineColor = System.Drawing.Color.FromArgb(120, 120, 120);  // Line color of major grid
			var minorLineColor = System.Drawing.Color.FromArgb(80, 80, 80);     // Line color of minor grid

			// Prepare Chart and add to WindowsFormsHost.
			_diskChart = new Chart();
			this.ChartHost.Child = _diskChart;

			// Prepare ChartArea and add to Chart.
			var chartArea = new ChartArea();
			chartArea.BackColor = primaryBackColor;
			chartArea.BackSecondaryColor = secondaryBackColor;
			chartArea.BackGradientStyle = GradientStyle.TopBottom;
			chartArea.Position = new ElementPosition(0, 0, 100, 100); // This is required for ChartArea to fill Chart object completely.

			// Set X axis.
			chartArea.AxisX.IsLabelAutoFit = false;
			chartArea.AxisX.LabelStyle.Font = labelFont;
			chartArea.AxisX.MajorGrid.LineColor = majorLineColor;
			chartArea.AxisX.MajorTickMark.Enabled = false;

			chartArea.AxisX.MinorGrid.Enabled = true;
			chartArea.AxisX.MinorGrid.LineColor = minorLineColor;

			// Set Y axis.
			chartArea.AxisY.IsLabelAutoFit = false;
			chartArea.AxisY.LabelStyle.Font = labelFont;
			chartArea.AxisY.MajorGrid.LineColor = majorLineColor;
			chartArea.AxisY.MajorTickMark.Enabled = false;

			chartArea.AxisY.MinorGrid.Enabled = true;
			chartArea.AxisY.MinorGrid.LineColor = minorLineColor;

			_diskChart.ChartAreas.Add(chartArea);

			// Draw blank Series. This is required for inner plot area to be drawn.
			DrawChartLine(DrawMode.Clear);

			AdjustChartArea();
		}

		private void AdjustChartArea()
		{
			if (_diskChart is null)
				return;

			var windowDpi = VisualTreeHelper.GetDpi(this);

			// ------------------
			// Set size of Chart.
			// ------------------
			// Width will be automatically resized except initial adjustment where it is required to set ChartArea.InnerPlotPosition.X.
			var clientAreaSize = WindowSupplement.GetClientAreaSize(this);
			_diskChart.Width = (int)clientAreaSize.Width;
			_diskChart.Height = (int)(clientAreaSize.Height - this.GridDashboard.ActualHeight * windowDpi.DpiScaleY);

			var chartArea = _diskChart.ChartAreas[0];

			// -----------------------------------------------------
			// Adjust maximum and minimum values in scales of Chart.
			// -----------------------------------------------------
			// X axis
			chartArea.AxisX.Maximum = Settings.Current.AreaLocation + Settings.Current.AreaSize;
			chartArea.AxisX.Minimum = Settings.Current.AreaLocation;

			// Y axis
			double chartMax;
			double chartMin;

			if (IsChartMaxFixed)
			{
				chartMax = ChartMax;
			}
			else
			{
				chartMax = (DiskScores[0].Data is not null)
					? Math.Ceiling(DiskScores[0].Data.Values.Max() / ChartUnit) * ChartUnit
					: ChartMaxDefault;
			}

			if (IsChartMinFixed)
			{
				chartMin = ChartMin;
			}
			else
			{
				chartMin = (DiskScores[0].Data is not null)
					? Math.Floor(DiskScores[0].Data.Values.Min() / ChartUnit) * ChartUnit
					: ChartMinDefault;
			}

			if (chartMin + ChartUnit > chartMax) // If relationship between maximum and minimum values is screwed.
			{
				if ((chartMin == 0D) | // Case where minimum value is already 0.
					(!IsChartMaxFixed &&
					 IsChartMinFixed)) // Case where maximum value is not fixed and minimum value is fixed.
				{
					chartMax = chartMin + ChartUnit;
				}
				else // Any other case.
				{
					chartMin = chartMax - ChartUnit;
				}
			}

			chartArea.AxisY.Maximum = chartMax;
			chartArea.AxisY.Minimum = chartMin;

			// -----------------------------------------------------
			// Adjust size and position of inner plot area of Chart.
			// -----------------------------------------------------
			int digitX = 4; // Digit number of maximum scale in X axis
			int digitY = 3; // Digit number of maximum scale in Y axis

			if (DiskScores[0].Data is not null)
			{
				digitX = Math.Max(digitX, chartArea.AxisX.Maximum.ToString(CultureInfo.InvariantCulture).Length);
				digitY = Math.Max(digitY, chartArea.AxisY.Maximum.ToString(CultureInfo.InvariantCulture).Length);
			}

			var labelFont = GetLabelFont(windowDpi.DpiScaleX / _initialDpi.DpiScaleX);

			chartArea.AxisX.LabelStyle.Font = labelFont;
			chartArea.AxisY.LabelStyle.Font = labelFont;

			var labelX = GetSize(digitX, labelFont);
			var labelY = GetSize(digitY, labelFont);

			// Note that all properties are percentage.
			chartArea.InnerPlotPosition.Auto = false;

			chartArea.InnerPlotPosition.X = GetPerc(labelY.Width + 2, _diskChart.Width);
			chartArea.InnerPlotPosition.Y = GetPerc(labelY.Height / 2, _diskChart.Height);

			chartArea.InnerPlotPosition.Width = 100f - GetPerc((labelY.Width + 2) + (labelX.Width / 2), _diskChart.Width);
			chartArea.InnerPlotPosition.Height = 100f - GetPerc((labelY.Height / 2) + (labelX.Height * 2), _diskChart.Height);

			// --------------------------------
			// Adjust scale intervals of Chart.
			// --------------------------------
			var shortest = 40D * windowDpi.DpiScaleY;

			// X axis
			double innerX = _diskChart.Width * (chartArea.InnerPlotPosition.Width / 100D);
			double intervalX = 256D; // This is fallback number in case appropriate number can not be found.

			foreach (int i in Enumerable.Range(2, 9)) // 10 means very large number.
			{
				var buffer = innerX / Math.Pow(2, i);

				if (buffer < shortest)
				{
					intervalX = Settings.Current.AreaSize / Math.Pow(2, i - 1);
					break;
				}
			}

			chartArea.AxisX.Interval = intervalX;
			chartArea.AxisX.MinorGrid.Interval = intervalX / 2;
			chartArea.AxisX.LabelStyle.Interval = intervalX * 2; // 1 label per 2 major grids

			// Y axis
			double rangeY = chartMax - chartMin;
			double innerY = _diskChart.Height * (chartArea.InnerPlotPosition.Height / 100D);
			double intervalY = 100D; // This is fallback number in case appropriate number can not be found.
			var intervals = new double[] { 5, 10, 20, 25, 50, 100, 200, 500, 1000 }; // Numbers to be used as interval

			foreach (var interval in intervals)
			{
				if ((interval < 100) && (rangeY % interval != 0))
					continue;

				if (rangeY <= interval)
					break;

				var buffer = innerY * (interval / rangeY);

				if (buffer > shortest)
				{
					intervalY = interval;
					break;
				}
			}

			chartArea.AxisY.Interval = intervalY;
			chartArea.AxisY.MinorGrid.Interval = intervalY / 2;
		}

		private System.Drawing.Font GetLabelFont(double scale) =>
			new(this.FontFamily.ToString(), (float)((this.FontSize - 3F) * scale));

		private static float GetPerc(double targetLength, int baseLength)
		{
			var sampleNumber = targetLength / (double)baseLength;

			return Math.Min((float)sampleNumber * 100F, 100F);
		}

		private static Size GetSize(int num, System.Drawing.Font font)
		{
			var sampleText = Enumerable.Repeat("9", num).Aggregate((total, next) => total + next);

			return GetSize(sampleText, font);
		}

		private static Size GetSize(string text, System.Drawing.Font font)
		{
			// System.Windows.Media.FormattedText method is not accurate (too small).
			var sampleSize = System.Windows.Forms.TextRenderer.MeasureText(text, font);

			return new Size(sampleSize.Width, sampleSize.Height);
		}

		private Size GetChartInnerPlotAreaSize()
		{
			if (_diskChart is null)
				return new Size(0, 0);

			var chartArea = _diskChart.ChartAreas[0];

			return new Size(
				Math.Round(_diskChart.Width * (chartArea.InnerPlotPosition.Width / 100F)),
				Math.Round(_diskChart.Height * (chartArea.InnerPlotPosition.Height / 100F)));
		}

		/// <summary>
		/// Draws chart line.
		/// </summary>
		/// <param name="mode">Draw mode of chart</param>
		internal void DrawChartLine(DrawMode mode)
		{
			var lineColor = System.Drawing.Color.FromArgb(
				_colorBarColors[_colorBarIndex].R,
				_colorBarColors[_colorBarIndex].G,
				_colorBarColors[_colorBarIndex].B); // Line color of Series

			// Remove existing Series.
			switch (mode)
			{
				case DrawMode.ClearCompletely:
					// Remove all Series.
					_diskChart.Series.Clear();
					break;

				case DrawMode.Clear:
				case DrawMode.DrawNewChart:
					// Remove seriesOne and Series that has no corresponding data anymore (if any).
					var seriesOnes = _diskChart.Series.Where(x => !DiskScores.Skip(1).Select(y => y.Guid).Contains(x.Name)).ToArray();
					for (int i = seriesOnes.Length - 1; i >= 0; i--)
						_diskChart.Series.Remove(seriesOnes[i]);
					break;
			}

			// Modify existing Series (if any).
			switch (mode)
			{
				case DrawMode.DrawNewChart:
				case DrawMode.RefreshPinnedChart:
					if (SlidesLine)
					{
						// Slide chart line to current location so as to make it visible.
						foreach (var seriesTwo in _diskChart.Series)
						{
							var slideLength = Settings.Current.AreaLocation - seriesTwo.Points.Min(x => x.XValue);

							foreach (var point in seriesTwo.Points)
								point.XValue += slideLength;
						}
					}
					else
					{
						// Return to original location.
						switch (mode)
						{
							case DrawMode.RefreshPinnedChart:
								foreach (var seriesTwo in _diskChart.Series)
								{
									var scoreTwo = DiskScores.FirstOrDefault(d => d.Guid == seriesTwo.Name);
									if (scoreTwo is not null)
									{
										var slideLength = scoreTwo.AreaLocation - seriesTwo.Points.Min(p => p.XValue);

										foreach (var point in seriesTwo.Points)
											point.XValue += slideLength;
									}
								}
								break;
						}
					}
					break;

				case DrawMode.PinCurrentChart:
					// Darken line color of seriesOne.
					var seriesOne = _diskChart.Series.FirstOrDefault(s => s.Name == DiskScores[0].Guid);
					if (seriesOne is not null)
					{
						// In ControlPaint.Dark method, 2nd parameter starts from -0.5 (not darkened yet) to 1 (fully darkened to be black).
						seriesOne.Color = System.Windows.Forms.ControlPaint.Dark(lineColor, (float)-0.2);
					}
					break;
			}

			// Prepare Series and add to Chart.
			switch (mode)
			{
				case DrawMode.Clear:
				case DrawMode.ClearCompletely:
				case DrawMode.DrawNewChart:
					var seriesOne = new Series(); // Current Series

					// Set unique name of Series (This is to be used to identify it later).
					seriesOne.Name = DiskScores[0].Guid;

					// Set type and line color of Series.
					seriesOne.ChartType = SeriesChartType.FastLine;
					seriesOne.Color = lineColor;

					// Set points of Series.
					switch (mode)
					{
						case DrawMode.Clear:
						case DrawMode.ClearCompletely:
							// At least one point in one Series is required to draw Chart.
							seriesOne.Points.Add(new DataPoint(0, 0));
							break;

						case DrawMode.DrawNewChart:
							if (DiskScores[0].Data is not null)
							{
								var dataList = new SortedList<double, double>(DiskScores[0].Data);

								foreach (var data in dataList)
									seriesOne.Points.AddXY(data.Key, data.Value);
							}
							break;
					}

					_diskChart.Series.Add(seriesOne);

					if (DiskScores[0].Data is not null)
						AdjustChartArea();
					break;
			}
		}

		/// <summary>
		/// Manages color bar for chart line color.
		/// </summary>
		private void ManageColorBar()
		{
			const double sizeNormal = 22D;
			const double sizeSelected = 30D;

			// If initial adjustment, fill color bar with buttons.
			if (this.ColorBarHost.Children.Count == 0)
			{
				for (int i = 0; i < _colorBarColors.Length; i++)
				{
					var colButton = new Button
					{
						Background = new SolidColorBrush(_colorBarColors[i]),
						Tag = i, // Store index number in Tag.
						Template = (ControlTemplate)(App.Current.Resources["ButtonColorTemplate"]),
						Margin = new Thickness(0, 0, 4, 0),
						VerticalAlignment = VerticalAlignment.Bottom,
					};

					colButton.Click += (sender, e) =>
					{
						var button = sender as Button;
						if (button is not null)
							_colorBarIndex = (int)button.Tag;

						ManageColorBar();
					};

					this.ColorBarHost.Children.Add(colButton);
				}
			}

			// Adjust each button size.
			foreach (var button in this.ColorBarHost.Children.OfType<Button>())
			{
				button.Width = button.Height = ((int)button.Tag == _colorBarIndex)
					? sizeSelected
					: sizeNormal;
			}
		}

		#endregion

		#region Command

		#region ApplicationCommands.Open

		/// <summary>
		/// Opens Monitor window.
		/// </summary>
		private void OpenExecuted(object target, ExecutedRoutedEventArgs e)
		{
			try
			{
				var monitorWindow = this.OwnedWindows.OfType<MonitorWindow>().FirstOrDefault();
				if (monitorWindow is not null)
				{
					// Activate Monitor window.
					WindowSupplement.ActivateWindow(monitorWindow);
					return;
				}

				// Open Monitor Window.
				monitorWindow = new MonitorWindow { Owner = this };
				monitorWindow.Show();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message,
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void CanOpenExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		#endregion

		#region ApplicationCommands.Close

		/// <summary>
		/// Closes this application.
		/// </summary>
		private void CloseExecuted(object target, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		private void CanCloseExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		#endregion

		#endregion
	}
}