using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Models
{
	/// <summary>
	/// Score of each series of runs
	/// </summary>
	internal class DiskScore
	{
		/// <summary>
		/// GUID to identify each score
		/// </summary>
		public string Guid => _guid ??= System.Guid.NewGuid().ToString();
		private string _guid;

		public DiskInfo Disk { get; set; }

		public int BlockSize { get; set; }
		public int BlockOffset { get; set; }
		public int AreaSize { get; set; }
		public int AreaLocation { get; set; }
		public int AreaRatioInner { get; set; }
		public int AreaRatioOuter { get; set; }
		public int NumRun { get; set; }
		public ReadMethod Method { get; set; }

		/// <summary>
		/// Starting time of runs
		/// </summary>
		public DateTime StartTime { get; set; }

		/// <summary>
		/// Data of runs
		/// </summary>
		/// <remarks>Key: location (MiB), Value: transfer rate (MB/s) at that location</remarks>
		public Dictionary<double, double> Data { get; set; }

		/// <summary>
		/// Whether the corresponding chart line is pinned
		/// </summary>
		public bool IsPinned { get; set; }
	}
}