using System;
using System.Collections.ObjectModel; 
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using DGLabGameController;
using DGLabGameController.Core.Debug;
using System.Windows;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Threading;


namespace YuyukikikoNoGameBox.ViewModels
{

    public class YuyukikikoNoGameBoxPageViewModel : ViewModelBase
    {
        // 1. 共享参数设置，它将被传递给所有子ViewModel
        public PunishmentSettingsViewModel PunishmentSettings { get; }

        // 2. 游戏选项列表，现在的类型是 IGameViewModel
        public ObservableCollection<IGameViewModel> GameViewModels { get; set; }

        // 3. 当前选中的游戏ViewModel，UI将通过它来动态显示内容
        private IGameViewModel _selectedGameViewModel;
        public IGameViewModel SelectedGameViewModel
        {
            get => _selectedGameViewModel;
            set
            {
                if (_selectedGameViewModel != value)
                {
                    _selectedGameViewModel = value;
                    OnPropertyChanged(); // 通知UI更新
                }
            }
        }

        public YuyukikikoNoGameBoxPageViewModel(string moduleFolderPath)
        {
            // 创建共享的惩罚参数设置ViewModel实例
            PunishmentSettings = new PunishmentSettingsViewModel();

            // 为每个游戏创建其专属的ViewModel实例，并将共享设置传递进去
            // 注意: 这里我们传入了 moduleFolderPath，因为像嗜血印那样的注入逻辑需要这个路径
            var gbvsrVM = new GBVSRViewModel(PunishmentSettings);
            // var guiltyGearVM = new GuiltyGearViewModel(PunishmentSettings); // 待创建
            // var bloodySpellVM = new BloodySpellViewModel(PunishmentSettings, moduleFolderPath); // 待创建

            // 初始化游戏选项列表
            GameViewModels = new ObservableCollection<IGameViewModel>
            {
                gbvsrVM,
            };

            // 设置默认选中的游戏
            SelectedGameViewModel = GameViewModels[0];
        }
    }


}
