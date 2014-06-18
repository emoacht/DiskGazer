using System;
using System.Collections.Generic;
using System.Linq;

namespace DiskGazer.Helper
{
	public static class EnumerableExtension
	{
		/// <summary>
		/// Calculate median.
		/// </summary>
		/// <param name="source">Source Enumerable Double</param>
		/// <returns>Median</returns>
		public static double Median(this IEnumerable<double> source)
		{
			if ((source == null) || !source.Any())
				throw new ArgumentNullException("source");

			var sourceArray = source.OrderBy(x => x).ToArray();

			var medianIndex = sourceArray.Length / 2;

			return (sourceArray.Length % 2 == 0) // 0 or 1
				? (sourceArray[medianIndex] + sourceArray[medianIndex - 1]) / 2D // Even number of elements
				: sourceArray[medianIndex]; // Odd number of elements
		}

		/// <summary>
		/// Calculate standard deviation.
		/// </summary>
		/// <param name="source">Source Enumerable Double</param>
		/// <returns>Standard deviation</returns>
		public static double StandardDeviation(this IEnumerable<double> source)
		{
			if ((source == null) || !source.Any())
				throw new ArgumentNullException("source");

			var averageValue = source.Average();

			return Math.Sqrt(source.Average(x => Math.Pow(x - averageValue, 2)));
		}
	}
}
