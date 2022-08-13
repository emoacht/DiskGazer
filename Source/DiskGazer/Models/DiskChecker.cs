using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

using DiskGazer.Models.Win32;

namespace DiskGazer.Models
{
	internal static class DiskChecker
	{
		/// <summary>
		/// Gets disk information by P/Invoke.
		/// </summary>
		/// <param name="physicalDrive">Index number of physical drive</param>
		/// <returns>Disk information</returns>
		internal static DiskInfo GetDiskInfo(int physicalDrive)
		{
			var disk = new DiskInfo(physicalDrive);

			SafeFileHandle hFile = null;

			try
			{
				hFile = NativeMethod.CreateFile(
					@$"\\.\PhysicalDrive{physicalDrive}",
					NativeMethod.GENERIC_READ | NativeMethod.GENERIC_WRITE, // Administrative privilege is required. GENERIC_WRITE is for IOCTL_ATA_PASS_THROUGH.
					NativeMethod.FILE_SHARE_READ | NativeMethod.FILE_SHARE_WRITE,
					IntPtr.Zero,
					NativeMethod.OPEN_EXISTING,
					NativeMethod.FILE_ATTRIBUTE_NORMAL,
					IntPtr.Zero);
				if (hFile is null || hFile.IsInvalid)
				{
					// This is normal when this application is not run by administrator.
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get handle to disk.");
				}

				// ---------------------------------
				// Use IOCTL_STORAGE_QUERY_PROPERTY.
				// ---------------------------------
				var storageQuery = new NativeMethod.STORAGE_PROPERTY_QUERY();
				storageQuery.PropertyId = NativeMethod.StorageDeviceProperty;
				storageQuery.QueryType = NativeMethod.PropertyStandardQuery;

				var result1 = NativeMethod.DeviceIoControl(
					hFile,
					NativeMethod.IOCTL_STORAGE_QUERY_PROPERTY,
					ref storageQuery,
					(uint)Marshal.SizeOf(typeof(NativeMethod.STORAGE_PROPERTY_QUERY)),
					out NativeMethod.STORAGE_DEVICE_DESCRIPTOR storageDescriptor,
					(uint)Marshal.SizeOf(typeof(NativeMethod.STORAGE_DEVICE_DESCRIPTOR)),
					out uint bytesReturned1,
					IntPtr.Zero);
				if (result1 == false)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get disk information.");

				// Convert to byte array.
				var size = Marshal.SizeOf(storageDescriptor);
				var ptr = Marshal.AllocHGlobal(size);
				var bytes = new byte[size];

				Marshal.StructureToPtr(storageDescriptor, ptr, true);
				Marshal.Copy(ptr, bytes, 0, size);
				Marshal.FreeHGlobal(ptr);

				// Set values.
				disk.Vendor = ConvertBytesToString(bytes, (int)storageDescriptor.VendorIdOffset).Trim();
				disk.Product = ConvertBytesToString(bytes, (int)storageDescriptor.ProductIdOffset).Trim();
				disk.IsRemovable = storageDescriptor.RemovableMedia;
				disk.BusType = ConvertBusTypeToString(storageDescriptor.BusType);

				// -------------------------------
				// Use IOCTL_DISK_GET_LENGTH_INFO.
				// -------------------------------
				var result2 = NativeMethod.DeviceIoControl(
					hFile,
					NativeMethod.IOCTL_DISK_GET_LENGTH_INFO,
					IntPtr.Zero,
					0,
					out long diskSize,
					(uint)Marshal.SizeOf(typeof(long)),
					out uint bytesReturned2,
					IntPtr.Zero);
				if (result2 == false)
					throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get disk size.");

				// Set value.
				disk.SizePInvoke = diskSize;

				// ---------------------------
				// Use IOCTL_ATA_PASS_THROUGH.
				// ---------------------------
				var ataQuery = new NativeMethod.ATAIdentifyDeviceQuery();
				ataQuery.data = new ushort[256];

				ataQuery.header.Length = (ushort)Marshal.SizeOf(typeof(NativeMethod.ATA_PASS_THROUGH_EX));
				ataQuery.header.AtaFlags = NativeMethod.ATA_FLAGS.ATA_FLAGS_DATA_IN;
				ataQuery.header.DataTransferLength = (uint)ataQuery.data.Length * 2; // Size of "data" in bytes
				ataQuery.header.TimeOutValue = 3; // Sec
				ataQuery.header.DataBufferOffset = Marshal.OffsetOf(typeof(NativeMethod.ATAIdentifyDeviceQuery), "data");
				ataQuery.header.PreviousTaskFile = new byte[8];
				ataQuery.header.CurrentTaskFile = new byte[8];
				ataQuery.header.CurrentTaskFile[6] = 0xec; // ATA IDENTIFY DEVICE

				var result3 = NativeMethod.DeviceIoControl(
					hFile,
					NativeMethod.IOCTL_ATA_PASS_THROUGH,
					ref ataQuery,
					(uint)Marshal.SizeOf(typeof(NativeMethod.ATAIdentifyDeviceQuery)),
					ref ataQuery,
					(uint)Marshal.SizeOf(typeof(NativeMethod.ATAIdentifyDeviceQuery)),
					out uint bytesReturned3,
					IntPtr.Zero);
				if (result3)
				{
					const int index = 217; // Word index of nominal media rotation rate (1 means non-rotating media.)
					disk.NominalMediaRotationRate = ataQuery.data[index];
				}
				else
				{
					// None. It is normal that IOCTL_ATA_PASS_THROUGH fails when used to external or removable media.
				}
			}
			catch (Win32Exception ex)
			{
				Debug.WriteLine($"{ex.Message.Substring(0, ex.Message.Length - 1)} (Code: {ex.ErrorCode}).");
				throw;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				throw;
			}
			finally
			{
				if (hFile is not null)
				{
					// CloseHandle is inappropriate to close SafeFileHandle.
					// Dispose method is not necessary because Close method will call it internally.
					hFile.Close();
				}
			}

			return disk;
		}

		private static string ConvertBytesToString(byte[] source, int indexStart)
		{
			if ((indexStart <= 0) || // If no data, start index is zero.
				(source.Length - 1 <= indexStart))
				return string.Empty;

			var indexEnd = Array.IndexOf(source, default(byte), indexStart); // default(byte) is null.
			if (indexEnd <= 0)
				return string.Empty;

			return Encoding.ASCII.GetString(source, indexStart, indexEnd - indexStart);
		}

		private static string ConvertBusTypeToString(NativeMethod.STORAGE_BUS_TYPE type)
		{
			return type switch
			{
				NativeMethod.STORAGE_BUS_TYPE.BusTypeScsi => "SCSI",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeAtapi => "ATAPI",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeAta => "ATA",
				NativeMethod.STORAGE_BUS_TYPE.BusType1394 => "1394",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeSsa => "SSA",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeFibre => "Fibre",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeUsb => "USB",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeRAID => "RAID",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeiScsi => "iSCSI",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeSas => "SAS",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeSata => "SATA",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeSd => "SD",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeMmc => "MMC",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeNvme => "NVMe",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeSCM => "SCM",
				NativeMethod.STORAGE_BUS_TYPE.BusTypeUfs => "UFS",
				_ => Convert.ToUInt32(type).ToString(),
			};
		}
	}
}