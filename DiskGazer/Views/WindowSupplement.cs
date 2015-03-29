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
		/// <param name="source">Source Window</param>
		internal static Rect GetWindowRect(Window source)
		{
			var handleWindow = new WindowInteropHelper(source).Handle;

			try
			{
				NativeMethod.RECT targetRect;

				// For Windows XP or older
				var result = NativeMethod.GetWindowRect(
					handleWindow,
					out targetRect);
				if (!result)
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
							handleWindow,
							NativeMethod.DWMWA_EXTENDED_FRAME_BOUNDS,
							ref targetRect,
							Marshal.SizeOf(typeof(NativeMethod.RECT)));
						if (result2 != 0) // 0 means S_OK.
							throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}

				return targetRect.ToRect();
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get window rect (Code: {0}).", ex.ErrorCode), ex);
			}
		}

		/// <summary>
		/// Get size of client area of a specified window.
		/// </summary>
		/// <param name="source">Source Window</param>
		internal static Size GetClientAreaSize(Window source)
		{
			var handleWindow = new WindowInteropHelper(source).Handle;

			try
			{
				NativeMethod.RECT targetRect;

				var result = NativeMethod.GetClientRect(
					handleWindow,
					out targetRect);
				if (!result)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				return targetRect.ToSize();
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get client area rect (Code: {0}).", ex.ErrorCode), ex);
			}
		}

		/// <summary>
		/// Activate a specified window.
		/// </summary>
		/// <param name="target">Target Window</param>
		internal static void ActivateWindow(Window target)
		{
			var handleWindow = new WindowInteropHelper(target).Handle;

			try
			{
				// Get process ID for target window's thread.
				var targetWindowId = NativeMethod.GetWindowThreadProcessId(
					handleWindow,
					IntPtr.Zero);
				if (targetWindowId == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get process ID for target window.");

				// Get process ID for the foreground window's thread.
				var foregroundWindow = NativeMethod.GetForegroundWindow();
				if (foregroundWindow == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get handle to the foreground window.");

				var foregroundWindowId = NativeMethod.GetWindowThreadProcessId(
					foregroundWindow,
					IntPtr.Zero);
				if (foregroundWindowId == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get process ID for the foreground window.");

				if (targetWindowId != foregroundWindowId)
				{
					try
					{
						// Attach target window's thread to the foreground window's thread.
						var result1 = NativeMethod.AttachThreadInput(
							foregroundWindowId,
							targetWindowId,
							true);
						if (!result1)
							throw new Win32Exception(Marshal.GetLastWin32Error(), String.Format("Failed to attach thread ({0}) to thread ({1}).", foregroundWindowId, targetWindowId));

						// Set position of target window.
						var result2 = NativeMethod.SetWindowPos(
							handleWindow,
							IntPtr.Zero,
							0,
							0,
							0,
							0,
							NativeMethod.SWP_NOSIZE | NativeMethod.SWP_NOMOVE | NativeMethod.SWP_SHOWWINDOW);
						if (!result2)
							throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set position of target window.");
					}
					finally
					{
						// Detach target window's thread from the foreground window's thread.
						var result3 = NativeMethod.AttachThreadInput(
							foregroundWindowId,
							targetWindowId,
							false);
						if (!result3)
							throw new Win32Exception(Marshal.GetLastWin32Error(), String.Format("Failed to detach thread ({0}) from thread ({1}).", foregroundWindowId, targetWindowId));
					}
				}
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("{0} (Code: {1}).", ex.Message.Substring(0, ex.Message.Length - 1), ex.ErrorCode), ex);
			}

			// Show and activate target window.
			if (target.WindowState == WindowState.Minimized)
				target.WindowState = WindowState.Normal;

			target.Show();
			target.Activate();
		}

		/// <summary>
		/// Check if a specified window is activated.
		/// </summary>
		/// <param name="target">Target Window</param>
		/// <returns>True if activated</returns>
		internal static bool IsWindowActivated(Window target)
		{
			// Prepare points where target window is supposed to be shown.
			var targetRect = GetWindowRect(target);
			var random = new Random();

			var points = Enumerable.Range(0, 10) // 10 points.
				.Select(_ => new NativeMethod.POINT
				{
					x = random.Next((int)targetRect.Left, (int)targetRect.Right),
					y = random.Next((int)targetRect.Top, (int)targetRect.Bottom),
				});

			// Check handles at each point.
			var handleWindow = new WindowInteropHelper(target).Handle;

			try
			{
				foreach (var point in points)
				{
					var handlePoint = NativeMethod.WindowFromPoint(
						point);
					if (handlePoint == IntPtr.Zero)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (handlePoint == handleWindow)
						continue;

					var handleAncestor = NativeMethod.GetAncestor(
						handlePoint,
						NativeMethod.GA_ROOT);
					if (handleAncestor == IntPtr.Zero)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (handleAncestor != handleWindow)
						return false;
				}

				return true;
			}
			catch (Win32Exception ex)
			{
				throw new Exception(String.Format("Failed to get handles where target window is supposed to be shown (Code: {0}).", ex.ErrorCode), ex);
			}
		}
	}
}