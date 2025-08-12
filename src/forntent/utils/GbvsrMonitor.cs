using System;
using System.Runtime.InteropServices;

namespace YuyukikikoNoGameBox.utils
{
    public static class GbsvrMonitor
    {
        // 1. 定义与C++ DLL中完全匹配的数据结构
        // 使用StructLayout和Pack=1确保内存布局一致
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct GameDataSnapshot
        {
            public int Hp1P;
            public int Hp2P;
            public int PosLocal;
            public int PosPark;
            // 确保这里的字段顺序和类型与C++的GameDataSnapshot完全一样
        }

        // 2. 定义两种回调委托：一种用于状态文本，一种用于游戏数据
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StatusCallback([MarshalAs(UnmanagedType.LPStr)] string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GameDataCallback(ref GameDataSnapshot snapshot); // 使用ref高效传递结构体

        // 3. 为每个委托创建一个静态字段来保存实例，防止被垃圾回收
        private static StatusCallback _statusDelegate;
        private static GameDataCallback _dataDelegate;

        private const string DllName = "gbvsrMonitor.dll"; // 确保你的DLL名字是这个

        // 4. 更新 DllImport 签名以匹配新的C++导出函数
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void StartMonitoring(StatusCallback statusCallback, GameDataCallback dataCallback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void StopMonitoring();

        /// <summary>
        /// Starts the monitoring process.
        /// </summary>
        /// <param name="onStatusUpdate">Action to call on status updates.</param>
        /// <param name="onDataUpdate">Action to call on game data updates.</param>
        public static void Start(Action<string> onStatusUpdate, Action<GameDataSnapshot> onDataUpdate)
        {
            // 创建并保存两个委托的实例
            _statusDelegate = message => onStatusUpdate?.Invoke(message);
            _dataDelegate = (ref GameDataSnapshot snapshot) => onDataUpdate?.Invoke(snapshot);

            // 调用新的 StartMonitoring 函数
            StartMonitoring(_statusDelegate, _dataDelegate);
        }

        /// <summary>
        /// Stops the monitoring process.
        /// </summary>
        public static void Stop()
        {
            StopMonitoring();
            // 清理委托引用
            _statusDelegate = null;
            _dataDelegate = null;
        }
    }
}