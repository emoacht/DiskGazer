using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DiskGazer.Common;
using DiskGazer.Helper;

namespace DiskGazer.Models
{
	public class Operator : NotificationObject
	{
		#region Operation state

		/// <summary>
		/// Whether ready for run
		/// </summary>
		public bool IsReady => !IsReading && !IsAnalyzing;

		/// <summary>
		/// Whether currently reading
		/// </summary>
		public bool IsReading
		{
			get => _isReading;
			private set
			{
				if (SetProperty(ref _isReading, value))
					OnPropertyChanged(nameof(IsReady));
			}
		}
		private bool _isReading;

		/// <summary>
		/// Whether currently analyzing
		/// </summary>
		public bool IsAnalyzing
		{
			get => _isAnalyzing;
			private set
			{
				if (SetProperty(ref _isAnalyzing, value))
					OnPropertyChanged(nameof(IsReady));
			}
		}
		private bool _isAnalyzing;

		/// <summary>
		/// Whether canceled in the course of run
		/// </summary>
		public bool IsCanceled { get; private set; }

		#endregion

		private readonly List<Dictionary<double, double>> _diskScoresRun = new(); // Temporary scores of runs
		private KeyValuePair<double, double>[] _diskScoresStep; // Temporary scores of steps
		private int _diskScoresStepCount; // The number of temporary scores of steps

		private CancellationTokenSource _tokenSource;
		private bool _isTokenSourceDisposed;

		private double _currentSpeed; // Current and latest transfer rate (MB/s)

		#region Read and analyze (Internal)

		internal async Task ReadAnalyzeAsync(IProgress<ProgressInfo> progress)
		{
			if (!IsReady)
				return;

			IsCanceled = false;
			_currentSpeed = 0D;

			_diskScoresRun.Clear();

			try
			{
				_tokenSource = new CancellationTokenSource();
				_isTokenSourceDisposed = false;

				using var rawDataCollection = new BlockingCollection<RawData>();

				// Read and analyze disk in parallel.
				var readTask = ReadAsnyc(rawDataCollection, progress, _tokenSource.Token);
				var analyzeTask = AnalyzeAsync(rawDataCollection, progress, _tokenSource.Token);

				await Task.WhenAll(readTask, analyzeTask);
			}
			catch (OperationCanceledException)
			{
				// None.
			}
			finally
			{
				progress.Report(new ProgressInfo(string.Empty));

				if (_tokenSource is not null)
				{
					_isTokenSourceDisposed = true;
					_tokenSource.Dispose();
				}
			}
		}

		internal void StopReadAnalyze(IProgress<ProgressInfo> progress)
		{
			IsCanceled = true;

			if (_isTokenSourceDisposed || _tokenSource.IsCancellationRequested)
				return;

			try
			{
				_tokenSource.Cancel();

				progress.Report(new ProgressInfo("Stopping"));
			}
			catch (ObjectDisposedException ode)
			{
				Debug.WriteLine($"CancellationTokenSource has been disposed when tried to cancel operation.{Environment.NewLine}{ode}");
			}
		}

		#endregion

		#region Read (Private)

		private async Task ReadAsnyc(BlockingCollection<RawData> rawDataCollection, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
		{
			try
			{
				IsReading = true;

				await ReadBaseAsync(rawDataCollection, progress, cancellationToken);

				progress.Report(new ProgressInfo("Analyzing"));
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to read disk.{Environment.NewLine}{ex}");
				throw;
			}
			finally
			{
				IsReading = false;
			}
		}

		private async Task ReadBaseAsync(BlockingCollection<RawData> rawDataCollection, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
		{
			try
			{
				for (int i = 1; i <= Settings.Current.NumRun; i++)
				{
					for (int j = 0; j < NumStep; j++)
					{
						cancellationToken.ThrowIfCancellationRequested();

						var rawData = new RawData();
						rawData.Run = i;
						rawData.Step = j + 1;
						rawData.Result = ReadResult.NotYet;

						// Update progress before reading.
						progress.Report(ComposeProgressInfo(rawData));

						rawData.BlockOffsetMultiple = j;

						switch (Settings.Current.Method)
						{
							case ReadMethod.Native:
								rawData = await DiskReader.ReadDiskNativeAsync(rawData, cancellationToken);
								break;

							case ReadMethod.P_Invoke:
								rawData = await DiskReader.ReadDiskPInvokeAsync(rawData, cancellationToken);
								break;
						}

						cancellationToken.ThrowIfCancellationRequested();

						// Update progress after reading.
						progress.Report(ComposeProgressInfo(rawData));

						if (rawData.Result == ReadResult.Success) // If success.
						{
							rawDataCollection.Add(rawData, cancellationToken);

							// Leave current speed.
							if (rawData.Data is not null)
							{
								_currentSpeed = rawData.Data.Average();
							}
						}
						else if (rawData.Result == ReadResult.Failure) // If failure.
						{
							throw new Exception(rawData.Message);
						}
					}
				}
			}
			finally
			{
				rawDataCollection.CompleteAdding();
			}
		}

		private ProgressInfo ComposeProgressInfo(RawData rawData)
		{
			// ------
			// Status
			// ------
			string status = null; // Null is to indicate that no inner status exists.

			// Prepare progress figures.
			var progress = string.Format("{0}/{1}", rawData.Run, Settings.Current.NumRun);

			if (1 < NumStep)
				progress += string.Format("- {0}/{1}", rawData.Step, NumStep);

			switch (rawData.Result)
			{
				case ReadResult.NotYet:
					// Prepare remaining time.
					string remaining = null;
					if (0 < _currentSpeed)
					{
						var remainingSteps = NumStep * (Settings.Current.NumRun - rawData.Run + 1) - rawData.Step + 1;
						var remainingBytes = ((double)Settings.Current.AreaSize * 1024D * (double)remainingSteps * (double)Settings.Current.AreaRatioInner / (double)Settings.Current.AreaRatioOuter) * 1024D; // Bytes
						var remainingTime = (remainingBytes / _currentSpeed) / 1000000D; // Second

						remaining = $" Remaining {DateTime.MinValue.AddSeconds(remainingTime):HH:mm:ss}";
					}

					status = $"Reading {progress}{remaining}";
					break;

				case ReadResult.Failure:
					status = $"Failed {progress}";
					break;
			}

			// ------------
			// Inner status
			// ------------
			string innerStatus = null; // Null is to indicate that no inner status exists.

			if (!string.IsNullOrWhiteSpace(rawData.Outcome) || !string.IsNullOrEmpty(rawData.Message))
			{
				innerStatus = string.Format("[{0}/{1} - {2}/{3} Reader ({4}) {5}]",
					rawData.Run, Settings.Current.NumRun,
					rawData.Step, NumStep,
					Settings.Current.Method.ToString().Replace("_", "/"),
					rawData.Result) + Environment.NewLine;

				innerStatus += string.IsNullOrEmpty(rawData.Message)
					? rawData.Outcome
					: rawData.Message;
			}

			return new ProgressInfo(status, innerStatus, true);
		}

		#endregion

		#region Analyze (Private)

		private async Task AnalyzeAsync(BlockingCollection<RawData> rawDataCollection, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
		{
			try
			{
				IsAnalyzing = true;

				await Task.Factory.StartNew(() =>
				{
					try
					{
						// BlockingCollection.Take and BlockingCollection.GetConsumingEnumerable methods
						// seem to block the entire application permanently if running on main thread.
						foreach (var rawData in rawDataCollection.GetConsumingEnumerable(cancellationToken))
						{
							// Update progress before analyzing.
							var innerStatusIn = string.Format("[{0}/{1} - {2}/{3} Analyzer In]",
								rawData.Run, Settings.Current.NumRun,
								rawData.Step, NumStep);

							progress.Report(new ProgressInfo(innerStatusIn, false));

							AnalyzeBase(rawData, progress, cancellationToken);

							// Update progress after analyzing.
							var innerStatusOut = string.Format("[{0}/{1} - {2}/{3} Analyzer Out ({4})]",
								_diskScoresRun.Count, Settings.Current.NumRun,
								_diskScoresStepCount, NumStep, _diskScoresStep.Count());

							progress.Report(new ProgressInfo(innerStatusOut, false));
						}
					}
					catch (OperationCanceledException)
					{
						// A OperationCanceledException from BlockingCollection.GetConsumingEnumerable method
						// cannot be caught outside of this task.
					}
				}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to analyze raw data.{Environment.NewLine}{ex}");
				throw;
			}
			finally
			{
				IsAnalyzing = false;
			}
		}

		private void AnalyzeBase(RawData rawData, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
		{
			var score = CombineLocationData(rawData.BlockOffsetMultiple, rawData.Data).ToArray();
			if (score is not { Length: > 0 })
				return;

			cancellationToken.ThrowIfCancellationRequested();

			// ------------------------
			// Process scores of steps.
			// ------------------------
			Dictionary<double, double> diskScoresStepAveraged;

			try
			{
				if (rawData.BlockOffsetMultiple == 0) // If initial step of run
				{
					// Store initial score.
					_diskScoresStep = score;
					_diskScoresStepCount = 1;

					diskScoresStepAveraged = _diskScoresStep.ToDictionary(pair => pair.Key, pair => pair.Value);
				}
				else
				{
					// Combine scores into one sequential score.
					_diskScoresStep = _diskScoresStep
						.Concat(score)
						.OrderBy(x => x.Key) // Order by locations.
						.ToArray();
					_diskScoresStepCount++;

					// Prepare score of averaged transfer rates (values) of locations (keys) within block size length.
					diskScoresStepAveraged = new Dictionary<double, double>();

					var blockSizeMibiBytes = (double)Settings.Current.BlockSize / 1024D; // Convert KiB to MiB.

					foreach (var data in _diskScoresStep.Select((body, index) => new { body, index }))
					{
						var dataCopy = data;

						var valueList = new List<double>();
						var index = dataCopy.index;
						var keyBottom = dataCopy.body.Key - blockSizeMibiBytes;

						do
						{
							valueList.Add(_diskScoresStep[index].Value);
							index--;

						} while ((0 <= index) && (keyBottom < _diskScoresStep[index].Key));

						if (Settings.Current.RemovesOutlier && (3 <= valueList.Count)) // Removing outliers from data less than 3 makes no sense.
						{
							// Remove outliers using average and standard deviation.
							valueList = RemoveOutlier(valueList, 2).ToList(); // This multiple (2) is to be considered.
						}

						diskScoresStepAveraged.Add(dataCopy.body.Key, valueList.Average());
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to process scores of steps.{Environment.NewLine}{ex}");
				throw;
			}

			cancellationToken.ThrowIfCancellationRequested();

			// -----------------------
			// Process scores of runs.
			// -----------------------
			try
			{
				if (!_diskScoresRun.Any()) // If initial step of initial run.
				{
					// Store initial score.
					_diskScoresRun.Add(diskScoresStepAveraged);

					progress.Report(new ProgressInfo(_diskScoresRun[0]));
				}
				else
				{
					if (rawData.BlockOffsetMultiple != 0) // If not initial step of run.
					{
						// Remove existing score of that run.
						_diskScoresRun.RemoveAt(_diskScoresRun.Count - 1);
					}

					_diskScoresRun.Add(diskScoresStepAveraged);

					// Prepare score of list of transfer rates (values) of the same locations (keys) over all runs.
					var diskScoresRunAll = _diskScoresRun
						.AsParallel()
						.SelectMany(dict => dict)
						.ToLookup(pair => pair.Key, pair => pair.Value);

					// Prepare score of averaged transfer rates (values) over all runs.
					var diskScoresRunAllAveraged = new Dictionary<double, double>();

					foreach (var data in diskScoresRunAll)
					{
						var dataArray = data.ToArray();

						if (Settings.Current.RemovesOutlier && (3 <= dataArray.Length)) // Removing outliers from data less than 3 makes no sense.
						{
							// Remove outliers using average and standard deviation.
							dataArray = RemoveOutlier(dataArray, 2).ToArray(); // This multiple (2) is to be considered.
						}

						diskScoresRunAllAveraged.Add(data.Key, dataArray.Average());
					}

					progress.Report(new ProgressInfo(diskScoresRunAllAveraged));
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to process scores of runs.{Environment.NewLine}{ex}");
				throw;
			}
		}

		private static IEnumerable<KeyValuePair<double, double>> CombineLocationData(int blockOffsetNum, double[] data)
		{
			var areaLocationPlus = Settings.Current.AreaLocation + ((double)Settings.Current.BlockOffset * (double)blockOffsetNum) / 1024D; // MiB
			var blockSizeMibiBytes = Settings.Current.BlockSize / 1024D; // Convert KiB to MiB.

			for (int i = 0; i < data.Length; i++)
			{
				var multiple = i;

				if (Settings.Current.AreaRatioInner < Settings.Current.AreaRatioOuter)
				{
					var remainder = multiple % Settings.Current.AreaRatioInner;
					if (remainder == 0)
						continue; // Remove data immediately after jump.

					multiple = Settings.Current.AreaRatioOuter * (multiple / Settings.Current.AreaRatioInner) + remainder;
				}

				yield return new KeyValuePair<double, double>(areaLocationPlus + (blockSizeMibiBytes * multiple), data[i]);
			}
		}

		private static IEnumerable<double> RemoveOutlier(IEnumerable<double> source, double rangeMultiple)
		{
			var buffer = source as double[] ?? source.ToArray();

			var average = buffer.Average();
			var rangeLength = buffer.StandardDeviation() * rangeMultiple;

			var rangeLowest = average - rangeLength;
			var rangeHighest = average + rangeLength;

			return buffer.Where(x => (rangeLowest <= x) && (x <= rangeHighest));
		}

		#endregion

		#region Helper

		private static int NumStep // The number of steps for block offset. The minimum number will be 1.
		{
			get => (0 < Settings.Current.BlockOffset)
				? Settings.Current.BlockSize / Settings.Current.BlockOffset
				: 1;
		}

		#endregion
	}
}