using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Models
{
	internal static class DiskReader
	{
		#region Native

		private const string nativeExeFile = "Gazer.exe"; // Executable file of Win32 console application
		private static readonly string nativeExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nativeExeFile);

		/// <summary>
		/// Check if executable file exists
		/// </summary>
		/// <returns>True if exists</returns>
		internal static bool ExistsNativeExe()
		{
			return File.Exists(nativeExePath);
		}

		/// <summary>
		/// Run by native.
		/// </summary>
		/// <param name="rawData">Raw data</param>
		internal static void ReadDiskNative(ref RawData rawData)
		{
			if (!ExistsNativeExe())
			{
				rawData.Result = ReadResult.Failure;
				rawData.Message = String.Format("Cannot find {0}.", nativeExeFile);
				return;
			}

			var blockOffsetMultiple = rawData.BlockOffsetMultiple;

			try
			{
				var arguments = String.Format("{0} {1} {2} {3} {4}",
					Settings.Current.PhysicalDrive,
					Settings.Current.BlockSize,
					Settings.Current.BlockOffset * blockOffsetMultiple,
					Settings.Current.AreaSize,
					Settings.Current.AreaLocation);

				if (Settings.Current.AreaRatioInner < Settings.Current.AreaRatioOuter)
				{
					arguments += String.Format(" {0} {1}",
						Settings.Current.AreaRatioInner,
						Settings.Current.AreaRatioOuter);
				}

				using (var proc = new Process()
				{
					StartInfo = new ProcessStartInfo()
					{
						FileName = nativeExePath,
						Verb = "RunAs", // Run as administrator.
						Arguments = arguments,
						UseShellExecute = false,
						CreateNoWindow = true,
						//WindowStyle = ProcessWindowStyle.Hidden,
						RedirectStandardOutput = true,
					},
				})
				{
					proc.Start();
					var outcome = proc.StandardOutput.ReadToEnd();
					proc.WaitForExit();

					rawData.Result = ((proc.HasExited) & (proc.ExitCode == 0))
						? ReadResult.Success
						: ReadResult.Failure;

					rawData.Outcome = outcome;

					switch (rawData.Result)
					{
						case ReadResult.Success:
							rawData.Data = FindData(outcome);
							break;
						case ReadResult.Failure:
							rawData.Message = FindMessage(outcome);
							break;
					}
				}
			}
			catch (Exception ex)
			{
				rawData.Result = ReadResult.Failure;
				rawData.Message = String.Format("Failed to execute {0}. {1}", nativeExeFile, ex.Message);
			}
		}

		private static double[] FindData(string outcome)
		{
			const string startSign = "[Start data]";
			const string endSign = "[End data]";

			var startPoint = outcome.IndexOf(startSign, StringComparison.InvariantCulture);
			var endPoint = outcome.LastIndexOf(endSign, StringComparison.InvariantCulture);

			if ((startPoint < 0) || (endPoint < 0) || (startPoint >= endPoint))
				return null;

			var buff = outcome
				.Substring(startPoint + startSign.Length, ((endPoint - 1) - (startPoint + startSign.Length))).Trim()
				.Split(new string[] { " ", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

			return buff
				.Select(x => double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture)) // Culture matters.
				.ToArray();
		}

		private static string FindMessage(string outcome)
		{
			var strLine = outcome.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

			return (strLine.Length >= 1)
				? strLine[strLine.Length - 1] // Last line should contain error message.
				: String.Empty;
		}

		#endregion


		#region P/Invoke

		/// <summary>
		/// Run by P/Invoke.
		/// </summary>
		/// <param name="rawData">Raw data</param>
		internal static void ReadDiskPInvoke(ref RawData rawData)
		{
			var blockOffsetMultiple = rawData.BlockOffsetMultiple;

			SafeFileHandle hFile = null;

			try
			{
				// ----------
				// Read disk.
				// ----------
				// This section is based on sequential read test of CrystalDiskMark (3.0.2)
				// created by hiyohiyo (http://crystalmark.info/).

				// Get handle to disk.
				hFile = W32.CreateFile(
					String.Format("\\\\.\\PhysicalDrive{0}", Settings.Current.PhysicalDrive),
					W32.GENERIC_READ, // Administrative privilege is required.
					0,
					IntPtr.Zero,
					W32.OPEN_EXISTING,
					W32.FILE_ATTRIBUTE_NORMAL | W32.FILE_FLAG_NO_BUFFERING | W32.FILE_FLAG_SEQUENTIAL_SCAN,
					IntPtr.Zero);

				if (hFile == null || hFile.IsInvalid)
					// This is normal when this application is not run by administrator.
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get handle to disk.");

				// Prepare parameters.
				var areaSizeActual = Settings.Current.AreaSize; // Area size for actual reading (MiB)
				if (0 < Settings.Current.BlockOffset)
				{
					areaSizeActual -= 1; // 1 is for the last MiB of area. If offset, it may exceed disk size.
				}

				int readNum = (areaSizeActual * 1024) / Settings.Current.BlockSize; // Number of reads

				int loopOuter = 1; // Number of outer loops
				int loopInner = readNum; // Number of inner loops

				if (Settings.Current.AreaRatioInner < Settings.Current.AreaRatioOuter)
				{
					loopOuter = (areaSizeActual * 1024) / (Settings.Current.BlockSize * Settings.Current.AreaRatioOuter);
					loopInner = Settings.Current.AreaRatioInner;

					readNum = loopInner * loopOuter;
				}

				long areaLocationBytes = (long)Settings.Current.AreaLocation * 1024L * 1024L; // Bytes
				long blockOffsetBytes = (long)Settings.Current.BlockOffset * (long)blockOffsetMultiple * 1024L; // Bytes
				long jumpBytes = (long)Settings.Current.BlockSize * (long)Settings.Current.AreaRatioOuter * 1024L; // Bytes

				areaLocationBytes += blockOffsetBytes;

				uint buffSize = (uint)Settings.Current.BlockSize * 1024U; // Buffer size (Bytes)
				var buff = new byte[buffSize]; // Buffer
				uint readSize = 0U;

				var sw = new Stopwatch();
				var lapTime = new TimeSpan[readNum + 1]; // 1 is for leading zero time.
				lapTime[0] = TimeSpan.Zero; // Leading zero time

				for (int i = 0; i < loopOuter; i++)
				{
					if (0 < i)
					{
						areaLocationBytes += jumpBytes;
					}

					// Move pointer.
					var result1 = W32.SetFilePointerEx(
						hFile,
						areaLocationBytes,
						IntPtr.Zero,
						W32.FILE_BEGIN);

					if (result1 == false)
						throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to move pointer.");

					// Measure disk transfer rate (sequential read).
					for (int j = 1; j <= loopInner; j++)
					{
						sw.Start();

						var result2 = W32.ReadFile(
							hFile,
							buff,
							buffSize,
							ref readSize,
							IntPtr.Zero);

						if (result2 == false)
							throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to measure disk transfer rate.");

						sw.Stop();

						lapTime[i * loopInner + j] = sw.Elapsed;
					}
				}

				hFile.Close(); // CloseHandle is inappropriate to close SafeFileHandle.

				// ----------------
				// Process results.
				// ----------------
				// Calculate each transfer rate.				
				var data = new double[readNum];

				for (int i = 1; i <= readNum; i++)
				{
					var timeEach = (lapTime[i] - lapTime[i - 1]).TotalSeconds; // Second
					var scoreEach = Math.Floor(buffSize / timeEach) / 1000000D; // MB/s

					data[i - 1] = scoreEach;
				}

				// Calculate total transfer rate (just for reference).
				var totalTime = lapTime[readNum].TotalSeconds; // Second
				var totalRead = (double)Settings.Current.BlockSize * (double)readNum * 1024D; // Bytes

				var totalScore = Math.Floor(totalRead / totalTime) / 1000000D; // MB/s

				// Compose outcome.
				var outcome = "[Start data]" + Environment.NewLine;

				int k = 0;
				for (int i = 0; i < readNum; i++)
				{
					outcome += String.Format("{0:f6} ", data[i]); // Data have 6 decimal places.

					k++;
					if ((k == 6) |
						(i == readNum - 1))
					{
						k = 0;
						outcome += Environment.NewLine;
					}
				}

				outcome += "[End data]" + Environment.NewLine;
				outcome += String.Format("Total {0:f6} MB/s", totalScore);

				rawData.Result = ReadResult.Success;
				rawData.Outcome = outcome;
				rawData.Data = data;
			}
			catch (Win32Exception ex)
			{
				rawData.Result = ReadResult.Failure;
				rawData.Message = String.Format("{0} (Code: {1}).", ex.Message.Substring(0, ex.Message.Length - 1), ex.ErrorCode);
			}
			catch (Exception ex)
			{
				rawData.Result = ReadResult.Failure;
				rawData.Message = ex.Message;
			}
			finally
			{
				if (hFile != null)
					hFile.Dispose(); // To assure SafeFileHandle to be disposed.
			}
		}

		#endregion
	}
}
