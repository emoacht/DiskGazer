using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Helper
{
	/// <summary>
	/// Extension method for <see cref="Double"/>
	/// </summary>
	public static class DoubleExtension
	{
		/// <summary>
		/// Gets the number of scale (decimals) of double.
		/// </summary>
		/// <param name="value">Double</param>
		/// <returns>The number of scale</returns>
		public static int Scale(this double value)
		{
			if (double.IsNaN(value) || double.IsInfinity(value))
				throw new ArgumentException("The value is not a number or evaluates to infinity.", nameof(value));

			const int max = 15; // The number of significant figures in double

			for (int i = 0; i < max; i++)
			{
				var num = Math.Abs(value) * Math.Pow(10, i);
				if (!(num - Math.Truncate(num) > 0D))
					return i;
			}

			return max;
		}
	}
}