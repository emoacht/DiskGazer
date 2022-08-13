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
			var disks = new List<DiskInfo>();

			SearchDiskDrive(ref disks);
			SearchPhysicalDisk(ref disks);

			return disks;
		}

		/// <summary>
		/// Searches drives by WMI (Win32_DiskDrive).
		/// </summary>
		private static void SearchDiskDrive(ref List<DiskInfo> disks)
		{
			using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

			foreach (var drive in searcher.Get())
			{
				if (!int.TryParse(drive["Index"]?.ToString(), out int index)) // Index number of physical drive
					continue;

				var disk = new DiskInfo(index);
				disk.Model = drive["Model"]?.ToString();
				disk.InterfaceType = drive["InterfaceType"]?.ToString();
				disk.MediaTypeDiskDrive = drive["MediaType"]?.ToString();

				if (long.TryParse(drive["Size"]?.ToString(), out long size))
					disk.SizeWMI = size;

				disks.Add(disk);
			}
		}

		/// <summary>
		/// Searches drives and supplement information by WMI (MSFT_PhysicalDisk).
		/// </summary>
		/// <remarks>Windows Storage Management API is only available for Windows 8 or newer.</remarks>
		private static void SearchPhysicalDisk(ref List<DiskInfo> disks)
		{
			if (!OsVersion.IsEightOrNewer)
				return;

			using var searcher = new ManagementObjectSearcher(@"\\.\Root\Microsoft\Windows\Storage", "SELECT * FROM MSFT_PhysicalDisk");

			foreach (var drive in searcher.Get())
			{
				if (!int.TryParse(drive["DeviceId"]?.ToString(), out int deviceId)) // Index number of physical drive
					continue;

				var disk = disks.FirstOrDefault(x => x.PhysicalDrive == deviceId);
				if (disk is null)
					continue;

				if (int.TryParse(drive["MediaType"]?.ToString(), out int mediaType))
					disk.MediaTypePhysicalDisk = mediaType;

				if (uint.TryParse(drive["SpindleSpeed"]?.ToString(), out uint spindleSpeed))
					disk.SpindleSpeed = spindleSpeed;
			}
		}
	}
}