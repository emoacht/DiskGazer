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
		/// Calculates median.
		/// </summary>
		/// <param name="source">Source sequence of double</param>
		/// <returns>Median</returns>
		public static double Median(this IEnumerable<double> source)
		{
			var sourceArray = source as double[] ?? source?.ToArray();
			if (sourceArray is not { Length: > 0 })
				throw new ArgumentNullException(nameof(source));

			var orderedArray = sourceArray.OrderBy(x => x).ToArray();

			var medianIndex = orderedArray.Length / 2;

			return (orderedArray.Length % 2 == 0) // 0 or 1
				? (orderedArray[medianIndex] + orderedArray[medianIndex - 1]) / 2D // Even number of elements
				: orderedArray[medianIndex]; // Odd number of elements
		}

		/// <summary>
		/// Calculates standard deviation.
		/// </summary>
		/// <param name="source">Source sequence of double</param>
		/// <returns>Standard deviation</returns>
		public static double StandardDeviation(this IEnumerable<double> source)
		{
			var sourceArray = source as double[] ?? source?.ToArray();
			if (sourceArray is not { Length: > 0 })
				throw new ArgumentNullException(nameof(source));

			var averageValue = sourceArray.Average();

			return Math.Sqrt(sourceArray.Average(x => Math.Pow(x - averageValue, 2)));
		}
	}
}