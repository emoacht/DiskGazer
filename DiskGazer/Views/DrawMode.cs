
namespace DiskGazer.Views
{
	/// <summary>
	/// Draw mode of chart
	/// </summary>
	internal enum DrawMode
	{
		/// <summary>
		/// Clear current chart line.
		/// </summary>
		Clear = 0,

		/// <summary>
		/// Clear all chart lines.
		/// </summary>
		ClearCompletely,

		/// <summary>
		/// Draw new chart line.
		/// </summary>
		DrawNewChart,

		/// <summary>
		/// Pin current chart line.
		/// </summary>
		PinCurrentChart,

		/// <summary>
		/// Refresh pinned chart lines.
		/// </summary>
		RefreshPinnedChart,
	}
}