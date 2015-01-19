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
	public class Operation : NotificationObject
	{
		#region Operation state

		/// <summary>
		/// Whether ready for run
		/// </summary>
		public bool IsReady
		{
			get { return (!IsReading && !IsAnalyzing); }
		}

		/// <summary>
		/// Whether currently reading
		/// </summary>
		public bool IsReading
		{
			get { return _isReading; }
			private set
			{
				_isReading = value;
				RaisePropertyChanged(() => IsReady);
			}
		}
		private bool _isReading;

		/// <summary>
		/// Whether currently analyzing
		/// </summary>
		public bool IsAnalyzing
		{
			get { return _isAnalyzing; }
			private set
			{
				_isAnalyzing = value;
				RaisePropertyChanged(() => IsReady);
			}
		}
		private bool _isAnalyzing;

		/// <summary>
		/// Whether canceled in the course of run
		/// </summary>
		public bool IsCanceled { get; private set; }

		#endregion


		private ConcurrentQueue<RawData> rawDataQueue; // Queue of raw data

		private readonly List<Dictionary<double, double>> diskScoresRun = new List<Dictionary<double, double>>(); // Temporary scores of runs
		private KeyValuePair<double, double>[] diskScoresStep; // Temporary scores of steps
		private int diskScoresStepCount = 0; // The number of temporary scores of steps

		private CancellationTokenSource tokenSource;
		private bool isTokenSourceDisposed;

		private double currentSpeed; // Current and latest transfer rate (MB/s)


		#region Read and analyze (Internal)

		internal async Task ReadAnalyzeAsync(IProgress<ProgressInfo> progress)
		{
			if (!IsReady)
				return;

			IsCanceled = false;
			currentSpeed = 0D;

			rawDataQueue = new ConcurrentQueue<RawData>();

			diskScoresRun.Clear();

			try
			{
				tokenSource = new CancellationTokenSource();
				isTokenSourceDisposed = false;

				// Read and analyze disk in parallel.
				var readTask = ReadAsnyc(progress, tokenSource.Token);
				var analyzeTask = AnalyzeAsync(progress, tokenSource.Token);

				await Task.WhenAll(readTask, analyzeTask);
			}
			catch (OperationCanceledException)
			{
				// None.
			}
			finally
			{
				progress.Report(new ProgressInfo(String.Empty));

				if (tokenSource != null)
				{
					isTokenSourceDisposed = true;
					tokenSource.Dispose();
				}
			}
		}

		internal void StopReadAnalyze(IProgress<ProgressInfo> progress)
		{
			IsCanceled = true;

			if (isTokenSourceDisposed || (tokenSource.IsCancellationRequested))
				return;

			try
			{
				tokenSource.Cancel();

				progress.Report(new ProgressInfo("Stopping"));
			}
			catch (ObjectDisposedException ode)
			{
				Debug.WriteLine("CancellationTokenSource has been disposed when tried to cancel operation. {0}", ode);
			}
		}

		#endregion


		#region Read (Private)

		private async Task ReadAsnyc(IProgress<ProgressInfo> progress, CancellationToken token)
		{
			try
			{
				IsReading = true;

				await ReadBaseAsync(progress, token);

				progress.Report(new ProgressInfo("Analyzing"));
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to read disk. {0}", ex);
				throw;
			}
			finally
			{
				IsReading = false;
			}
		}

		private async Task ReadBaseAsync(IProgress<ProgressInfo> progress, CancellationToken token)
		{
			for (int i = 1; i <= Settings.Current.NumRun; i++)
			{
				for (int j = 0; j < NumStep; j++)
				{
					token.ThrowIfCancellationRequested();

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
							rawData = await DiskReader.ReadDiskNativeAsync(rawData, token);
							break;

						case ReadMethod.P_Invoke:
							rawData = await DiskReader.ReadDiskPInvokeAsync(rawData, token);
							break;
					}

					token.ThrowIfCancellationRequested();

					// Update progress after reading.
					progress.Report(ComposeProgressInfo(rawData));

					if (rawData.Result == ReadResult.Success) // If success.
					{
						// Store raw data into queue.
						rawDataQueue.Enqueue(rawData);

						// Leave current speed.
						if (rawData.Data != null)
						{
							currentSpeed = rawData.Data.Average();
						}
					}
					else if (rawData.Result == ReadResult.Failure) // If failure.
					{
						throw new Exception(rawData.Message);
					}
				}
			}
		}

		private ProgressInfo ComposeProgressInfo(RawData rawData)
		{
			// ------
			// Status
			// ------
			string status = null; // Null is to indicate that no inner status exists.

			// Prepare progress figures.
			var strProgress = String.Format(" {0}/{1}", rawData.Run, Settings.Current.NumRun);

			if (1 < NumStep)
				strProgress += String.Format(" - {0}/{1}", rawData.Step, NumStep);

			switch (rawData.Result)
			{
				case ReadResult.NotYet:
					// Prepare remaining time.
					string strTime = "";
					if (0 < currentSpeed)
					{
						var remainingSteps = NumStep * (Settings.Current.NumRun - rawData.Run + 1) - rawData.Step + 1;
						var remainingBytes = ((double)Settings.Current.AreaSize * 1024D * (double)remainingSteps * (double)Settings.Current.AreaRatioInner / (double)Settings.Current.AreaRatioOuter) * 1024D; // Bytes
						var remainingTime = (remainingBytes / currentSpeed) / 1000000D; // Second

						strTime = String.Format(" Remaining {0:HH:mm:ss}", DateTime.MinValue.AddSeconds(remainingTime));
					}

					status = String.Format("Reading{0}{1}", strProgress, strTime);
					break;

				case ReadResult.Failure:
					status = String.Format("Failed{0}", strProgress);
					break;
			}

			// ------------
			// Inner status
			// ------------
			string innerStatus = null; // Null is to indicate that no inner status exists.

			if (!String.IsNullOrWhiteSpace(rawData.Outcome) || !String.IsNullOrEmpty(rawData.Message))
			{
				innerStatus = String.Format("[{0}/{1} - {2}/{3} Reader ({4}) {5}]",
					rawData.Run, Settings.Current.NumRun,
					rawData.Step, NumStep,
					Settings.Current.Method.ToString().Replace("_", "/"),
					rawData.Result) + Environment.NewLine;

				innerStatus += String.IsNullOrEmpty(rawData.Message)
					? rawData.Outcome
					: rawData.Message;
			}

			return new ProgressInfo(status, innerStatus, true);
		}

		#endregion


		#region Analyze (Private)

		private readonly TimeSpan waitTime = TimeSpan.FromMilliseconds(100); // Wait time when queue has no data

		private async Task AnalyzeAsync(IProgress<ProgressInfo> progress, CancellationToken token)
		{
			try
			{
				IsAnalyzing = true;

				while (IsReading || !rawDataQueue.IsEmpty)
				{
					token.ThrowIfCancellationRequested();

					if (rawDataQueue.IsEmpty)
					{
						// Wait for new data.
						await Task.Delay(waitTime, token);
						continue;
					}

					// Dequeue raw data from queue.
					RawData rawData;
					if (!rawDataQueue.TryDequeue(out rawData))
						throw new Exception("Failed to dequeue raw data from queue.");

					// Update progress before analyzing.
					var innerStatusIn = String.Format("[{0}/{1} - {2}/{3} Analyzer In]",
						rawData.Run, Settings.Current.NumRun,
						rawData.Step, NumStep);

					progress.Report(new ProgressInfo(innerStatusIn, false));

					await Task.Run(() => AnalyzeBase(rawData, progress, token), token);

					// Update progress after analyzing.					
					var innerStatusOut = String.Format("[{0}/{1} - {2}/{3} Analyzer Out ({4})]",
						diskScoresRun.Count, Settings.Current.NumRun,
						diskScoresStepCount, NumStep, diskScoresStep.Count());

					progress.Report(new ProgressInfo(innerStatusOut, false));
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to analyze raw data. {0}", ex);
				throw;
			}
			finally
			{
				IsAnalyzing = false;
			}
		}

		private void AnalyzeBase(RawData rawData, IProgress<ProgressInfo> progress, CancellationToken token)
		{
			var score = CombineLocationData(rawData.BlockOffsetMultiple, rawData.Data).ToArray();

			if (!score.Any())
				return;

			token.ThrowIfCancellationRequested();

			// ------------------------
			// Process scores of steps.
			// ------------------------
			Dictionary<double, double> diskScoresStepAveraged;

			try
			{
				if (rawData.BlockOffsetMultiple == 0) // If initial step of run
				{
					// Store initial score.
					diskScoresStep = score;
					diskScoresStepCount = 1;

					diskScoresStepAveraged = diskScoresStep.ToDictionary(pair => pair.Key, pair => pair.Value);
				}
				else
				{
					// Combine scores into one sequential score.
					diskScoresStep = diskScoresStep
						.Concat(score)
						.OrderBy(x => x.Key) // Order by locations.
						.ToArray();
					diskScoresStepCount++;

					// Prepare score of averaged transfer rates (values) of locations (keys) within block size length.
					diskScoresStepAveraged = new Dictionary<double, double>();

					var blockSizeMibiBytes = (double)Settings.Current.BlockSize / 1024D; // Convert KiB to MiB.

					foreach (var data in diskScoresStep.Select((body, index) => new { body, index }))
					{
						var dataCopy = data;

						var valueList = new List<double>();
						var index = dataCopy.index;
						var keyBottom = dataCopy.body.Key - blockSizeMibiBytes;

						do
						{
							valueList.Add(diskScoresStep[index].Value);
							index--;

						} while ((0 <= index) && (keyBottom < diskScoresStep[index].Key));

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
				Debug.WriteLine("Failed to process scores of steps. {0}", ex);
				throw;
			}

			token.ThrowIfCancellationRequested();

			// -----------------------
			// Process scores of runs.
			// -----------------------
			try
			{
				if (!diskScoresRun.Any()) // If initial step of initial run.
				{
					// Store initial score.
					diskScoresRun.Add(diskScoresStepAveraged);

					progress.Report(new ProgressInfo(diskScoresRun[0]));
				}
				else
				{
					if (rawData.BlockOffsetMultiple != 0) // If not initial step of run.
					{
						// Remove existing score of that run.
						diskScoresRun.RemoveAt(diskScoresRun.Count - 1);
					}

					diskScoresRun.Add(diskScoresStepAveraged);

					// Prepare score of list of transfer rates (values) of the same locations (keys) over all runs.
					var diskScoresRunAll = diskScoresRun
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
				Debug.WriteLine("Failed to process scores of runs. {0}", ex);
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
			var buff = source as double[] ?? source.ToArray();

			var average = buff.Average();
			var rangeLength = buff.StandardDeviation() * rangeMultiple;

			var rangeLowest = average - rangeLength;
			var rangeHighest = average + rangeLength;

			return buff.Where(x => (rangeLowest <= x) && (x <= rangeHighest));
		}

		#endregion


		#region Helper

		private static int NumStep // The number of steps for block offset. The minimum number will be 1.
		{
			get
			{
				return (0 < Settings.Current.BlockOffset)
					? Settings.Current.BlockSize / Settings.Current.BlockOffset
					: 1;
			}
		}

		#endregion
	}
}