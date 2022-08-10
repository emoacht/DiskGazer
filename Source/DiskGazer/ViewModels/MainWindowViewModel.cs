using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

using DiskGazer.Common;
using DiskGazer.Helper;
using DiskGazer.Models;
using DiskGazer.Views;

namespace DiskGazer.ViewModels
{
	public class MainWindowViewModel : NotificationObject
	{
		/// <summary>
		/// Roster of disks
		/// </summary>
		private readonly List<DiskInfo> _diskRoster = new List<DiskInfo>();

		/// <summary>
		/// Scores of runs (private)
		/// </summary>
		/// <remarks>Current score is always diskScores[0].</remarks>
		private readonly List<DiskScore> _diskScores = new List<DiskScore> { new DiskScore() };

		/// <summary>
		/// Scores of runs (internal)
		/// </summary>
		/// <remark>Current score is always DiskScores[0].</remark>
		internal IReadOnlyList<DiskScore> DiskScores
		{
			get { return _diskScores; }
		}

		public Operation Op
		{
			get { return _op ?? (_op = new Operation()); }
		}
		private Operation _op;

		private readonly MainWindow _mainWindow;

		public MainWindowViewModel(MainWindow mainWindow)
		{
			this._mainWindow = mainWindow;

			InitializeTask = InitializeAsync();
		}

		private Task InitializeTask { get; set; }

		private async Task InitializeAsync()
		{
			await SearchAsync();
		}

		#region Status

		/// <summary>
		/// Status for Main window
		/// </summary>
		public string Status
		{
			get => _status;
			private set => SetProperty(ref _status, value);
		}
		private string _status;

		/// <summary>
		/// Inner status for Monitor window
		/// </summary>
		public string InnerStatus
		{
			get => _innerStatus;
			private set => SetProperty(ref _innerStatus, value);
		}
		private string _innerStatus;

		#endregion

		#region Scores

		/// <summary>
		/// Maximum value of current score
		/// </summary>
		public double ScoreMax
		{
			get => _scoreMax;
			private set => SetProperty(ref _scoreMax, value);
		}
		private double _scoreMax;

		/// <summary>
		/// Minimum value of current score
		/// </summary>
		public double ScoreMin
		{
			get => _scoreMin;
			private set => SetProperty(ref _scoreMin, value);
		}
		private double _scoreMin;

		/// <summary>
		/// Average value of current score
		/// </summary>
		public double ScoreAvg
		{
			get => _scoreAvg;
			private set => SetProperty(ref _scoreAvg, value);
		}
		private double _scoreAvg;

		#endregion

		#region Settings

		public Settings SettingsCurrent
		{
			get { return Settings.Current; }
		}

		#region Disks

		public ObservableCollection<string> DiskRosterNames
		{
			get => _diskRosterNames ??= new ObservableCollection<string>();
			set => SetProperty(ref _diskRosterNames, value);
		}
		private ObservableCollection<string> _diskRosterNames;

		public int DiskRosterNamesIndex
		{
			get => _diskRosterNamesIndex;
			set
			{
				SetProperty(ref _diskRosterNamesIndex, value);

				// This must run every time.
				if (_diskRoster.Any())
					CurrentDisk = _diskRoster[value];
			}
		}
		private int _diskRosterNamesIndex;

		public DiskInfo CurrentDisk
		{
			get => _currentDisk;
			set
			{
				if (SetProperty(ref _currentDisk, value))
				{
					Settings.Current.PhysicalDrive = value.PhysicalDrive;

					AreaSize = AreaFineness; // Set to minimum value.
					AreaLocation = 0; // Reset.
				}
			}
		}
		private DiskInfo _currentDisk;

		#endregion

		#region Area size and location

		public int AreaSize
		{
			get { return Settings.Current.AreaSize; }
			set { SetAreaSizeLocation(size: value, location: AreaLocation, selection: AreaSelection.Size); }
		}

		public int AreaLocation
		{
			get { return Settings.Current.AreaLocation; }
			set { SetAreaSizeLocation(size: AreaSize, location: value, selection: AreaSelection.Location); }
		}

		private enum AreaSelection
		{
			Size,
			Location,
		}

		private void SetAreaSizeLocation(int size, int location, AreaSelection selection)
		{
			var sizeCopy = size;
			var locationCopy = location;
			var remainder = CurrentDisk.Size % AreaFineness;

			if (sizeCopy + locationCopy > CurrentDisk.Size)
			{
				switch (selection)
				{
					case AreaSelection.Size:
						locationCopy = CurrentDisk.Size - remainder - sizeCopy;
						if (locationCopy < 0)
							locationCopy = 0; // Default value;

						sizeCopy = CurrentDisk.Size - remainder - locationCopy;
						break;
					case AreaSelection.Location:
						sizeCopy = CurrentDisk.Size - remainder - locationCopy;
						if (sizeCopy < AreaFineness)
							sizeCopy = AreaFineness; // Minimum value;

						locationCopy = CurrentDisk.Size - remainder - sizeCopy;
						break;
				}
			}

			if (Settings.Current.AreaSize != sizeCopy)
			{
				Settings.Current.AreaSize = sizeCopy;
				OnPropertyChanged(nameof(AreaSize));
			}

			if (Settings.Current.AreaLocation != locationCopy)
			{
				Settings.Current.AreaLocation = locationCopy;
				OnPropertyChanged(nameof(AreaLocation));
			}
		}

		#endregion

		#region Area fineness

		public IReadOnlyCollection<double> MenuAreaFineness
		{
			get { return new[] { 1, 0.5, 0.25 }; } // GiB
		}

		public int AreaFineness
		{
			get => _areaFineness;
			set => SetProperty(ref _areaFineness, value);
		}
		private int _areaFineness = 1024; // MiB

		#endregion

		#region Area Ratio

		private readonly List<int> _menuAreaRatioDivider = new List<int> { 1, 2, 4, 8, 16 };

		public string[] MenuAreaRatio
		{
			get
			{
				return _menuAreaRatioDivider
					.Select(x => String.Format("1/{0}", x))
					.ToArray();
			}
		}

		public int MenuAreaRatioIndex
		{
			get
			{
				var index = _menuAreaRatioDivider.IndexOf(Settings.Current.AreaRatioOuter / Settings.Current.AreaRatioInner);
				return (0 <= index) ? index : 0;
			}
			set
			{
				var buffer = Settings.Current.AreaRatioInner * _menuAreaRatioDivider[value];
				if (Settings.Current.AreaRatioOuter != buffer)
				{
					Settings.Current.AreaRatioOuter = buffer;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region The number of runs

		public IReadOnlyCollection<int> MenuNumRun
		{
			get { return new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }; }
		}

		#endregion

		#region Block size

		public IReadOnlyCollection<int> MenuBlockSize
		{
			get { return _menuBlockSize ?? (_menuBlockSize = new[] { 1024, 512, 256, 128, 64, 32 }); } //MiB
		}
		private int[] _menuBlockSize;

		public int MenuBlockSizeItem
		{
			get => Settings.Current.BlockSize;
			set
			{
				if (Settings.Current.BlockSize != value)
				{
					Settings.Current.BlockSize = value;
					OnPropertyChanged();

					MenuBlockOffsetIndex = 0; // Reset.
					MenuBlockOffset = null; // Reset.
				}
			}
		}

		#endregion

		#region Block offset

		private readonly List<int> _menuBlockOffsetDivider = new List<int> { 1, 2, 4, 8, 16, 32 };

		private string[] GetMenuBlockOffset()
		{
			return _menuBlockOffsetDivider
				.Where(x => MenuBlockSize.Min() <= Settings.Current.BlockSize / x)
				.Select(x => String.Format("1/{0}", x))
				.ToArray();
		}

		public string[] MenuBlockOffset
		{
			get => _menuBlockOffset ??= GetMenuBlockOffset();
			set => SetProperty(ref _menuBlockOffset, value);
		}
		private string[] _menuBlockOffset;

		public int MenuBlockOffsetIndex
		{
			get
			{
				return (0 < Settings.Current.BlockOffset)
					? _menuBlockOffsetDivider.FindIndex(x => x == Settings.Current.BlockSize / Settings.Current.BlockOffset)
					: 0;
			}
			set
			{
				Settings.Current.BlockOffset = (0 < value)
					? Settings.Current.BlockSize / _menuBlockOffsetDivider[value]
					: 0;

				OnPropertyChanged();
			}
		}

		#endregion

		#endregion

		#region Command

		#region Run

		public DelegateCommand RunCommand
		{
			get { return _runCommand ?? (_runCommand = new DelegateCommand(RunExecute, CanRunExecute)); }
		}
		private DelegateCommand _runCommand;

		private async void RunExecute()
		{
			if (Op.IsReady)
			{
				await RunAsync();
			}
			else
			{
				StopRun();
			}
		}

		private bool CanRunExecute()
		{
			return true;
		}

		#endregion

		#region Rescan

		public DelegateCommand RescanCommand
		{
			get { return _rescanCommand ?? (_rescanCommand = new DelegateCommand(RescanExecute, CanRescanExecute)); }
		}
		private DelegateCommand _rescanCommand;

		private async void RescanExecute()
		{
			await RescanAsync();
		}

		private bool CanRescanExecute()
		{
			return true;
		}

		#endregion

		#region Save screenshot to file

		public DelegateCommand SaveScreenshotFileCommand
		{
			get { return _saveScreenshotFileCommand ?? (_saveScreenshotFileCommand = new DelegateCommand(SaveScreenshotFileExecute, CanSaveScreenshotFileExecute)); }
		}
		private DelegateCommand _saveScreenshotFileCommand;

		private void SaveScreenshotFileExecute()
		{
			SaveScreenshotFile();
		}

		private bool CanSaveScreenshotFileExecute()
		{
			return true;
		}

		#endregion

		#region Save log to file

		public DelegateCommand SaveLogFileCommand
		{
			get { return _saveLogFileCommand ?? (_saveLogFileCommand = new DelegateCommand(SaveLogFileExecute, CanSaveLogFileExecute)); }
		}
		private DelegateCommand _saveLogFileCommand;

		private async void SaveLogFileExecute()
		{
			await SaveLogFile();
		}

		private bool CanSaveLogFileExecute()
		{
			return (_diskScores[0].Data != null);
		}

		#endregion

		#region Send log to clipboard

		public DelegateCommand SendLogClipboardCommand
		{
			get { return _sendLogClipboardCommand ?? (_sendLogClipboardCommand = new DelegateCommand(SendLogClipboardExecute, CanSendLogClipboardExecute)); }
		}
		private DelegateCommand _sendLogClipboardCommand;

		private async void SendLogClipboardExecute()
		{
			await SendLogClipboard();
		}

		private bool CanSendLogClipboardExecute()
		{
			return (_diskScores[0].Data != null);
		}

		#endregion

		#region Pin current chart line

		public DelegateCommand PinLineCommand
		{
			get { return _pinLineCommand ?? (_pinLineCommand = new DelegateCommand(PinLineExecute, CanPinLineExecute)); }
		}
		private DelegateCommand _pinLineCommand;

		private void PinLineExecute()
		{
			_diskScores[0].IsPinned = true;
		}

		private bool CanPinLineExecute()
		{
			return (_diskScores[0].Data != null);
		}

		#endregion

		#region Clear all chart lines

		public DelegateCommand ClearLinesCommand
		{
			get { return _clearLinesCommand ?? (_clearLinesCommand = new DelegateCommand(ClearLinesExecute, CanClearLinesExecute)); }
		}
		private DelegateCommand _clearLinesCommand;

		private void ClearLinesExecute()
		{
			_diskScores.Clear();
			_diskScores.Add(new DiskScore()); // Make diskScores[0] always exist.
			UpdateScores();
		}

		private bool CanClearLinesExecute()
		{
			return true;
		}

		#endregion

		#endregion

		#region Search

		/// <summary>
		/// Search disks.
		/// </summary>
		private async Task SearchAsync()
		{
			try
			{
				List<DiskInfo> diskRosterPre = null;

				// Get disk information by WMI.
				var searchTask = Task.Run(() => DiskSearcher.Search());

				try
				{
					diskRosterPre = await searchTask;
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Failed to get disk information by WMI. {0}", ex);

					if (searchTask.Exception != null)
					{
						searchTask.Exception.Flatten().InnerExceptions
							.ToList()
							.ForEach(x => Debug.WriteLine(x));
					}
				}

				if (diskRosterPre == null)
					return;

				// Sort by PhysicalDrive.
				diskRosterPre.Sort();

				foreach (var infoPre in diskRosterPre)
				{
					DiskInfo infoNew = null;

					// Add disk information by P/Invoke.
					var physicalDrive = infoPre.PhysicalDrive;

					var checkTask = Task.Run(() => DiskChecker.GetDiskInfo(physicalDrive));

					try
					{
						infoNew = await checkTask;
					}
					catch (Exception ex)
					{
						Debug.WriteLine("Failed to get disk information by P/Invoke. {0}", ex);

						if (checkTask.Exception != null)
						{
							checkTask.Exception.Flatten().InnerExceptions
								.ToList()
								.ForEach(x => Debug.WriteLine(x));
						}
					}

					if (infoNew == null)
					{
						infoNew = new DiskInfo { PhysicalDrive = infoPre.PhysicalDrive };
					}

					infoNew.Model = infoPre.Model;
					infoNew.InterfaceType = infoPre.InterfaceType;
					infoNew.MediaTypeDiskDrive = infoPre.MediaTypeDiskDrive;
					infoNew.MediaTypePhysicalDisk = infoPre.MediaTypePhysicalDisk;
					infoNew.SpindleSpeed = infoPre.SpindleSpeed;
					infoNew.SizeWMI = infoPre.SizeWMI;

					// Add disk information to disk roster.
					int index = 0;

					if (_diskRoster.Any())
					{
						while ((index < _diskRoster.Count) && (infoNew.PhysicalDrive > _diskRoster[index].PhysicalDrive))
						{
							index++;
						}
					}

					_diskRoster.Insert(index, infoNew);
					DiskRosterNames.Insert(index, infoNew.NameBus);

					if (index == 0)
					{
						DiskRosterNamesIndex = 0;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to search disks {0}", ex);
			}
		}

		/// <summary>
		/// Rescan disks.
		/// </summary>
		private async Task RescanAsync()
		{
			_diskRoster.Clear();
			DiskRosterNames.Clear();

			await SearchAsync();
		}

		#endregion

		#region Run

		/// <summary>
		/// Start reading and analyzing disk.
		/// </summary>
		private async Task RunAsync()
		{
			if (!Account.IsAdmin)
			{
				MessageBox.Show("This application needs to be run by administrator.",
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			if ((Settings.Current.Method == ReadMethod.Native) && !DiskReader.NativeExeExists)
			{
				MessageBox.Show("Win32 console application for native method is not found.",
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			// Prepare to store settings and data.
			if (!_diskScores[0].IsPinned)
			{
				_diskScores[0] = new DiskScore();
			}
			else // If preceding score is pinned.
			{
				_diskScores.Insert(0, new DiskScore());
			}

			_diskScores[0].Disk = CurrentDisk.Clone();
			_diskScores[0].StartTime = DateTime.Now;

			_diskScores[0].BlockSize = Settings.Current.BlockSize;
			_diskScores[0].BlockOffset = Settings.Current.BlockOffset;
			_diskScores[0].AreaSize = Settings.Current.AreaSize;
			_diskScores[0].AreaLocation = Settings.Current.AreaLocation;
			_diskScores[0].AreaRatioInner = Settings.Current.AreaRatioInner;
			_diskScores[0].AreaRatioOuter = Settings.Current.AreaRatioOuter;

			_diskScores[0].NumRun = Settings.Current.NumRun;
			_diskScores[0].Method = Settings.Current.Method;

			// Reset scores and chart.
			UpdateScores();
			_mainWindow.DrawChart(DrawMode.Clear);

			try
			{
				// Start.
				await Op.ReadAnalyzeAsync(new Progress<ProgressInfo>(UpdateProgress));
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to read disk. {0}", ex);
			}

			// Save screenshot and log.
			if (Settings.Current.SavesScreenshotLog && !Op.IsCanceled)
			{
				// Wait for rendering of scores and chart.
				// (Synchronously start empty action of lower priority than rendering.)
				_mainWindow.Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);

				WindowSupplement.ActivateWindow(_mainWindow);

				// Wait for this window to be activated (provided up to 10 times).
				for (int i = 0; i <= 9; i++)
				{
					if (WindowSupplement.IsWindowActivated(_mainWindow))
						break;

					await Task.Delay(TimeSpan.FromMilliseconds(100));
				}

				await SaveScreenshotLog();
			}
		}

		/// <summary>
		/// Stop reading and analyzing disk.
		/// </summary>
		private void StopRun()
		{
			Op.StopReadAnalyze(new Progress<ProgressInfo>(UpdateProgress));
		}

		private void UpdateProgress(ProgressInfo info)
		{
			// Update scores and chart.
			if ((info.Data != null) && !Op.IsCanceled)
			{
				_diskScores[0].Data = info.Data;

				UpdateScores();
				_mainWindow.DrawChart(DrawMode.DrawNewChart);
			}

			// Update status.
			if (info.Status != null)
			{
				Status = info.Status;
			}

			// Update inner status.
			if (info.InnerStatus != null)
			{
				InnerStatus = info.IsInnerStatusRenewed
					? info.InnerStatus
					: InnerStatus.Insert(0, info.InnerStatus + Environment.NewLine);
			}
		}

		private void UpdateScores()
		{
			if (_diskScores[0].Data == null)
			{
				ScoreMax = 0;
				ScoreMin = 0;
				ScoreAvg = 0;
			}
			else
			{
				ScoreMax = _diskScores[0].Data.Values.Max();
				ScoreMin = _diskScores[0].Data.Values.Min();
				ScoreAvg = _diskScores[0].Data.Values.Average();
			}
		}

		#endregion

		#region Screenshot and log

		private const string _resultFolderName = "result"; // Folder name to save screenshots and logs

		/// <summary>
		/// Save screenshot and log.
		/// </summary>
		private async Task SaveScreenshotLog()
		{
			var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _resultFolderName);
			var filePath = Path.Combine(folderPath, _diskScores[0].StartTime.ToString("yyyyMMddHHmmss")); // File path except extension

			try
			{
				if (!Directory.Exists(folderPath))
					Directory.CreateDirectory(folderPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("Failed to save screenshot and log. {0}", ex),
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Save screenshot to file.
			System.Drawing.Bitmap screenshot = GetScreenshot();
			if (screenshot != null)
				SaveScreenshotFileBase(screenshot, String.Format("{0}.png", filePath));

			// Save log to file.
			await SaveLogFileBase(String.Format("{0}.txt", filePath));
		}

		/// <summary>
		/// Save screenshot to file.
		/// </summary>
		private void SaveScreenshotFile()
		{
			System.Drawing.Bitmap screenshot = GetScreenshot();
			if (screenshot == null)
				return;

			var sfd = new SaveFileDialog
			{
				FileName = "screenshot.png",
				Filter = "(*.png)|*.png|(*.*)|*.*",
			};

			if (sfd.ShowDialog() == true)
				SaveScreenshotFileBase(screenshot, sfd.FileName);
		}

		private void SaveScreenshotFileBase(System.Drawing.Bitmap screenshot, string filePath)
		{
			try
			{
				screenshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to save screenshot to file. " + ex.Message,
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Save log to file.
		/// </summary>
		private async Task SaveLogFile()
		{
			var sfd = new SaveFileDialog
			{
				FileName = "log.txt",
				Filter = "(*.txt)|*.txt|(*.*)|*.*",
			};

			if (sfd.ShowDialog() == true)
				await SaveLogFileBase(sfd.FileName);
		}

		private async Task SaveLogFileBase(string filePath)
		{
			try
			{
				await Task.Run(() => ComposeLog())
					.ContinueWith(async composition =>
					{
						using (var sw = new StreamWriter(filePath, false, Encoding.UTF8))
						{
							await sw.WriteAsync(composition.Result);
						}
					});
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to save log to file. " + ex.Message,
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// Send log to clipboard.
		/// </summary>
		private async Task SendLogClipboard()
		{
			var tcs = new TaskCompletionSource<bool>(); // No meaning for Boolean.

			try
			{
				var thread = new Thread(() =>
				{
					try
					{
						Clipboard.SetText(ComposeLog());
						tcs.SetResult(true);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				});
				thread.SetApartmentState(ApartmentState.STA); // Clipboard class requires STA thread.
				thread.Start();

				await tcs.Task;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to send log to clipboard. " + ex.Message,
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private System.Drawing.Bitmap GetScreenshot()
		{
			try
			{
				var rct = WindowSupplement.GetWindowRect(_mainWindow);

				var screenshot = new System.Drawing.Bitmap((int)rct.Width, (int)rct.Height);
				using (var g = System.Drawing.Graphics.FromImage(screenshot))
				{
					g.CopyFromScreen(new System.Drawing.Point((int)rct.Left, (int)rct.Top), System.Drawing.Point.Empty, screenshot.Size);
				}
				return screenshot;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to save screenshot. " + ex.Message,
								ProductInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}
		}

		private string ComposeLog()
		{
			if (_diskScores[0].Data == null)
				return String.Empty;

			// Compose header.
			var headerElements = new[]
			{
				String.Format("[{0}]", ProductInfo.NameVersionMiddle),
				String.Format("Disk model     : {0}", _diskScores[0].Disk.Name),
				String.Format("Bus type       : {0}", _diskScores[0].Disk.BusType),
				String.Format("Disk capacity  : {0:f2} GiB", (double)_diskScores[0].Disk.Size / 1024D), // Convert MiB to GiB.
				String.Format("Block size     : {0} KiB", _diskScores[0].BlockSize),
				String.Format("Block offset   : {0} KiB", _diskScores[0].BlockOffset),
				String.Format("Area size      : {0} GiB", (double)_diskScores[0].AreaSize / 1024D), // Convert MiB to GiB.
				String.Format("Area location  : {0} GiB", (double)_diskScores[0].AreaLocation / 1024D), // Convert MiB to GiB.
				String.Format("Area ratio     : {0} / {1}", _diskScores[0].AreaRatioInner, _diskScores[0].AreaRatioOuter),
				String.Format("Number of runs : {0}", _diskScores[0].NumRun),
				String.Format("Method to run  : {0}", _diskScores[0].Method.ToString().Replace("_", "/")),
				String.Format("Start time     : {0:yyyy/MM/dd HH:mm:ss}", _diskScores[0].StartTime),
				String.Format("Maximum read   : {0:f2} MB/s", ScoreMax),
				String.Format("Minimum read   : {0:f2} MB/s", ScoreMin),
				String.Format("Average read   : {0:f2} MB/s", ScoreAvg),
				"(MB/s = 1,000,000 Bytes / second)",
			};

			var header = String.Join(Environment.NewLine, headerElements);

			// Compose body.
			const string bodyElementTop = "Location (MiB), Sequential read (MB/s)";

			var body = new SortedList<double, double>(_diskScores[0].Data)
				.Select(x => String.Format("{0}, {1:f6}", x.Key, x.Value))
				.Aggregate(bodyElementTop, (total, next) => total + Environment.NewLine + next);

			return header + Environment.NewLine +
				Environment.NewLine +
				body;
		}

		#endregion
	}
}