using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiskGazer.Helper
{
	/// <summary>
	/// Extension method for <see cref="Enumerable"/>
	/// </summary>
	public static class EnumerableExtension
	{
		/// <summary>
		/// Calculate median.
		/// </summary>
		/// <param name="source">Source sequence of double</param>
		/// <returns>Median</returns>
		public static double Median(this IEnumerable<double> source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var buff = source as double[] ?? source.ToArray();
			if (!buff.Any())
				throw new ArgumentNullException("source");

			var sourceArray = buff.OrderBy(x => x).ToArray();

			var medianIndex = sourceArray.Length / 2;

			return (sourceArray.Length % 2 == 0) // 0 or 1
				? (sourceArray[medianIndex] + sourceArray[medianIndex - 1]) / 2D // Even number of elements
				: sourceArray[medianIndex]; // Odd number of elements
		}

		/// <summary>
		/// Calculate standard deviation.
		/// </summary>
		/// <param name="source">Source sequence of double</param>
		/// <returns>Standard deviation</returns>
		public static double StandardDeviation(this IEnumerable<double> source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var buff = source as double[] ?? source.ToArray();
			if (!buff.Any())
				throw new ArgumentNullException("source");

			var averageValue = buff.Average();

			return Math.Sqrt(buff.Average(x => Math.Pow(x - averageValue, 2)));
		}
	}
}