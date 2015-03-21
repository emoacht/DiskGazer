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
using MonitorAware.Models;

namespace DiskGazer.Views
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel _mainWindowViewModel;

		public MainWindow()
		{
			InitializeComponent();

			_mainWindowViewModel = new MainWindowViewModel(this);
			this.DataContext = _mainWindowViewModel;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			CreateAddChart();
			ManageColorBar();

			if (_mainWindowViewModel != null)
			{
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
			}

			this.SetBinding(
				WindowDpiProperty,
				new Binding("WindowHandler.WindowDpi")
				{
					Source = MonitorProperty,
					Mode = BindingMode.OneWay
				});

			MonitorProperty.WindowHandler.DpiChanged += OnDpiChanged;

			SetMinSize();
			ForceChartRedraw();
		}


		#region Window management

		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_diskChart != null)
				AdjustChartAppearance();

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
				new FrameworkPropertyMetadata(
					false,
					(d, e) =>
					{
						if ((bool)e.NewValue)
							((MainWindow)d).IndicateWindowChartSize();
						else
							((MainWindow)d).ShowStatus(String.Empty);
					}));

		/// <summary>
		/// Indicate window/chart size.
		/// </summary>
		private void IndicateWindowChartSize()
		{
			var rct = WindowSupplement.GetWindowRect(this);
			var innerPlotAreaSize = ChartInnerPlotAreaSize();

			ShowStatus(String.Format("Window {0}-{1} Chart {2}-{3}",
				rct.Width,
				rct.Height,
				innerPlotAreaSize.Width,
				innerPlotAreaSize.Height));
		}

		/// <summary>
		/// Window DPI
		/// </summary>
		public Dpi WindowDpi
		{
			get { return (Dpi)GetValue(WindowDpiProperty); }
			set { SetValue(WindowDpiProperty, value); }
		}
		public static readonly DependencyProperty WindowDpiProperty =
			DependencyProperty.Register(
				"WindowDpi",
				typeof(Dpi),
				typeof(MainWindow),
				new FrameworkPropertyMetadata(
					Dpi.Default,
					(d, e) =>
					{
						if (((Dpi)e.NewValue).Equals((Dpi)e.OldValue))
							return;

						var window = (MainWindow)d;
						window.SetMinSize();
						window.ForceChartRedraw();
					}));


		private double WindowDpiFactorY
		{
			get { return (double)MonitorProperty.WindowHandler.WindowDpi.Y / (double)Dpi.Default.Y; }
		}

		private void OnDpiChanged(object sender, DpiChangedEventArgs e)
		{
			ForceChartRedraw();
		}

		private void SetMinSize()
		{
			var window = new MainWindow();
			try
			{
				this.MinWidth = window.Width * (double)MonitorProperty.WindowHandler.WindowDpi.X / (double)MonitorProperty.WindowHandler.SystemDpi.X;
				this.MinHeight = window.Height * (double)MonitorProperty.WindowHandler.WindowDpi.Y / (double)MonitorProperty.WindowHandler.SystemDpi.Y;
			}
			finally
			{
				window.Close();
			}
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
				new FrameworkPropertyMetadata(
					String.Empty,
					(d, e) =>
					{
						((MainWindow)d).ShowStatus((string)e.NewValue);
						((MainWindow)d).ManageStartButton();
					}));

		/// <summary>
		/// Manage start button appearance.
		/// </summary>
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

		/// <summary>
		/// Show status
		/// </summary>
		/// <param name="text">Status text</param>
		private void ShowStatus(string text)
		{
			const string nameStatus = "MenuItemStatus";

			var menuItemStatus = this.MenuTop.Items.OfType<MenuItem>().FirstOrDefault(x => x.Name == nameStatus);
			if (menuItemStatus != null) // If MenuItemStatus is already in MenuTop.
			{
				if (String.IsNullOrWhiteSpace(text))
				{
					// Remove MenuItemStatus from MenuTop.
					this.MenuTop.Items.Remove(menuItemStatus);
				}
				else
				{
					// Modify header of MenuItemStatus.
					menuItemStatus.Header = String.Format("[{0}]", text);
				}
				return;
			}

			if (String.IsNullOrWhiteSpace(text))
				return;

			// Prepare ControlTemplate for MenuItemStatus.
			var templateStatus = new ControlTemplate(typeof(MenuItem));

			var gd = new FrameworkElementFactory(typeof(System.Windows.Controls.Grid));
			gd.SetValue(System.Windows.Controls.Grid.MarginProperty, new Thickness(7, 0, 0, 0));
			templateStatus.VisualTree = gd;

			var cp = new FrameworkElementFactory(typeof(ContentPresenter));
			cp.SetValue(ContentPresenter.ContentSourceProperty, "Header");
			cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
			gd.AppendChild(cp);

			// Create MenuItemStatus and add to MenuTop.
			menuItemStatus = new MenuItem
			{
				Name = nameStatus,
				Height = this.MenuTop.Height,
				Template = templateStatus,
				Header = String.Format("[{0}]", text),
			};

			this.MenuTop.Items.Add(menuItemStatus);
		}

		#endregion


		#region Disk Information

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
				new FrameworkPropertyMetadata(
					null,
					(d, e) => ((MainWindow)d).SetDiskInfo((DiskInfo)e.NewValue)));

		/// <summary>
		/// Set disk information.
		/// </summary>
		/// <param name="info">Disk information</param>
		private void SetDiskInfo(DiskInfo info)
		{
			if (info == null)
				return;

			// Create outer Grid and add to MenuItemDiskInfo.
			var gridOuter = new System.Windows.Controls.Grid();
			for (int i = 0; i <= 1; i++) // 2 rows
			{
				var rowNew = new RowDefinition { Height = System.Windows.GridLength.Auto };
				gridOuter.RowDefinitions.Add(rowNew);
			}
			this.MenuItemDiskInfo.Items.Clear();
			this.MenuItemDiskInfo.Items.Add(gridOuter);

			// Create inner Grid and add to outer Grid.
			var gridInner = new System.Windows.Controls.Grid();
			System.Windows.Controls.Grid.SetRow(gridInner, 1);
			gridOuter.Children.Add(gridInner);

			// Prepare contents.
			var infoSizeWMI = info.SizeWMI;
			var infoSizePInvoke = info.SizePInvoke;

			var header = new GridElement(String.Format("[{0}]", info.Name));

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
					new GridElement(String.Format("{0} Bytes", infoSizeWMI), HorizontalAlignment.Right),
					new GridElement(String.Format("({0:f3} MiB)", (double)infoSizeWMI / (1024 * 1024)), HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Capacity (P/Invoke)"),
					new GridElement(String.Format("{0} Bytes", infoSizePInvoke), HorizontalAlignment.Right),
					new GridElement(String.Format("({0:f3} MiB)", (double)infoSizePInvoke / (1024 * 1024)), HorizontalAlignment.Right),
				},
				new List<GridElement>
				{
					new GridElement("Difference"),
					new GridElement(String.Format("{0} Bytes", (infoSizePInvoke - infoSizeWMI)), HorizontalAlignment.Right),
					new GridElement(String.Format("({0:f3} MiB)", (double)(infoSizePInvoke - infoSizeWMI) / (1024 * 1024)), HorizontalAlignment.Right),
				},
			};

			// Add rows and columns to inner Grid.
			for (int i = 0; i < body.Count; i++) // Rows
			{
				var rowNew = new RowDefinition { Height = System.Windows.GridLength.Auto };
				gridInner.RowDefinitions.Add(rowNew);
			}
			for (int i = 0; i < body.Max(x => x.Count); i++) // Columns
			{
				var columnNew = new ColumnDefinition { Width = System.Windows.GridLength.Auto };
				gridInner.ColumnDefinitions.Add(columnNew);
			}

			// Add contents.
			gridOuter.Children.Add(CreateTextBlock(header, 0, 0));

			for (int i = 0; i < gridInner.RowDefinitions.Count; i++)
			{
				for (int j = 0; j < gridInner.ColumnDefinitions.Count; j++)
				{
					if (j <= body[i].Count - 1)
						gridInner.Children.Add(CreateTextBlock(body[i][j], i, j));
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

		private TextBlock CreateTextBlock(GridElement element, int indexRow, int indexColumn)
		{
			var textNew = new TextBlock
			{
				Text = element.Text,
				Margin = new Thickness(2),
				HorizontalAlignment = element.Alignment,
			};

			System.Windows.Controls.Grid.SetRow(textNew, indexRow);
			System.Windows.Controls.Grid.SetColumn(textNew, indexColumn);

			return textNew;
		}

		#endregion


		#region Chart

		private IReadOnlyList<DiskScore> DiskScores
		{
			get { return _mainWindowViewModel.DiskScores; }
		}

		private Chart _diskChart;

		private const double _chartUnit = 50D;        // Unit length of Y axis
		private const double _chartMaxDefault = 200D; // Default maximum value of Y axis
		private const double _chartMinDefault = 0D;   // Default minimum value of Y axis

		private readonly Color[] _colBar = // Colors for color bar
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

		private int _indexColSelected = 8; // Index of color

		/// <summary>
		/// Maximum value of Y axle
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
				new FrameworkPropertyMetadata(
					_chartMaxDefault,
					(d, e) =>
					{
						var window = (MainWindow)d;

						if ((window.SliderChartMax.IsFocused | window.SliderChartMax.IsMouseOver) &&
							(window.SliderChartMin.Value + _chartUnit > (double)e.NewValue))
							window.SliderChartMin.Value = (double)e.NewValue - _chartUnit;

						if (window.IsChartMaxFixed)
							window.AdjustChartAppearance();
					},
					(d, baseValue) => Math.Round((double)baseValue / _chartUnit) * _chartUnit));

		/// <summary>
		/// Minimum value of Y axle
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
				new FrameworkPropertyMetadata(
					_chartMinDefault,
					(d, e) =>
					{
						var window = (MainWindow)d;

						if ((window.SliderChartMin.IsFocused | window.SliderChartMin.IsMouseOver) &&
							((double)e.NewValue + _chartUnit > window.SliderChartMax.Value))
							window.SliderChartMax.Value = (double)e.NewValue + _chartUnit;

						if (window.IsChartMinFixed)
							window.AdjustChartAppearance();
					},
					(d, baseValue) => Math.Round((double)baseValue / _chartUnit) * _chartUnit));

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
				new FrameworkPropertyMetadata(
					false,
					(d, e) => ((MainWindow)d).AdjustChartAppearance()));

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
				new FrameworkPropertyMetadata(
					false,
					(d, e) => ((MainWindow)d).AdjustChartAppearance()));

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
				new FrameworkPropertyMetadata(
					true,
					(d, e) => ((MainWindow)d).DrawChart(DrawMode.RefreshPinnedChart)));

		private void MenuItemPinLine_Clicked(object sender, RoutedEventArgs e)
		{
			// Ping current chart line.
			if (_mainWindowViewModel.PinLineCommand.CanExecute())
			{
				_mainWindowViewModel.PinLineCommand.Execute();
				DrawChart(DrawMode.PinCurrentChart);
			}
		}

		private void MenuItemClearLines_Clicked(object sender, RoutedEventArgs e)
		{
			// Clear all chart lines.
			if (_mainWindowViewModel.ClearLinesCommand.CanExecute())
			{
				_mainWindowViewModel.ClearLinesCommand.Execute();
				DrawChart(DrawMode.ClearCompletely);
			}
		}

		/// <summary>
		/// Create and add chart.
		/// </summary>
		private void CreateAddChart()
		{
			var chartFont = new System.Drawing.Font(this.FontFamily.ToString(), (float)this.FontSize - 3F); // To be considered.

			var colBack = System.Drawing.Color.FromArgb(10, 10, 10);      // Back color of inner plot area
			var colBack2ndry = System.Drawing.Color.FromArgb(60, 60, 60); // Secondary back color of inner plot area
			var colMajor = System.Drawing.Color.FromArgb(120, 120, 120);  // Line color of major grid
			var colMinor = System.Drawing.Color.FromArgb(80, 80, 80);     // Line color of minor grid

			// Prepare Chart and add to WindowsFormsHost.
			_diskChart = new Chart();
			this.ChartHost.Child = _diskChart;

			// Prepare ChartArea and add to Chart.
			var chartAreaOne = new ChartArea();
			chartAreaOne.BackColor = colBack;
			chartAreaOne.BackSecondaryColor = colBack2ndry;
			chartAreaOne.BackGradientStyle = GradientStyle.TopBottom;
			chartAreaOne.Position = new ElementPosition(0, 0, 100, 100); // This is required for ChartArea to fill Chart object completely.

			// Set X axis.
			chartAreaOne.AxisX.IsLabelAutoFit = false;
			chartAreaOne.AxisX.LabelStyle.Font = chartFont;
			chartAreaOne.AxisX.MajorGrid.LineColor = colMajor;
			chartAreaOne.AxisX.MajorTickMark.Enabled = false;

			chartAreaOne.AxisX.MinorGrid.Enabled = true;
			chartAreaOne.AxisX.MinorGrid.LineColor = colMinor;

			// Set Y axis.
			chartAreaOne.AxisY.IsLabelAutoFit = false;
			chartAreaOne.AxisY.LabelStyle.Font = chartFont;
			chartAreaOne.AxisY.MajorGrid.LineColor = colMajor;
			chartAreaOne.AxisY.MajorTickMark.Enabled = false;

			chartAreaOne.AxisY.MinorGrid.Enabled = true;
			chartAreaOne.AxisY.MinorGrid.LineColor = colMinor;

			_diskChart.ChartAreas.Add(chartAreaOne);

			// Draw blank Series. This is required for inner plot area to be drawn.
			DrawChart(DrawMode.Clear);

			AdjustChartAppearance();
		}

		private void AdjustChartAppearance()
		{
			if (_diskChart == null)
				return;

			// ------------------
			// Set size of Chart.
			// ------------------
			// Width will be automatically resized except initial adjustment where it is required to set ChartArea.InnerPlotPosition.X.
			_diskChart.Width = (int)WindowSupplement.GetClientAreaSize(this).Width;
			_diskChart.Height = (int)(WindowSupplement.GetClientAreaSize(this).Height - this.GridDashboard.ActualHeight * WindowDpiFactorY);

			var chartAreaOne = _diskChart.ChartAreas[0];

			// -----------------------------------------------------
			// Adjust maximum and minimum values in scales of Chart.
			// -----------------------------------------------------
			// X axis
			chartAreaOne.AxisX.Maximum = Settings.Current.AreaLocation + Settings.Current.AreaSize;
			chartAreaOne.AxisX.Minimum = Settings.Current.AreaLocation;

			// Y axis
			double chartMax;
			double chartMin;

			if (IsChartMaxFixed)
			{
				chartMax = ChartMax;
			}
			else
			{
				chartMax = (DiskScores[0].Data != null)
					? Math.Ceiling(DiskScores[0].Data.Values.Max() / _chartUnit) * _chartUnit
					: _chartMaxDefault;
			}

			if (IsChartMinFixed)
			{
				chartMin = ChartMin;
			}
			else
			{
				chartMin = (DiskScores[0].Data != null)
					? Math.Floor(DiskScores[0].Data.Values.Min() / _chartUnit) * _chartUnit
					: _chartMinDefault;
			}

			if (chartMin + _chartUnit > chartMax) // If relationship between maximum and minimum values is screwed.
			{
				if ((chartMin == 0D) | // Case where minimum value is already 0.
					(!IsChartMaxFixed &&
					 IsChartMinFixed)) // Case where maximum value is not fixed and minimum value is fixed.
				{
					chartMax = chartMin + _chartUnit;
				}
				else // Any other case.
				{
					chartMin = chartMax - _chartUnit;
				}
			}

			chartAreaOne.AxisY.Maximum = chartMax;
			chartAreaOne.AxisY.Minimum = chartMin;

			// -----------------------------------------------------
			// Adjust size and position of inner plot area of Chart.
			// -----------------------------------------------------
			int digitX = 4; // Digit number of maximum scale in X axis
			int digitY = 3; // Digit number of maximum scale in Y axis

			if (DiskScores[0].Data != null)
			{
				digitX = Math.Max(digitX, chartAreaOne.AxisX.Maximum.ToString(CultureInfo.InvariantCulture).Length);
				digitY = Math.Max(digitY, chartAreaOne.AxisY.Maximum.ToString(CultureInfo.InvariantCulture).Length);
			}

			var labelX = TextSize(digitX, chartAreaOne.AxisX.LabelStyle.Font);
			var labelY = TextSize(digitY, chartAreaOne.AxisY.LabelStyle.Font);

			// Note that all properties are percentage.
			chartAreaOne.InnerPlotPosition.Auto = false;

			chartAreaOne.InnerPlotPosition.X = GetPerc(labelY.Width + 2, _diskChart.Width);
			chartAreaOne.InnerPlotPosition.Y = GetPerc(labelY.Height / 2, _diskChart.Height);

			chartAreaOne.InnerPlotPosition.Width = 100f - GetPerc((labelY.Width + 2) + (labelX.Width / 2), _diskChart.Width);
			chartAreaOne.InnerPlotPosition.Height = 100f - GetPerc((labelY.Height / 2) + (labelX.Height * 2), _diskChart.Height);

			// --------------------------------
			// Adjust scale intervals of Chart.
			// --------------------------------
			var shortest = 40D * WindowDpiFactorY;

			// X axis
			double innerX = _diskChart.Width * (chartAreaOne.InnerPlotPosition.Width / 100D);
			double intervalX = 256D; // This is fallback number in case appropriate number can not be found.

			for (int i = 2; i <= 10; i++) // 10 means very large number.
			{
				var interval = innerX / Math.Pow(2, i);

				if (interval < shortest)
				{
					intervalX = Settings.Current.AreaSize / Math.Pow(2, i - 1);
					break;
				}
			}

			chartAreaOne.AxisX.Interval = intervalX;
			chartAreaOne.AxisX.MinorGrid.Interval = intervalX / 2;
			chartAreaOne.AxisX.LabelStyle.Interval = intervalX * 2; // 1 label per 2 major grids

			// Y axis
			double innerY = _diskChart.Height * (chartAreaOne.InnerPlotPosition.Height / 100D);
			double intervalY = 100D; // This is fallback number in case appropriate number can not be found.
			var intervals = new double[] { 5, 10, 20, 25, 50, 100, 200, 500, 1000 }; // Numbers to be used as interval

			for (int i = 0; i < intervals.Length; i++)
			{
				if ((chartMax - chartMin) % intervals[i] > 0D)
					continue;

				var interval = innerY * intervals[i] / (chartMax - chartMin);

				if (interval > shortest)
				{
					intervalY = intervals[i];
					break;
				}
			}

			chartAreaOne.AxisY.Interval = intervalY;
			chartAreaOne.AxisY.MinorGrid.Interval = intervalY / 2;
		}

		private float GetPerc(double targetLength, int baseLength)
		{
			var sampleNum = targetLength / (double)baseLength;

			return (float)sampleNum * 100F;
		}

		private Size TextSize(int num, System.Drawing.Font font)
		{
			var sampleText = Enumerable.Repeat("9", num).Aggregate((total, next) => total + next);

			return TextSize(sampleText, font);
		}

		private Size TextSize(string text, System.Drawing.Font font)
		{
			// System.Windows.Media.FormattedText method is not accurate (too small).
			var sampleSize = System.Windows.Forms.TextRenderer.MeasureText(text, font);

			return new Size(sampleSize.Width, sampleSize.Height);
		}

		private Size ChartInnerPlotAreaSize()
		{
			if (_diskChart == null)
				return new Size(0, 0);

			var chartAreaOne = _diskChart.ChartAreas[0];

			return new Size(
				Math.Round((_diskChart.Width * chartAreaOne.InnerPlotPosition.Width) / 100),
				Math.Round((_diskChart.Height * chartAreaOne.InnerPlotPosition.Height) / 100));
		}

		/// <summary>
		/// Draw chart line.
		/// </summary>
		/// <param name="mode">Draw mode of chart</param>
		internal void DrawChart(DrawMode mode)
		{
			var colLine = System.Drawing.Color.FromArgb(
				_colBar[_indexColSelected].R,
				_colBar[_indexColSelected].G,
				_colBar[_indexColSelected].B); // Line color of Series

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
									if (scoreTwo != null)
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
					if (seriesOne != null)
					{
						// In ControlPaint.Dark method, 2nd parameter starts from -0.5 (not darkened yet) to 1 (fully darkened to be black).
						seriesOne.Color = System.Windows.Forms.ControlPaint.Dark(colLine, (float)-0.2);
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
					seriesOne.Color = colLine;

					// Set points of Series.
					switch (mode)
					{
						case DrawMode.Clear:
						case DrawMode.ClearCompletely:
							// At least one point in one Series is required to draw Chart.
							seriesOne.Points.Add(new DataPoint(0, 0));
							break;

						case DrawMode.DrawNewChart:
							if (DiskScores[0].Data != null)
							{
								var dataList = new SortedList<double, double>(DiskScores[0].Data);

								foreach (var data in dataList)
									seriesOne.Points.AddXY(data.Key, data.Value);
							}
							break;
					}

					_diskChart.Series.Add(seriesOne);

					if (DiskScores[0].Data != null)
						AdjustChartAppearance();
					break;
			}
		}

		/// <summary>
		/// Manage color bar for chart line color.
		/// </summary>
		private void ManageColorBar()
		{
			const double sizeNormal = 22D;
			const double sizeSelected = 30D;

			// If initial adjustment, fill color bar with buttons.
			if (this.PanelColorBar.Children.Count == 0)
			{
				for (int i = 0; i < _colBar.Length; i++)
				{
					var colButton = new Button
					{
						Background = new SolidColorBrush(_colBar[i]),
						Tag = i, // Store index number in Tag.
						Template = (ControlTemplate)(App.Current.Resources["ButtonColorTemplate"]),
						Margin = new Thickness(0, 0, 4, 0),
						VerticalAlignment = VerticalAlignment.Bottom,
					};

					colButton.Click += (sender, e) =>
					{
						var button = sender as Button;
						if (button != null)
							_indexColSelected = (int)button.Tag;

						ManageColorBar();
					};

					this.PanelColorBar.Children.Add(colButton);
				}
			}

			// Adjust each button size.
			foreach (var button in this.PanelColorBar.Children.OfType<Button>())
			{
				button.Width = button.Height = ((int)button.Tag == _indexColSelected)
					? sizeSelected
					: sizeNormal;
			}
		}

		#endregion


		#region Command

		#region ApplicationCommands.Open

		/// <summary>
		/// Open Monitor window.
		/// </summary>
		private void OpenExecuted(object target, ExecutedRoutedEventArgs e)
		{
			try
			{
				var monitorWindow = this.OwnedWindows.OfType<MonitorWindow>().FirstOrDefault();
				if (monitorWindow != null)
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
		/// Close this application.
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