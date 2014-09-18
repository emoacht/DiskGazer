using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using DiskGazer.Helper;

namespace DiskGazer.Views
{
	internal static class WindowSupplement
	{
		/// <summary>
		/// Get rectangle of a specified window.
		/// </summary>
		/// <param name="source">Source window</param>
		internal static Rect GetWindowRect(Window source)
		{
			var rct = new W32.RECT();

			try
			{
				var handle = new WindowInteropHelper(source).Handle;

				// For Windows XP or older
				var result = W32.GetWindowRect(
					handle,
					out rct);

				if (result == false)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				if (OsVersion.IsVistaOrNewer)
				{
					// For Windows Vista or newer
					bool isEnabled = false;

					var hresult1 = W32.DwmIsCompositionEnabled(ref isEnabled);
					if (hresult1 != W32.S_OK)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (isEnabled)
					{
						var hresult2 = W32.DwmGetWindowAttribute(
							handle,
							W32.DWMWA_EXTENDED_FRAME_BOUNDS,
							ref rct,
							Marshal.SizeOf(typeof(W32.RECT)));

						if (hresult2 != W32.S_OK)
							throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get window rect (Code: {0}).", ex.ErrorCode), ex);
			}

			return new Rect(new Point(rct.Left, rct.Top), new Point(rct.Right, rct.Bottom));
		}

		/// <summary>
		/// Get size of client area of a specified window.
		/// </summary>
		/// <param name="source">Source window</param>
		internal static Size GetClientAreaSize(Window source)
		{
			var rct = new W32.RECT();

			try
			{
				var handle = new WindowInteropHelper(source).Handle;

				// GetClientRect method only provides size but not position (Left and Top are always 0).
				var result = W32.GetClientRect(
					handle,
					out rct);

				if (result == false)
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get client area rect (Code: {0}).", ex.ErrorCode), ex);
			}

			return new Size(rct.Right, rct.Bottom);
		}

		/// <summary>
		/// Activate a specified window.
		/// </summary>
		/// <param name="target">Target window</param>
		internal static void ActivateWindow(Window target)
		{
			var handle = new WindowInteropHelper(target).Handle;

			try
			{
				// Get process ID for this window's thread.
				var thisWindowThreadId = W32.GetWindowThreadProcessId(
					handle,
					IntPtr.Zero);

				if (thisWindowThreadId == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get process ID for this window.");

				// Get process ID for foreground window's thread.
				var foregroundWindow = W32.GetForegroundWindow();
				if (foregroundWindow == null)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get handle to foreground window.");

				var foregroundWindowThreadId = W32.GetWindowThreadProcessId(
					foregroundWindow,
					IntPtr.Zero);

				if (foregroundWindowThreadId == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get process ID for foreground window.");

				if (thisWindowThreadId != foregroundWindowThreadId)
				{
					// Attach this window's thread to foreground window's thread.
					var result1 = W32.AttachThreadInput(
						foregroundWindowThreadId,
						thisWindowThreadId,
						true);

					if (result1 == false)
						throw new Win32Exception(Marshal.GetLastWin32Error(), String.Format("Failed to attach thread ({0}) to thread ({1}).", foregroundWindowThreadId, thisWindowThreadId));

					// Set position of this window.
					var result2 = W32.SetWindowPos(
						handle,
						new IntPtr(0),
						0, 0,
						0, 0,
						W32.SWP_NOSIZE | W32.SWP_NOMOVE | W32.SWP_SHOWWINDOW);

					if (result2 == false)
						throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set position of this window.");

					// Detach this window's thread from foreground window's thread.
					var result3 = W32.AttachThreadInput(
						foregroundWindowThreadId,
						thisWindowThreadId,
						false);

					if (result3 == false)
						throw new Win32Exception(Marshal.GetLastWin32Error(), String.Format("Failed to detach thread ({0}) from thread ({1}).", foregroundWindowThreadId, thisWindowThreadId));
				}
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("{0} (Code: {1}).", ex.Message.Substring(0, ex.Message.Length - 1), ex.ErrorCode), ex);
			}

			// Show and activate this window.
			if (target.WindowState == WindowState.Minimized)
				target.WindowState = WindowState.Normal;

			target.Show();
			target.Activate();
		}

		/// <summary>
		/// Check if a specified window is activated.
		/// </summary>
		/// <param name="target">Target window</param>
		/// <returns>True if activated</returns>
		internal static bool IsWindowActivated(Window target)
		{
			// Prepare points where this window is supposed to be shown.
			var points = new List<W32.POINT>();

			var rct = GetWindowRect(target);
			var rnd = new Random();

			for (int i = 0; i <= 9; i++) // 10 points.
			{
				var point = new W32.POINT()
				{
					X = rnd.Next((int)rct.Left, (int)rct.Right),
					Y = rnd.Next((int)rct.Top, (int)rct.Bottom),
				};

				points.Add(point);
			}

			// Check handles at the points.
			var handleWindow = new WindowInteropHelper(target).Handle;

			try
			{
				foreach (var point in points)
				{
					var handlePoint = W32.WindowFromPoint(point);
					if (handlePoint == null)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (handlePoint == handleWindow)
						continue;

					var handleAncestor = W32.GetAncestor(
						handlePoint,
						W32.GA_ROOT);

					if (handleAncestor == null)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (handleAncestor != handleWindow)
						return false;
				}
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get handles where this window is supposed to be shown (Code: {0}).", ex.ErrorCode), ex);
			}

			return true;
		}

		/// <summary>
		/// Get rate of current DPI of a specified window against 96.
		/// </summary>
		/// <param name="window">Source window</param>
		internal static double GetDpiRate(Window window)
		{
			var source = PresentationSource.FromVisual(window);
			if ((source == null) || (source.CompositionTarget == null))
				return 1D; // Fall back

			return source.CompositionTarget.TransformToDevice.M11;
		}
	}
}
