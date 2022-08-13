using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Models
{
	/// <summary>
	/// Raw data of reading
	/// </summary>
	internal struct RawData
	{
		/// <summary>
		/// Ordinal number of runs
		/// </summary>
		public int Run;

		/// <summary>
		/// Ordinal number of steps
		/// </summary>
		public int Step;

		/// <summary>
		/// Multiple of block offset
		/// </summary>
		public int BlockOffsetMultiple;

		/// <summary>
		/// Result
		/// </summary>
		public ReadResult Result;

		/// <summary>
		/// Outcome
		/// </summary>
		/// <remarks>This consists of data string and error message (if any).</remarks>
		public string Outcome;

		/// <summary>
		/// Transfer rate (MB/s)
		/// </summary>
		public double[] Data;

		/// <summary>
		/// Error message
		/// </summary>
		public string Message;
	}
}