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
			get { return _physicalDrive; }
			set
			{
				_physicalDrive = value;
				RaisePropertyChanged();
			}
		}
		private int _physicalDrive = 0;

		/// <summary>
		/// Block size (KiB)
		/// </summary>
		public int BlockSize
		{
			get { return _blockSize; }
			set
			{
				_blockSize = value;
				RaisePropertyChanged();
			}
		}
		private int _blockSize = 1024;

		/// <summary>
		/// Block offset (KiB)
		/// </summary>
		/// <remarks>To divide block size so as to produce block offset.</remarks>
		public int BlockOffset
		{
			get { return _blockOffset; }
			set
			{
				_blockOffset = value;
				RaisePropertyChanged();
			}
		}
		private int _blockOffset = 0;

		/// <summary>
		/// Area size (MiB)
		/// </summary>
		public int AreaSize
		{
			get { return _areaSize; }
			set
			{
				_areaSize = value;
				RaisePropertyChanged();
			}
		}
		private int _areaSize = 1024;

		/// <summary>
		/// Area location (MiB)
		/// </summary>
		public int AreaLocation
		{
			get { return _areaLocation; }
			set
			{
				_areaLocation = value;
				RaisePropertyChanged();
			}
		}
		private int _areaLocation = 0;

		/// <summary>
		/// Area ratio inner (= numerator = the number of blocks in inner part of jump)
		/// </summary>
		public int AreaRatioInner
		{
			get { return _areaRatioInner; }
			set
			{
				_areaRatioInner = value;
				RaisePropertyChanged();
			}
		}
		private int _areaRatioInner = 8; // Fixed

		/// <summary>
		/// Area ratio outer (= denominator = the number of blocks in outer part of jump)
		/// </summary>
		public int AreaRatioOuter
		{
			get { return _areaRatioOuter; }
			set
			{
				_areaRatioOuter = value;
				RaisePropertyChanged();
			}
		}
		private int _areaRatioOuter = 8; // Changeable

		/// <summary>
		/// The number of runs
		/// </summary>
		public int NumRun
		{
			get { return _numRun; }
			set
			{
				_numRun = value;
				RaisePropertyChanged();
			}
		}
		private int _numRun = 5;

		/// <summary>
		/// Method for reading
		/// </summary>
		public ReadMethod Method
		{
			get { return _method; }
			set
			{
				_method = value;
				RaisePropertyChanged();
			}
		}
		private ReadMethod _method = ReadMethod.Native;

		/// <summary>
		/// Whether to remove outliers
		/// </summary>
		public bool RemovesOutlier
		{
			get { return _removesOutlier; }
			set
			{
				_removesOutlier = value;
				RaisePropertyChanged();
			}
		}
		private bool _removesOutlier = true;

		/// <summary>
		/// Whether to save screenshot and log automatically.
		/// </summary>
		public bool SavesScreenshotLog
		{
			get { return _savesScreenshotLog; }
			set
			{
				_savesScreenshotLog = value;
				RaisePropertyChanged();
			}
		}
		private bool _savesScreenshotLog;

		#endregion
	}
}