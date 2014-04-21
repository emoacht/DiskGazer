using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace DiskGazer.Models
{
	/// <summary>
	/// Disk information
	/// </summary>
	[Serializable]
	public class DiskInfo : IComparable<DiskInfo>, ICloneable
	{
		/// <summary>
		/// Index number of Physical drive
		/// </summary>
		public int PhysicalDrive { get; set; }

		/// <summary>
		/// Model by WMI
		/// </summary>
		public string Model { get; set; } 
		
		/// <summary>
		/// Vendor ID by P/Invoke
		/// </summary>
		public string Vendor { get; set; }
		
		/// <summary>
		/// Product ID by P/Invoke
		/// </summary>
		public string Product { get; set; }

		/// <summary>
		/// Name
		/// </summary>
		public string Name
		{
			get
			{
				if (String.IsNullOrWhiteSpace(Product))
					return Model;

				if (String.IsNullOrWhiteSpace(Vendor) ||
					Vendor.Contains("VID:"))
					return Product;

				var name = String.Format("{0}{1}", Vendor, Product);
				if (Model.Contains(name) | // If Vendor ID field has former part of Product ID. To be considered.
					((Vendor.Length == 8) && Vendor.Contains(" "))) // If Vendor ID field has not only Vendor ID but also former part of Product ID. To be considered.
					return name;

				return String.Format("{0} {1}", Vendor, Product);
			}
		}

		/// <summary>
		/// Name and bus type
		/// </summary>
		public string NameBus
		{
			get
			{
				if (String.IsNullOrWhiteSpace(Product))
					return Model; // Model may include information on bus type.

				if (String.IsNullOrWhiteSpace(BusType))
					return Name;

				return String.Format("{0} ({1})", Name, BusType);
			}
		}

		/// <summary>
		/// Interface type by WMI
		/// </summary>
		public string InterfaceType { set; get; }

		/// <summary>
		/// Bus type by P/Invoke
		/// </summary>
		public string BusType { set; get; }
 
		/// <summary>
		/// Media type by WMI
		/// </summary>
		public string MediaType { set; get; }

		/// <summary>
		/// Whether removable disk by P/Invoke
		/// </summary>
		public bool IsRemovable { set; get; }

		/// <summary>
		/// Nominal media rotation rate by P/Invoke
		/// </summary>
		public int? NominalMediaRotationRate { get; set; }

		/// <summary>
		/// Description of nominal media rotation rate
		/// </summary>
		public string NominalMediaRotationRateDescription
		{
			get
			{
				switch (NominalMediaRotationRate)
				{
					case null:
						return "Not supported";

					case 0:
						return "Rate not reported";

					case 1:
						return "Non-rotating media";

					default:
						return String.Format("{0} RPM", NominalMediaRotationRate);
				}
			}
		}

		/// <summary>
		/// Size by WMI (Bytes)
		/// </summary>
		public long SizeWMI { get; set; }

		/// <summary>
		/// Size by P/Invoke (Bytes)
		/// </summary>
		public long SizePInvoke { get; set; }

		/// <summary>
		/// Size (MiB) (up to 2PiB)
		/// </summary>
		public int Size
		{
			get { return (int)Math.Truncate(Math.Max(SizeWMI, SizePInvoke) / Math.Pow(1024D, 2)); }
		}


		#region IComparable Member

		public int CompareTo(DiskInfo other)
		{
			if (other == null)
			{
				return 1;
			}

			return this.PhysicalDrive.CompareTo(other.PhysicalDrive);
		}

		#endregion


		#region ICloneable Member

		public DiskInfo Clone()
		{
			var binaryFormatter = new BinaryFormatter();

			using (var ms = new MemoryStream())
			{
				binaryFormatter.Serialize(ms, this);
				ms.Seek(0, SeekOrigin.Begin);

				return (DiskInfo)binaryFormatter.Deserialize(ms);
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion
	}
}
