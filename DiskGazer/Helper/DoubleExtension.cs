using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Helper
{
	/// <summary>
	/// Extension method for Double
	/// </summary>
	public static class DoubleExtension
	{
		/// <summary>
		/// Get the number of scale (decimals) of double.
		/// </summary>
		/// <param name="source">Source double</param>
		/// <returns>Number of scale</returns>
		public static int Scale(this double source)
		{
			if (double.IsNaN(source) || double.IsInfinity(source))
				throw new NotSupportedException("Value is not a number or evaluates to infinity.");

			const int max = 15; // Number of significant figures in double

			for (int i = 0; i < max; i++)
			{
				var num = Math.Abs(source) * Math.Pow(10, i);
				if (!(num - Math.Truncate(num) > 0D))
					return i;
			}

			return max;
		}
	}
}