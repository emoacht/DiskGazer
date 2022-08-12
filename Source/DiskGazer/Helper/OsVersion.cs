using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Helper
{
	/// <summary>
	/// OS version information
	/// </summary>
	public static class OsVersion
	{
		/// <summary>
		/// Whether OS is Vista or newer
		/// </summary>
		/// <remarks>Windows Vista = version 6.0</remarks>
		public static bool IsVistaOrNewer
		{
			get => Environment.OSVersion.Version is { Major: >= 6 };
		}

		/// <summary>
		/// Whether OS is Windows 8 or newer
		/// </summary>
		/// <remarks>Windows 8 = version 6.2</remarks>
		public static bool IsEightOrNewer
		{
			get => Environment.OSVersion.Version is { Major: 6, Minor: >= 2 } or { Major: >= 7 };
		}
	}
}