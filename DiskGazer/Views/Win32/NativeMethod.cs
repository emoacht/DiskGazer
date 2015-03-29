using System;
using System.Runtime.InteropServices;

namespace DiskGazer.Views.Win32
{
	public class NativeMethod
	{
		// Get rectangle of window.
		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(
			IntPtr hWnd,
			out RECT lpRect);

		// Get rectangle of window's client area.
		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetClientRect(
			IntPtr hWnd,
			out RECT lpRect);

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public System.Windows.Rect ToRect()
			{
				return new System.Windows.Rect(
					(double)this.left,
					(double)this.top,
					(double)(this.right - this.left),
					(double)(this.bottom - this.top));
			}

			public System.Windows.Size ToSize()
			{
				return new System.Windows.Size(
					(double)(this.right - this.left),
					(double)(this.bottom - this.top));
			}
		}

		// Check if DWM composition is enabled (for Windows Vista or newer).
		[DllImport("Dwmapi.dll", SetLastError = true)]
		public static extern int DwmIsCompositionEnabled(
			[MarshalAs(UnmanagedType.Bool)]
			ref bool pfEnabled);

		// Get rectangle of window under DWM.
		[DllImport("Dwmapi.dll", SetLastError = true)]
		public static extern int DwmGetWindowAttribute(
			IntPtr hwnd,
			int dwAttribute,
			ref RECT pvAttribute,
			int cbAttribute);

		public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

		// Get handle to foreground (focused) window.
		[DllImport("User32.dll", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		// Set foreground window.
		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(
			IntPtr hWnd);

		// Get process ID for thread that created specified window.
		[DllImport("User32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(
			IntPtr hWnd,
			IntPtr lpdwProcessId);

		// Attach thread to another thread.
		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AttachThreadInput(
			uint idAttach,
			uint idAttachTo,
			[MarshalAs(UnmanagedType.Bool)]
			bool fAttach);

		// Set position, size and Z order of window.
		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int X,
			int Y,
			int cx,
			int cy,
			uint uFlags);

		public const uint SWP_NOSIZE = 0x0001;
		public const uint SWP_NOMOVE = 0x0002;
		public const uint SWP_SHOWWINDOW = 0x0040;

		// Get handle to window that contains specified point.
		[DllImport("User32.dll", SetLastError = true)]
		public static extern IntPtr WindowFromPoint(
			POINT Point);

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;
		}

		// Get handle to ancestor of specified window.
		[DllImport("User32.dll", SetLastError = true)]
		public static extern IntPtr GetAncestor(
			IntPtr Hwnd,
			uint gaFlags);

		public const uint GA_PARENT = 1;
		public const uint GA_ROOT = 2;
		public const uint GA_ROOTOWNER = 3;
	}
}