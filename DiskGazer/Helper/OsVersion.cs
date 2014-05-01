using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Helper
{
	public static class OsVersion
	{
		private static readonly OperatingSystem os = Environment.OSVersion;

		/// <summary>
		/// Check if OS is Vista or newer.
		/// </summary>
		/// <remarks>Windows Vista = version 6.0</remarks>
		public static bool IsVistaOrNewer
		{
			get { return (6 <= os.Version.Major); }
		}

		/// <summary>
		/// Check if OS is Windows 8 or newer.
		/// </summary>
		/// <remarks>Windows 8 = version 6.2</remarks>
		public static bool IsEightOrNewer
		{
			get { return (6 <= os.Version.Major) && (2 <= os.Version.Minor); }
		}
	}
}
