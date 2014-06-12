using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Helper
{
	public static class Account
	{
		#region Win32

		// Check if this application is run by administrator.
		[DllImport("Shell32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsUserAnAdmin();

		#endregion


		/// <summary>
		/// Whether this application is run by administrator
		/// </summary>
		public static bool IsAdmin
		{
			get { return IsUserAnAdmin(); }
		}
	}
}
