using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RetailPoC
{
    public class ItemClickedCommand : ICommand
    {
        Action action;
        Func<bool> canExecute;

        public ItemClickedCommand(Action action) : this(action, () => true) { }
        public ItemClickedCommand(Action action, Func<bool> canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return canExecute();
        }

        public void Execute(object parameter)
        {
            action();
        }
    }
}
