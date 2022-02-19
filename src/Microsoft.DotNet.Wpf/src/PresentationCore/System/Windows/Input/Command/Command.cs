namespace System.Windows.Input
{
    public class Command : ICommand
    {
        private readonly Func<object, bool> _canExecute;

        private readonly Action<object> _execute;

        private readonly LowEventManager _weakEventManager = new LowEventManager();

        /// <summary>
        /// Occurs when the target of the Command should reevaluate whether or not the Command
        ///     can be executed.
        /// </summary>
        /// <remarks>
        /// To be added.
        /// </remarks>
#pragma warning disable CS8612
        public event EventHandler CanExecuteChanged
        {
            add
            {
                _weakEventManager.AddEventHandler(value, "CanExecuteChanged");
            }
            remove
            {
                _weakEventManager.RemoveEventHandler(value, "CanExecuteChanged");
            }
        }
#pragma warning restore CS8612

#pragma warning disable CS8618
        public Command(Action<object> execute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
        }
#pragma warning restore CS8618

        public Command(Action execute)
            : this((Action<object>)delegate
            {
                execute();
            })
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
        }

        public Command(Action<object> execute, Func<object, bool> canExecute)
            : this(execute)
        {
            if (canExecute == null)
            {
                throw new ArgumentNullException("canExecute");
            }

            _canExecute = canExecute;
        }

        public Command(Action execute, Func<bool> canExecute)
            : this(delegate
            {
                execute();
            }, (object o) => canExecute())
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            if (canExecute == null)
            {
                throw new ArgumentNullException("canExecute");
            }
        }

        /// <summary>
        /// Returns a System.Boolean indicating if the Command can be exectued with the given
        ///     parameter.
        /// </summary>
        /// <param name="parameter">
        /// An System.Object used as parameter to determine if the Command can be executed.
        /// </param>
        /// <returns> 
        /// true if the Command can be executed, false otherwise.
        /// </returns>
        /// <remarks>
        /// If no canExecute parameter was passed to the Command constructor, this method
        /// always returns true.
        /// If the Command was created with non-generic execute parameter, the parameter
        /// of this method is ignored.
        /// </remarks>
#pragma warning disable CS8767
        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
            {
                return _canExecute(parameter);
            }

            return true;
        }
#pragma warning restore CS9867

        /// <summary>
        /// Invokes the execute Action
        /// </summary>
        /// <param name="parameter">
        ///  An System.Object used as parameter for the execute Action.
        /// </param>
        /// <remarks>
        /// If the Command was created with non-generic execute parameter, the parameter
        //     of this method is ignored.
        /// </remarks>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
        /// <summary>
        /// Send a System.Windows.Input.ICommand.CanExecuteChanged
        /// </summary>
        /// <remarks>
        ///     To be added.
        /// </remarks>
        public void ChangeCanExecute()
        {
            _weakEventManager.HandleEvent(this, EventArgs.Empty, "CanExecuteChanged");
        }
    }
}
