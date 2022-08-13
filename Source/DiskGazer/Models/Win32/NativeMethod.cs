using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace DiskGazer.Models.Win32
{
	internal static class NativeMethod
	{
		// Get handle to a specified disk.
		[DllImport("Kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true)]
		public static extern SafeFileHandle CreateFile(
			[MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		public const uint GENERIC_READ = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;
		public const uint FILE_SHARE_READ = 0x00000001;
		public const uint FILE_SHARE_WRITE = 0x00000002;
		public const uint OPEN_EXISTING = 3;
		public const uint FILE_ATTRIBUTE_DEVICE = 0x40;
		public const uint FILE_ATTRIBUTE_NORMAL = 0x80;

		public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
		public const uint FILE_FLAG_DELETE_ON_CLOSE = 0x4000000;
		public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
		public const uint FILE_FLAG_OPEN_NO_RECALL = 0x100000;
		public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x200000;
		public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
		public const uint FILE_FLAG_POSIX_SEMANTICS = 0x1000000;
		public const uint FILE_FLAG_RANDOM_ACCESS = 0x10000000;
		public const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000;
		public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;

		// Get information on a specified disk.
		[DllImport("Kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeviceIoControl(
			SafeFileHandle hDevice,
			uint dwIoControlCode,
			ref STORAGE_PROPERTY_QUERY lpInBuffer, // To get disk information.
			uint nInBufferSize,
			out STORAGE_DEVICE_DESCRIPTOR lpOutBuffer, // To get disk information.
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped);

		// Get size of a specified disk.
		[DllImport("Kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeviceIoControl(
			SafeFileHandle hDevice,
			uint dwIoControlCode,
			IntPtr lpInBuffer, // To get disk size.
			uint nInBufferSize,
			out long lpOutBuffer, // To get disk size.
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped);

		// Get nominal media rotation rate.
		[DllImport("Kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeviceIoControl(
			SafeFileHandle hDevice,
			uint dwIoControlCode,
			ref ATAIdentifyDeviceQuery lpInBuffer, // To get nominal media rotation rate
			uint nInBufferSize,
			ref ATAIdentifyDeviceQuery lpOutBuffer, // To get nominal media rotation rate
			uint nOutBufferSize,
			out uint lpBytesReturned,
			IntPtr lpOverlapped);

		public static uint IOCTL_STORAGE_QUERY_PROPERTY = CTL_CODE(
			IOCTL_STORAGE_BASE,
			0x500,
			METHOD_BUFFERED,
			FILE_ANY_ACCESS);

		public static uint IOCTL_DISK_GET_LENGTH_INFO = CTL_CODE(
			IOCTL_DISK_BASE,
			0x0017,
			METHOD_BUFFERED,
			FILE_READ_ACCESS);

		public static uint IOCTL_ATA_PASS_THROUGH = CTL_CODE(
			IOCTL_SCSI_BASE,
			0x040b,
			METHOD_BUFFERED,
			FILE_READ_ACCESS | FILE_WRITE_ACCESS);

		private static uint CTL_CODE(
			uint deviceType,
			uint function,
			uint method,
			uint access)
		{
			return ((deviceType << 16) | (access << 14) | (function << 2) | method);
		}

		private const uint FILE_DEVICE_CONTROLLER = 0x00000004;
		private const uint FILE_DEVICE_DISK = 0x00000007;
		private const uint FILE_DEVICE_MASS_STORAGE = 0x0000002d;

		private const uint IOCTL_SCSI_BASE = FILE_DEVICE_CONTROLLER;
		private const uint IOCTL_DISK_BASE = FILE_DEVICE_DISK;
		private const uint IOCTL_STORAGE_BASE = FILE_DEVICE_MASS_STORAGE;

		private const uint METHOD_BUFFERED = 0;
		private const uint FILE_ANY_ACCESS = 0;
		private const uint FILE_READ_ACCESS = 0x0001;
		private const uint FILE_WRITE_ACCESS = 0x0002;

		[StructLayout(LayoutKind.Sequential)]
		public struct STORAGE_PROPERTY_QUERY
		{
			public uint PropertyId;
			public uint QueryType;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public byte[] AdditionalParameters;
		}

		public const uint StorageDeviceProperty = 0; // From STORAGE_PROPERTY_ID enum
		public const uint PropertyStandardQuery = 0; // From STORAGE_QUERY_TYPE enum

		[StructLayout(LayoutKind.Sequential)]
		public struct STORAGE_DEVICE_DESCRIPTOR
		{
			public uint Version;
			public uint Size;
			public byte DeviceType;
			public byte DeviceTypeModifier;

			[MarshalAs(UnmanagedType.U1)]
			public bool RemovableMedia;

			[MarshalAs(UnmanagedType.U1)]
			public bool CommandQueueing;

			public uint VendorIdOffset;
			public uint ProductIdOffset;
			public uint ProductRevisionOffset;
			public uint SerialNumberOffset;
			public STORAGE_BUS_TYPE BusType;
			public uint RawPropertiesLength;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)] // To be considered.
			public byte[] RawDeviceProperties;
		}

		// Storage bus type
		// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddstor/ne-ntddstor-storage_bus_type
		public enum STORAGE_BUS_TYPE : uint
		{
			BusTypeUnknown = 0x00,
			BusTypeScsi,
			BusTypeAtapi,
			BusTypeAta,
			BusType1394,
			BusTypeSsa,
			BusTypeFibre,
			BusTypeUsb,
			BusTypeRAID,
			BusTypeiScsi,
			BusTypeSas,
			BusTypeSata,
			BusTypeSd,
			BusTypeMmc,
			BusTypeVirtual,
			BusTypeFileBackedVirtual,
			BusTypeSpaces,
			BusTypeNvme,
			BusTypeSCM,
			BusTypeUfs,
			BusTypeMax,
			BusTypeMaxReserved = 0x7F
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ATAIdentifyDeviceQuery
		{
			public ATA_PASS_THROUGH_EX header;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public ushort[] data;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ATA_PASS_THROUGH_EX
		{
			public ushort Length;
			public ATA_FLAGS AtaFlags;
			public byte PathId;
			public byte TargetId;
			public byte Lun;
			public byte ReservedAsUchar;
			public uint DataTransferLength;
			public uint TimeOutValue;
			public uint ReservedAsUlong;
			public IntPtr DataBufferOffset;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] PreviousTaskFile;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] CurrentTaskFile;
		}

		[Flags]
		public enum ATA_FLAGS : ushort
		{
			ATA_FLAGS_DRDY_REQUIRED = 1,
			ATA_FLAGS_DATA_IN = 2,
			ATA_FLAGS_DATA_OUT = 4,
			ATA_FLAGS_48BIT_COMMAND = 8,
			ATA_FLAGS_USE_DMA = 16,
			ATA_FLAGS_NO_MULTIPLE = 32
		}

		// Move a pointer.
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetFilePointerEx(
			SafeFileHandle hFile,
			long liDistanceToMove,
			IntPtr lpNewFilePointer,
			uint dwMoveMethod);

		public const uint FILE_BEGIN = 0;
		public const uint FILE_CURRENT = 1;
		public const uint FILE_END = 2;

		// Read from a specified disk.
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadFile(
			SafeFileHandle hFile,
			[Out] byte[] lpBuffer,
			uint nNumberOfBytesToRead,
			ref uint lpNumberOfBytesRead,
			IntPtr lpOverlapped);
	}
}