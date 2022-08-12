using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

using DiskGazer.Helper;

namespace DiskGazer.Models
{
	internal static class DiskSearcher
	{
		/// <summary>
		/// Searches disks by WMI.
		/// </summary>
		/// <returns>List of disk information</returns>
		internal static List<DiskInfo> Search()
		{
			var diskRosterPre = new List<DiskInfo>();

			SearchDiskDrive(ref diskRosterPre);
			SearchPhysicalDisk(ref diskRosterPre);

			return diskRosterPre;
		}

		/// <summary>
		/// Searches drives by WMI (Win32_DiskDrive).
		/// </summary>
		/// <param name="diskRosterPre">List of disk information</param>
		private static void SearchDiskDrive(ref List<DiskInfo> diskRosterPre)
		{
			using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

			foreach (var drive in searcher.Get())
			{
				if (!int.TryParse(drive["Index"]?.ToString(), out int index)) // Index number of physical drive
					continue;

				var info = new DiskInfo { PhysicalDrive = index };
				info.Model = drive["Model"]?.ToString();
				info.InterfaceType = drive["InterfaceType"]?.ToString();
				info.MediaTypeDiskDrive = drive["MediaType"]?.ToString();

				if (long.TryParse(drive["Size"]?.ToString(), out long size))
					info.SizeWMI = size;

				diskRosterPre.Add(info);
			}
		}

		/// <summary>
		/// Searches drives and supplement information by WMI (MSFT_PhysicalDisk).
		/// </summary>
		/// <param name="diskRosterPre">List of disk information</param>
		/// <remarks>Windows Storage Management API is only available for Windows 8 or newer.</remarks>
		private static void SearchPhysicalDisk(ref List<DiskInfo> diskRosterPre)
		{
			if (!OsVersion.IsEightOrNewer)
				return;

			using var searcher = new ManagementObjectSearcher(@"\\.\Root\Microsoft\Windows\Storage", "SELECT * FROM MSFT_PhysicalDisk");

			foreach (var drive in searcher.Get())
			{
				if (!int.TryParse(drive["DeviceId"]?.ToString(), out int deviceId)) // Index number of physical drive
					continue;

				var info = diskRosterPre.FirstOrDefault(x => x.PhysicalDrive == deviceId);
				if (info is null)
					continue;

				if (int.TryParse(drive["MediaType"]?.ToString(), out int mediaType))
					info.MediaTypePhysicalDisk = mediaType;

				if (uint.TryParse(drive["SpindleSpeed"]?.ToString(), out uint spindleSpeed))
					info.SpindleSpeed = spindleSpeed;
			}
		}
	}
}