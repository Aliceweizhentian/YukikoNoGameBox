using DGLabGameController.Core.Debug; // 引入 DebugHub
using DGLabGameController.Core.DGLabApi;
using DGLabGameController.Core.Config;
using DGLabGameController;
using YuyukikikoNoGameBox.commands;


namespace YuyukikikoNoGameBox.ViewModels
{
    public class PunishmentSettingsViewModel : ViewModelBase
    {
        // --- 属性 ---
        private double _mutible = 1.0;
        public double Mutible
        {
            get => _mutible;
            set { _mutible = value; OnPropertyChanged(); }
        }

        private int _limit = 200;
        public int Limit
        {
            get => _limit;
            set { _limit = value; OnPropertyChanged(); }
        }

        private int _baseStrength = 20;
        public int BaseStrength
        {
            get => _baseStrength;
            set { _baseStrength = value; OnPropertyChanged(); }
        }

        private double _punishmentDuration = 2.0;
        public double PunishmentDuration
        {
            get => _punishmentDuration;
            set { _punishmentDuration = value; OnPropertyChanged(); }
        }

        // --- 命令 ---
        public RelayCommand SetMutibleCommand { get; }
        public RelayCommand SetLimitCommand { get; }
        public RelayCommand SetBaseStrengthCommand { get; }
        public RelayCommand SetPunishTimerCommand { get; }

        // 构造函数
        public PunishmentSettingsViewModel()
        {
            // 初始化所有命令，将之前在主ViewModel中的逻辑直接复制过来
            SetMutibleCommand = new RelayCommand(ExecuteSetMutible);
            SetLimitCommand = new RelayCommand(ExecuteSetLimit);
            SetBaseStrengthCommand = new RelayCommand(ExecuteSetBaseStrength);
            SetPunishTimerCommand = new RelayCommand(ExecuteSetPunishTimer);
        }

        // --- 命令的执行方法 ---
        private void ExecuteSetMutible(object parameter)
        {
            // 使用 InputDialog 来获取用户输入
            new InputDialog("强度倍数", "输入一个数，角色减少的体力值乘以强度倍数即为受到的电击强度", this.Mutible.ToString(), "设定",
                (data) =>
                {
                    if (!string.IsNullOrWhiteSpace(data.InputText))
                    {
                        if (double.TryParse(data.InputText, out double value) && value >= 0)
                        {
                            this.Mutible = value; // 直接更新自身的属性
                        }
                        else DebugHub.Warning("参数设置", "请输入一个有效的正数。");
                    }
                    else DebugHub.Warning("参数设置", "请输入一个有效值。");
                    data.Close();
                }, "取消", (data) => data.Close()).ShowDialog();
        }

        private void ExecuteSetLimit(object parameter)
        {
            new InputDialog("强度上限", "输入一个整数，设置电击的最高强度", this.Limit.ToString(), "设定",
                (data) =>
                {
                    if (int.TryParse(data.InputText, out int value) && value >= 0)
                    {
                        this.Limit = value; // 更新自身的属性
                    }
                    else DebugHub.Warning("参数设置", "请输入一个有效的正整数。");
                    data.Close();
                }, "取消", (data) => data.Close()).ShowDialog();
        }

        private void ExecuteSetBaseStrength(object parameter)
        {
            new InputDialog("基础强度", "输入一个整数，设置惩罚的基础强度", this.BaseStrength.ToString(), "设定",
                (data) =>
                {
                    if (int.TryParse(data.InputText, out int value) && value >= 0)
                    {
                        this.BaseStrength = value; // 更新自身的属性
                    }
                    else DebugHub.Warning("参数设置", "请输入一个有效的正整数。");
                    data.Close();
                }, "取消", (data) => data.Close()).ShowDialog();
        }

        private void ExecuteSetPunishTimer(object parameter)
        {
            new InputDialog("惩罚时长", "输入一个数，设置惩罚的持续时间（秒）", this.PunishmentDuration.ToString(), "设定",
               (data) =>
               {
                   if (double.TryParse(data.InputText, out double value) && value >= 0)
                   {
                       this.PunishmentDuration = value; // 更新自身的属性
                   }
                   else DebugHub.Warning("参数设置", "请输入一个有效的正数。");
                   data.Close();
               }, "取消", (data) => data.Close()).ShowDialog();
        }
    }
}