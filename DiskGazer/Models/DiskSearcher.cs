using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Models
{
	internal static class DiskSearcher
	{
		/// <summary>
		/// Search disks by WMI.
		/// </summary>
		internal static List<DiskInfo> Search()
		{
			const string strPhysical = "PHYSICALDRIVE";

			var diskRosterPre = new List<DiskInfo>();

			var drives = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

			foreach (var drive in drives.Get())
			{
				if (drive["DeviceID"] == null)
					continue;

				var strDeviceId = drive["DeviceID"].ToString();
				int numDeviceId;
				if (!int.TryParse(strDeviceId.Substring(strDeviceId.IndexOf(strPhysical, StringComparison.InvariantCulture) + strPhysical.Length), out numDeviceId))
					continue;

				var info = new DiskInfo();
				info.PhysicalDrive = numDeviceId;

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
					info.MediaType = drive["MediaType"].ToString();
				}

				if (drive["Size"] != null)
				{
					var strSize = drive["Size"].ToString();
					long numSize;
					if (long.TryParse(strSize, out numSize))
					{
						info.SizeWMI = numSize;
					}
				}

				diskRosterPre.Add(info);
			}

			return diskRosterPre;
		}
	}
}
