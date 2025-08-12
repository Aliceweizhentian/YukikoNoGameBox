using DGLabGameController.Core.Debug;
using DGLabGameController;
using DGLabGameController.Core.DGLabApi;
using System.Windows;
using System.Windows.Threading;
using YuyukikikoNoGameBox.utils;
using YuyukikikoNoGameBox.commands;



namespace YuyukikikoNoGameBox.ViewModels
{
    public class GBVSRViewModel : ViewModelBase, IGameViewModel
    {
        // --- 定时器相关成员 ---
        private readonly DispatcherTimer _punishmentTimer;
        private int _currentPunishmentIntensity = 0;

        // 1. 外部依赖：通过构造函数注入的共享惩罚设置
        private readonly PunishmentSettingsViewModel _settings;

        // 2. 实现 IGameViewModel 接口要求的属性
        public string GameName => "GBVSR";

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        // 3. GBVSR 专属的UI状态和命令
        private int _comboPunish = 1;
        public int ComboPunish
        {
            get => _comboPunish;
            set { _comboPunish = value; OnPropertyChanged(); }
        }
        private bool _isAllPunishMode;
        public bool IsAllPunishMode // 用于绑定UI上的“敌我不分”开关
        {
            get => _isAllPunishMode;
            set { _isAllPunishMode = value; OnPropertyChanged(); }
        }

        private bool _isRankMode;
        public bool IsRankMode // 用于绑定UI上的“敌我不分”开关
        {
            get => _isRankMode;
            set { _isRankMode = value; OnPropertyChanged(); }
        }

        private bool _isParkMode;
        public bool IsParkMode // 用于绑定UI上的“敌我不分”开关
        {
            get => _isParkMode;
            set { _isParkMode = value; OnPropertyChanged(); }
        }

        private bool _isLocalMode;
        public bool IsLocalMode // 用于绑定UI上的“敌我不分”开关
        {
            get => _isLocalMode;
            set { _isLocalMode = value; OnPropertyChanged(); }
        }

        private string _currentStatusText;
        public string CurrentStatusText
        {
            get => _currentStatusText;
            set { _currentStatusText = value; OnPropertyChanged(); } // 当它改变时通知UI
        }


        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand SetComboPunishCommand { get; }

        // 4. 用于处理来自DLL的数据
        private GbsvrMonitor.GameDataSnapshot _lastSnapshot;

        // 构造函数
        public GBVSRViewModel(PunishmentSettingsViewModel settings)
        {
            _settings = settings;
            settings.Mutible = 0.01;

            _punishmentTimer = new DispatcherTimer();

            // 初始化命令
            StartCommand = new RelayCommand(ExecuteStart, CanExecuteStart);
            StopCommand = new RelayCommand(ExecuteStop, CanExecuteStop);
            SetComboPunishCommand = new RelayCommand(ExecuteSetComboPunish, CanExecuteSetComboPunish);
        }

        private bool CanExecuteStart(object p) => !IsRunning;
        private bool CanExecuteStop(object p) => IsRunning;
        private bool CanExecuteSetComboPunish(object p) => IsRunning;

        private void ExecuteStart(object parameter)
        {
            DebugHub.Log(GameName, "Starting monitoring...");

            // --- 配置定时器 ---
            _punishmentTimer.Tick += PunishmentTimer_Tick; // 订阅Tick事件
            _punishmentTimer.Interval = TimeSpan.FromSeconds(_settings.PunishmentDuration); // 从共享设置获取时长

            // 初始化lastSnapshot，避免第一次回调时出现空引用或错误比较
            _lastSnapshot = new GbsvrMonitor.GameDataSnapshot { Hp1P = -1, Hp2P = -1, PosLocal = -1, PosPark = -1 };

            // 调用C#封装类，传入两个回调方法的引用
            GbsvrMonitor.Start(OnStatusUpdate, OnDataUpdate);
        }

        private void ExecuteStop(object parameter)
        {
            DebugHub.Log(GameName, "Stopping monitoring...");
            _punishmentTimer.Stop(); // 停止监控时也要确保定时器停止
            _punishmentTimer.Tick -= PunishmentTimer_Tick;
            _ = DGLab.SetStrength.Set((int)_settings.BaseStrength); // 恢复基础强度
            GbsvrMonitor.Stop();
        }

        private void ExecuteSetComboPunish(object parameter)
        {
            // 使用 InputDialog 来获取用户输入
            new InputDialog("连段惩罚", "输入一个数，在连续受到惩罚时惩罚值会用这个数进行累加", this.ComboPunish.ToString(), "设定",
                (data) =>
                {
                    if (!string.IsNullOrWhiteSpace(data.InputText))
                    {
                        if (int.TryParse(data.InputText, out int value) && value >= 0)
                        {
                            this.ComboPunish = value; // 直接更新自身的属性
                        }
                        else DebugHub.Warning("参数设置", "请输入一个有效的数。");
                    }
                    else DebugHub.Warning("参数设置", "请输入一个有效值。");
                    data.Close();
                }, "取消", (data) => data.Close()).ShowDialog();
        }

        // 回调方法1: 处理来自DLL的状态/错误信息
        private void OnStatusUpdate(string message)
        {
            // 使用Dispatcher来确保UI更新在主线程上执行
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugHub.Log(GameName, $"[STATUS]: {message}");

                // 如果消息包含"Error"或在启动后很快收到非成功消息，说明启动失败
                if (message.Contains("Error") || message.Contains("failed"))
                {
                    IsRunning = false;
                }
                else if (message.Contains("started successfully"))
                {
                    IsRunning = true;
                }
                else if (message.Contains("stopped")) // 假设我们未来会添加停止回调
                {
                    IsRunning = false;
                    _punishmentTimer.Stop(); // 如果是从外部停止，也确保定时器停止
                }
                UpdateCanExecute();
            });
        }

        // 回调方法2: 处理来自DLL的实时游戏数据，这是所有前端逻辑的核心！
        private void OnDataUpdate(GbsvrMonitor.GameDataSnapshot snapshot)
        {
            int damage = 0;

            // 根据UI上的开关，决定执行哪套逻辑
            if (IsAllPunishMode) // “敌我不分”模式
            {
                // 检查1P血量变化
                if (snapshot.Hp1P != -1 && _lastSnapshot.Hp1P != -1 && snapshot.Hp1P < _lastSnapshot.Hp1P)
                {
                    damage = _lastSnapshot.Hp1P - snapshot.Hp1P;
                    TriggerPunishment(damage);
                }
                // 检查2P血量变化
                if (snapshot.Hp2P != -1 && _lastSnapshot.Hp2P != -1 && snapshot.Hp2P < _lastSnapshot.Hp2P)
                {
                    damage = _lastSnapshot.Hp2P - snapshot.Hp2P;
                    TriggerPunishment(damage);
                }
            }
            if (IsRankMode) // “排位对战”模式
            {
                // 检查1P血量变化
                if (snapshot.Hp1P != -1 && _lastSnapshot.Hp1P != -1 && snapshot.Hp1P < _lastSnapshot.Hp1P)
                {
                    damage = _lastSnapshot.Hp1P - snapshot.Hp1P;
                    TriggerPunishment(damage);
                }

            }
            if (IsLocalMode) // “本地对战”模式
            {
                if (snapshot.PosLocal == 0)
                {
                    // 检查1P血量变化
                    if (snapshot.Hp1P != -1 && _lastSnapshot.Hp1P != -1 && snapshot.Hp1P < _lastSnapshot.Hp1P)
                    {
                        damage = _lastSnapshot.Hp1P - snapshot.Hp1P;
                        TriggerPunishment(damage);
                    }
                }
                else if (snapshot.PosLocal == 1)
                {
                    // 检查2P血量变化
                    if (snapshot.Hp2P != -1 && _lastSnapshot.Hp2P != -1 && snapshot.Hp2P < _lastSnapshot.Hp2P)
                    {
                        damage = _lastSnapshot.Hp2P - snapshot.Hp2P;
                        TriggerPunishment(damage);
                    }
                }
            }
            if (IsParkMode) // “大厅对战”模式
            {
                if (snapshot.PosPark == 1)
                {
                    // 检查1P血量变化
                    if (snapshot.Hp1P != -1 && _lastSnapshot.Hp1P != -1 && snapshot.Hp1P < _lastSnapshot.Hp1P)
                    {
                        damage = _lastSnapshot.Hp1P - snapshot.Hp1P;
                        TriggerPunishment(damage);
                    }
                }
                else if (snapshot.PosLocal == 2)
                {
                    // 检查2P血量变化
                    if (snapshot.Hp2P != -1 && _lastSnapshot.Hp2P != -1 && snapshot.Hp2P < _lastSnapshot.Hp2P)
                    {
                        damage = _lastSnapshot.Hp2P - snapshot.Hp2P;
                        TriggerPunishment(damage);
                    }
                }
            }
            else // 默认模式
            {
                // 只关心1P
                if (snapshot.Hp1P != -1 && _lastSnapshot.Hp1P != -1 && snapshot.Hp1P < _lastSnapshot.Hp1P)
                {
                    damage = _lastSnapshot.Hp1P - snapshot.Hp1P;
                    TriggerPunishment(damage);
                }
            }

            // 保存当前快照，用于下一次比较
            _lastSnapshot = snapshot;
        }

        private void TriggerPunishment(int damage)
        {
            // 计算惩罚强度
            int shockIntensity = (int)((damage * _settings.Mutible) + _settings.BaseStrength);
            shockIntensity = System.Math.Min(shockIntensity, _settings.Limit);

            // --- 触发惩罚时，使用定时器 ---
            // 使用Dispatcher Invoke确保所有UI和定时器操作都在UI线程上
            Application.Current.Dispatcher.Invoke(() =>
            {
                _punishmentTimer.Stop(); // 总是先停止，以重置计时

                if (shockIntensity > _currentPunishmentIntensity)
                {
                    _ = DGLab.SetStrength.Set(shockIntensity);
                    _currentPunishmentIntensity = shockIntensity;

                    CurrentStatusText = $"玩家 受到 {damage} 点伤害，输出 {shockIntensity:F0} 强度";
                }
                else
                {
                    int comboShockIntensity = _currentPunishmentIntensity + ComboPunish;
                    comboShockIntensity = System.Math.Min(comboShockIntensity, _settings.Limit);
                    _ = DGLab.SetStrength.Set(comboShockIntensity);
                    _currentPunishmentIntensity = comboShockIntensity;

                    CurrentStatusText = $"玩家 受到 {damage} 点伤害，输出 {shockIntensity:F0} 强度";
                }
                // 重新设置间隔
                _punishmentTimer.Interval = TimeSpan.FromSeconds(_settings.PunishmentDuration);
                _punishmentTimer.Start(); 
            });

        }

        // --- 5. 定时器到点后执行的方法 ---
        private void PunishmentTimer_Tick(object sender, System.EventArgs e)
        {
            // 停止计时器，使其成为一次性触发
            _punishmentTimer.Stop();

            // 恢复到基础强度
            _ = DGLab.SetStrength.Set((int)_settings.BaseStrength);

            // 重置当前惩罚记录
            _currentPunishmentIntensity = 0;
            
        }
        
        private void UpdateCanExecute()
        {
            // 通知UI更新按钮的可用状态
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
    }
}