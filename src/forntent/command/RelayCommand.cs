using System;
using System.Windows.Input;

namespace YuyukikikoNoGameBox.commands
{
    public class RelayCommand : ICommand
    {
        // _execute: 存储需要执行的方法 (一个委托)
        // Action<object> 是一个内置的委托类型，表示一个接受 object 参数且无返回值的方法。
        private readonly Action<object> _execute;

        // _canExecute: 存储一个方法，用于判断命令是否可以执行 (一个委托)
        // Predicate<object> 是一个内置的委托类型，表示一个接受 object 参数并返回 bool 值的方法。
        private readonly Predicate<object> _canExecute;

        // 构造函数
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // 当 `CanExecute` 的结果可能改变时，WPF 会检查这个事件
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        // 判断命令是否可以执行
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // 执行命令
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}