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
		/// Search disks by WMI.
		/// </summary>
		internal static List<DiskInfo> Search()
		{
			var diskRosterPre = new List<DiskInfo>();

			SearchDiskDrive(ref diskRosterPre);
			SearchPhysicalDisk(ref diskRosterPre);

			return diskRosterPre;
		}

		/// <summary>
		/// Search drives by WMI (Win32_DiskDrive).
		/// </summary>
		private static void SearchDiskDrive(ref List<DiskInfo> diskRosterPre)
		{
			var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

			foreach (var drive in searcher.Get())
			{
				if (drive["Index"] == null) // Index number of physical drive
					continue;

				int index;
				if (!int.TryParse(drive["Index"].ToString(), out index))
					continue;

				var info = new DiskInfo();
				info.PhysicalDrive = index;

				if (drive["Model"] != null)
				{
					info.Model = drive["Model"].ToString();
				}

				if (drive["InterfaceType"] != null)
				{
					info.InterfaceType = drive["InterfaceType"].ToString();
				}

				if (drive["MediaType"] != null)
				{
					info.MediaTypeDiskDrive = drive["MediaType"].ToString();
				}

				if (drive["Size"] != null)
				{
					long numSize;
					if (long.TryParse(drive["Size"].ToString(), out numSize))
						info.SizeWMI = numSize;
				}

				diskRosterPre.Add(info);
			}
		}

		/// <summary>
		/// Search drives and supplement information by WMI (MSFT_PhysicalDisk).
		/// </summary>
		/// <remarks>Windows Storage Management API is only available for Windows 8 or newer.</remarks>
		private static void SearchPhysicalDisk(ref List<DiskInfo> diskRosterPre)
		{
			if (!OsVersion.IsEightOrNewer)
				return;

			var scope = new ManagementScope("\\\\.\\root\\microsoft\\windows\\storage");
			scope.Connect();

			var searcher = new ManagementObjectSearcher("SELECT * FROM MSFT_PhysicalDisk");
			searcher.Scope = scope;

			foreach (var drive in searcher.Get())
			{
				if (drive["DeviceId"] == null) // Index number of physical drive
					continue;

				int numId;
				if (!int.TryParse(drive["DeviceId"].ToString(), out numId))
					continue;

				var info = diskRosterPre.FirstOrDefault(x => x.PhysicalDrive == numId);
				if (info == null)
					continue;

				if (drive["MediaType"] != null)
				{
					int numMediaType;
					if (int.TryParse(drive["MediaType"].ToString(), out numMediaType))
						info.MediaTypePhysicalDisk = numMediaType;
				}

				if (drive["SpindleSpeed"] != null)
				{
					uint numSpindleSpeed;
					if (uint.TryParse(drive["SpindleSpeed"].ToString(), out numSpindleSpeed))
						info.SpindleSpeed = numSpindleSpeed;
				}
			}
		}
	}
}