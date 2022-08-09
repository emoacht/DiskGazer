using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DiskGazer.Common
{
	public class DelegateCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		#region Constructor

		public DelegateCommand(Action execute) : this(execute, () => true)
		{ }

		public DelegateCommand(Action execute, Func<bool> canExecute)
		{
			this._execute = execute ?? throw new ArgumentNullException(nameof(execute));
			this._canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
		}

		#endregion

		#region Execute

		public void Execute() => _execute();
		void ICommand.Execute(object parameter) => Execute();

		#endregion

		#region CanExecute

		public bool CanExecute() => _canExecute();
		bool ICommand.CanExecute(object parameter) => CanExecute();

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		#endregion
	}
}