using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Models
{
	internal class ProgressInfo
	{
		/// <summary>
		/// Status for Main window
		/// </summary>
		public string Status { get; }

		/// <summary>
		/// Inner status for Monitor window
		/// </summary>
		public string InnerStatus { get; }

		/// <summary>
		/// Whether inner status is renewed
		/// </summary>
		public bool IsInnerStatusRenewed { get; }

		/// <summary>
		/// Data of runs
		/// </summary>
		public Dictionary<double, double> Data { get; }

		#region Constructor

		public ProgressInfo()
		{
			// Set null to distinguish whether any valid value is set.
			this.Status = null;
			this.InnerStatus = null;
		}

		public ProgressInfo(Dictionary<double, double> data) : this()
		{
			this.Data = data;
		}

		public ProgressInfo(string status) : this()
		{
			this.Status = status;
		}

		public ProgressInfo(string innerStatus, bool isInnerStatusRenewed) : this()
		{
			this.InnerStatus = innerStatus;
			this.IsInnerStatusRenewed = isInnerStatusRenewed;
		}

		public ProgressInfo(string status, string innerStatus, bool isInnerStatusRenewed)
		{
			this.Status = status;
			this.InnerStatus = innerStatus;
			this.IsInnerStatusRenewed = isInnerStatusRenewed;
		}

		#endregion
	}
}