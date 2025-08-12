using System.ComponentModel;
using YuyukikikoNoGameBox.commands;
using System.Runtime.CompilerServices;

namespace YuyukikikoNoGameBox.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    // 定义所有游戏ViewModel必须具备的基本功能
    public interface IGameViewModel : INotifyPropertyChanged
    {
        string GameName { get; }
        RelayCommand StartCommand { get; }
        RelayCommand StopCommand { get; }

        bool IsRunning { get; set; }
    }
    // 一个用于占位的简单ViewModel，实现了IGameViewModel接口
    // 这样它就可以被放入ObservableCollection<IGameViewModel>中
    public class PlaceholderViewModel : ViewModelBase, IGameViewModel
    {
        public string GameName => "请选择一个游戏";
        public bool IsRunning { get; set; }

        // 对于占位符，命令什么都不做，并且总是不可用
        public RelayCommand StartCommand => new RelayCommand(p => { }, p => false);
        public RelayCommand StopCommand => new RelayCommand(p => { }, p => false);
    }
}
