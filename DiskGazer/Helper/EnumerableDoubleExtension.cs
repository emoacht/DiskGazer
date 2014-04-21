using System;
using System.Collections.Generic;
using System.Linq;

namespace DiskGazer.Helper
{
	public static class EnumerableDoubleExtension
	{
		/// <summary>
		/// Calculate median.
		/// </summary>
		/// <param name="source">Source Enumerable Double</param>
		/// <returns>Median</returns>
		public static double Median(this IEnumerable<double> source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var sourceArray = source.ToArray();

			if (!sourceArray.Any())
				throw new ArgumentNullException("source");

			var sortedList = sourceArray.OrderBy(x => x);

			var itemIndex = sortedList.Count() / 2;

			if (sortedList.Count() % 2 == 0)
			{
				// Even number of items.
				return (sortedList.ElementAt(itemIndex) + sortedList.ElementAt(itemIndex - 1)) / 2;
			}
			else
			{
				// Odd number of items.
				return sortedList.ElementAt(itemIndex);
			}
		}

		/// <summary>
		/// Calculate standard deviation.
		/// </summary>
		/// <param name="source">Source Enumerable Double</param>
		/// <returns>Standard deviation</returns>
		public static double StandardDeviation(this IEnumerable<double> source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var sourceArray = source.ToArray();

			if (!sourceArray.Any())
				throw new ArgumentNullException("source");

			var avg = sourceArray.Average();

			return Math.Sqrt(sourceArray.Average(x => Math.Pow(x - avg, 2)));
		}
	}
}
