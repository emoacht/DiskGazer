using DiskGazer.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace DiskGazer
{
	public partial class App : Application
	{
		public App()
		{
#if (!DEBUG)
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => ReportException(sender, args.ExceptionObject as Exception);
#endif
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

#if (!DEBUG)
			this.DispatcherUnhandledException += (sender, args) => ReportException(sender, args.Exception);
#endif

			this.MainWindow = new MainWindow();
			this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
		}


		#region Exception

		private static void ReportException(object sender, Exception exception)
		{
			var filePath = Path.Combine(
				Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
				"exception.log");

			try
			{
				var content = String.Format(@"[Date: {0} Sender: {1}]", DateTime.Now, sender) + Environment.NewLine
					+ exception + Environment.NewLine + Environment.NewLine;

				Debug.WriteLine(content);

				var folderPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(folderPath))
					Directory.CreateDirectory(folderPath);

				File.AppendAllText(filePath, content);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to report exception. {0}", ex);
			}
		}

		#endregion
	}
}