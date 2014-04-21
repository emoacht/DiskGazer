using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Models
{
	/// <summary>
	/// Method for reading
	/// </summary>
	public enum ReadMethod
	{
		/// <summary>
		/// By native method
		/// </summary>
		Native = 0,

		/// <summary>
		/// By P/Invoke method
		/// </summary>
		P_Invoke,
	}
}
