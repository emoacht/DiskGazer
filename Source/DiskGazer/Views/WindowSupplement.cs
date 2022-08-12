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
		/// Gets rectangle of a specified window.
		/// </summary>
		/// <param name="source">Source Window</param>
		internal static Rect GetWindowRect(Window source)
		{
			var windowHandle = new WindowInteropHelper(source).Handle;

			try
			{
				// For Windows XP or older
				var result1 = NativeMethod.GetWindowRect(
					windowHandle,
					out NativeMethod.RECT targetRect);
				if (!result1)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				if (OsVersion.IsVistaOrNewer)
				{
					// For Windows Vista or newer
					bool isEnabled = false;

					var result2 = NativeMethod.DwmIsCompositionEnabled(
						ref isEnabled);
					if (result2 != 0) // 0 means S_OK.
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (isEnabled)
					{
						var result3 = NativeMethod.DwmGetWindowAttribute(
							windowHandle,
							NativeMethod.DWMWA_EXTENDED_FRAME_BOUNDS,
							ref targetRect,
							Marshal.SizeOf(typeof(NativeMethod.RECT)));
						if (result3 != 0) // 0 means S_OK.
							throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}

				return targetRect.ToRect();
			}
			catch (Win32Exception ex)
			{
				throw new Exception($"Failed to get window rect (Code: {ex.ErrorCode}).", ex);
			}
		}

		/// <summary>
		/// Gets size of client area of a specified window.
		/// </summary>
		/// <param name="source">Source Window</param>
		internal static Size GetClientAreaSize(Window source)
		{
			var windowHandle = new WindowInteropHelper(source).Handle;

			try
			{
				var result = NativeMethod.GetClientRect(
					windowHandle,
					out NativeMethod.RECT targetRect);
				if (!result)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				return targetRect.ToSize();
			}
			catch (Win32Exception ex)
			{
				throw new Exception($"Failed to get client area rect (Code: {ex.ErrorCode}).", ex);
			}
		}

		/// <summary>
		/// Activates a specified window.
		/// </summary>
		/// <param name="target">Target Window</param>
		internal static void ActivateWindow(Window target)
		{
			var windowHandle = new WindowInteropHelper(target).Handle;

			try
			{
				// Get process ID for target window's thread.
				var targetWindowId = NativeMethod.GetWindowThreadProcessId(
					windowHandle,
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
							throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to attach thread ({foregroundWindowId}) to thread ({targetWindowId}).");

						// Set position of target window.
						var result2 = NativeMethod.SetWindowPos(
							windowHandle,
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
							throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to detach thread ({foregroundWindowId}) from thread ({targetWindowId}).");
					}
				}
			}
			catch (Win32Exception ex)
			{
				throw new Exception($"{ex.Message.Substring(0, ex.Message.Length - 1)} (Code: {ex.ErrorCode}).", ex);
			}

			// Show and activate target window.
			if (target.WindowState == WindowState.Minimized)
				target.WindowState = WindowState.Normal;

			target.Show();
			target.Activate();
		}

		/// <summary>
		/// Checks if a specified window is activated.
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
			var windowHandle = new WindowInteropHelper(target).Handle;

			try
			{
				foreach (var point in points)
				{
					var pointHandle = NativeMethod.WindowFromPoint(
						point);
					if (pointHandle == IntPtr.Zero)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (pointHandle == windowHandle)
						continue;

					var ancestorHandle = NativeMethod.GetAncestor(
						pointHandle,
						NativeMethod.GA_ROOT);
					if (ancestorHandle == IntPtr.Zero)
						throw new Win32Exception(Marshal.GetLastWin32Error());

					if (ancestorHandle != windowHandle)
						return false;
				}

				return true;
			}
			catch (Win32Exception ex)
			{
				throw new Exception($"Failed to get handles where target window is supposed to be shown (Code: {ex.ErrorCode}).", ex);
			}
		}
	}
}