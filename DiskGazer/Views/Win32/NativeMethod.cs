using System;
using System.Runtime.InteropServices;

namespace DiskGazer.Views.Win32
{
	public class NativeMethod
	{
		// Get rectangle of window.
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(
			IntPtr hWnd,
			out RECT lpRect);

		// Get rectangle of window's client area.
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetClientRect(
			IntPtr hWnd,
			out RECT lpRect);

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;   // X of upper-left corner
			public int Top;    // Y of upper-left corner
			public int Right;  // X of lower-right corner
			public int Bottom; // Y of lower-right corner
		}

		// Check if DWM composition is enabled (Windows Vista or later).
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmIsCompositionEnabled(
			[MarshalAs(UnmanagedType.Bool)]
			ref bool pfEnabled);

		// Get rectangle of window under DWM.
		[DllImport("dwmapi.dll", SetLastError = true)]
		public static extern int DwmGetWindowAttribute(
			IntPtr hwnd,
			int dwAttribute,
			ref RECT pvAttribute,
			int cbAttribute);

		public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

		// Get handle to foreground (focused) window.
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetForegroundWindow();

		// Set foreground window.
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(
			IntPtr hWnd);

		// Get process ID for thread that created that window.
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(
			IntPtr hWnd,
			IntPtr lpdwProcessId);

		// Attach thread to another thread.
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool AttachThreadInput(
			uint idAttach,
			uint idAttachTo,
			[MarshalAs(UnmanagedType.Bool)]
			bool fAttach);

		// Set position, size and Z order of window.
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int X, int Y,
			int cx, int cy,
			uint uFlags);

		public const uint SWP_NOSIZE = 0x0001;
		public const uint SWP_NOMOVE = 0x0002;
		public const uint SWP_SHOWWINDOW = 0x0040;

		// Get handle to window that contains specific point.
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr WindowFromPoint(
			POINT Point);

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
		}

		// Retrieve handle to ancestor of specified window.
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetAncestor(
			IntPtr Hwnd,
			uint gaFlags);

		public const uint GA_PARENT = 1;
		public const uint GA_ROOT = 2;
		public const uint GA_ROOTOWNER = 3;
	}
}