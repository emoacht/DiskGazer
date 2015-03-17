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
using DiskGazer.Views.Win32;

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
			var targetRect = new NativeMethod.RECT();

			try
			{
				var handle = new WindowInteropHelper(source).Handle;

				// For Windows XP or older
				var result = NativeMethod.GetWindowRect(
					handle,
					out targetRect);

				if (result == false)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				if (OsVersion.IsVistaOrNewer)
				{
					// For Windows Vista or newer
					bool isEnabled = false;

					var result1 = NativeMethod.DwmIsCompositionEnabled(
						ref isEnabled);
					if (result1 != 0) // 0 means S_OK.
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (isEnabled)
					{
						var result2 = NativeMethod.DwmGetWindowAttribute(
							handle,
							NativeMethod.DWMWA_EXTENDED_FRAME_BOUNDS,
							ref targetRect,
							Marshal.SizeOf(typeof(NativeMethod.RECT)));
						if (result2 != 0) // 0 means S_OK.
							throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get window rect (Code: {0}).", ex.ErrorCode), ex);
			}

			return new Rect(new Point(targetRect.Left, targetRect.Top), new Point(targetRect.Right, targetRect.Bottom));
		}

		/// <summary>
		/// Get size of client area of a specified window.
		/// </summary>
		/// <param name="source">Source window</param>
		internal static Size GetClientAreaSize(Window source)
		{
			var targetRect = new NativeMethod.RECT();

			try
			{
				var handle = new WindowInteropHelper(source).Handle;

				// GetClientRect method only provides size but not position (Left and Top are always 0).
				var result = NativeMethod.GetClientRect(
					handle,
					out targetRect);
				if (result == false)
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get client area rect (Code: {0}).", ex.ErrorCode), ex);
			}

			return new Size(targetRect.Right, targetRect.Bottom);
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
				var thisWindowThreadId = NativeMethod.GetWindowThreadProcessId(
					handle,
					IntPtr.Zero);
				if (thisWindowThreadId == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get process ID for this window.");

				// Get process ID for foreground window's thread.
				var foregroundWindow = NativeMethod.GetForegroundWindow();
				if (foregroundWindow == null)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get handle to foreground window.");

				var foregroundWindowThreadId = NativeMethod.GetWindowThreadProcessId(
					foregroundWindow,
					IntPtr.Zero);
				if (foregroundWindowThreadId == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get process ID for foreground window.");

				if (thisWindowThreadId != foregroundWindowThreadId)
				{
					// Attach this window's thread to foreground window's thread.
					var result1 = NativeMethod.AttachThreadInput(
						foregroundWindowThreadId,
						thisWindowThreadId,
						true);
					if (result1 == false)
						throw new Win32Exception(Marshal.GetLastWin32Error(), String.Format("Failed to attach thread ({0}) to thread ({1}).", foregroundWindowThreadId, thisWindowThreadId));

					// Set position of this window.
					var result2 = NativeMethod.SetWindowPos(
						handle,
						new IntPtr(0),
						0, 0,
						0, 0,
						NativeMethod.SWP_NOSIZE | NativeMethod.SWP_NOMOVE | NativeMethod.SWP_SHOWWINDOW);
					if (result2 == false)
						throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set position of this window.");

					// Detach this window's thread from foreground window's thread.
					var result3 = NativeMethod.AttachThreadInput(
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
			var targetRect = GetWindowRect(target);
			var random = new Random();

			var points = Enumerable.Range(0, 10) // 10 points.
				.Select(x => new NativeMethod.POINT
				{
					X = random.Next((int)targetRect.Left, (int)targetRect.Right),
					Y = random.Next((int)targetRect.Top, (int)targetRect.Bottom),
				});

			// Check handles at the points.
			var handleWindow = new WindowInteropHelper(target).Handle;

			try
			{
				foreach (var point in points)
				{
					var handlePoint = NativeMethod.WindowFromPoint(
						point);
					if (handlePoint == null)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (handlePoint == handleWindow)
						continue;

					var handleAncestor = NativeMethod.GetAncestor(
						handlePoint,
						NativeMethod.GA_ROOT);
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