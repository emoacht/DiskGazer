using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiskGazer.Common;

namespace DiskGazer.Models
{
	public class Settings : NotificationObject
	{
		private Settings()
		{ }

		public static Settings Current { get { return _current; } }
		private static readonly Settings _current = new Settings();

		#region Settings

		/// <summary>
		/// Index number of physical drive
		/// </summary>
		public int PhysicalDrive
		{
			get => _physicalDrive;
			set => SetProperty(ref _physicalDrive, value);
		}
		private int _physicalDrive = 0;

		/// <summary>
		/// Block size (KiB)
		/// </summary>
		public int BlockSize
		{
			get => _blockSize;
			set => SetProperty(ref _blockSize, value);
		}
		private int _blockSize = 1024;

		/// <summary>
		/// Block offset (KiB)
		/// </summary>
		/// <remarks>To divide block size so as to produce block offset.</remarks>
		public int BlockOffset
		{
			get => _blockOffset;
			set => SetProperty(ref _blockOffset, value);
		}
		private int _blockOffset = 0;

		/// <summary>
		/// Area size (MiB)
		/// </summary>
		public int AreaSize
		{
			get => _areaSize;
			set => SetProperty(ref _areaSize, value);
		}
		private int _areaSize = 1024;

		/// <summary>
		/// Area location (MiB)
		/// </summary>
		public int AreaLocation
		{
			get => _areaLocation;
			set => SetProperty(ref _areaLocation, value);
		}
		private int _areaLocation = 0;

		/// <summary>
		/// Area ratio inner (= numerator = the number of blocks in inner part of jump)
		/// </summary>
		public int AreaRatioInner
		{
			get => _areaRatioInner;
			set => SetProperty(ref _areaRatioInner, value);
		}
		private int _areaRatioInner = 8; // Fixed

		/// <summary>
		/// Area ratio outer (= denominator = the number of blocks in outer part of jump)
		/// </summary>
		public int AreaRatioOuter
		{
			get => _areaRatioOuter;
			set => SetProperty(ref _areaRatioOuter, value);
		}
		private int _areaRatioOuter = 8; // Changeable

		/// <summary>
		/// The number of runs
		/// </summary>
		public int NumRun
		{
			get => _numRun;
			set => SetProperty(ref _numRun, value);
		}
		private int _numRun = 5;

		/// <summary>
		/// Method for reading
		/// </summary>
		public ReadMethod Method
		{
			get => _method;
			set => SetProperty(ref _method, value);
		}
		private ReadMethod _method = ReadMethod.Native;

		/// <summary>
		/// Whether to remove outliers
		/// </summary>
		public bool RemovesOutlier
		{
			get => _removesOutlier;
			set => SetProperty(ref _removesOutlier, value);
		}
		private bool _removesOutlier = true;

		/// <summary>
		/// Whether to save screenshot and log automatically.
		/// </summary>
		public bool SavesScreenshotLog
		{
			get => _savesScreenshotLog;
			set => SetProperty(ref _savesScreenshotLog, value);
		}
		private bool _savesScreenshotLog;

		#endregion
	}
}