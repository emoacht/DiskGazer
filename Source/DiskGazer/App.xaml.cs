﻿using System;
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
			if (!Debugger.IsAttached)
				AppDomain.CurrentDomain.UnhandledException += (sender, args) => RecordException(sender, args.ExceptionObject as Exception);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (!Debugger.IsAttached)
				this.DispatcherUnhandledException += (sender, args) => RecordException(sender, args.Exception);
		}

		#region Exception

		private static void RecordException(object sender, Exception exception)
		{
			const string fileName = "exception.log";

			var content = $"[Date: {DateTime.Now} Sender: {sender}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";

			Trace.WriteLine(content);

			var filePathAppData = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				Assembly.GetExecutingAssembly().GetName().Name,
				fileName);

			try
			{
				var folderPathAppData = Path.GetDirectoryName(filePathAppData);
				if (!string.IsNullOrEmpty(folderPathAppData) && !Directory.Exists(folderPathAppData))
					Directory.CreateDirectory(folderPathAppData);

				File.AppendAllText(filePathAppData, content);
			}
			catch (Exception ex)
			{
				Trace.WriteLine($"Failed to record exception to AppData.{Environment.NewLine}{Environment.NewLine}{ex}");
			}
		}

		#endregion
	}
}