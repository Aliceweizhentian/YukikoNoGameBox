using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using DGLabGameController.Core.Debug;

namespace YuyukikikoNoGameBox.utils
{
    public static class Injector
    {
        // 导入我们将要使用的 Windows API 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        // 定义所需的常量
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x04;

        /// <summary>
        /// 将指定的DLaL注入到目标进程中。
        /// </summary>
        /// <param name="processName">目标进程的名称 (例如 "BloodySpell")</param>
        /// <param name="dllPath">要注入的DLL的完整路径</param>
        /// <returns>成功返回true，失败返回false</returns>
        public static bool Inject(string processName, string dllPath)
        {
            // 1. 检查DLL文件是否存在
            if (!File.Exists(dllPath))
            {
                DebugHub.Log("yuki", $"[Injector] Error: DLL file not found at '{dllPath}'");
                return false;
            }

            // 2. 找到目标进程
            Process[] targetProcesses = Process.GetProcessesByName(processName);
            if (targetProcesses.Length == 0)
            {
                DebugHub.Log("yuki", $"[Injector] Error: Process '{processName}' not found.");
                return false;
            }
            Process targetProcess = targetProcesses[0];
            uint processId = (uint)targetProcess.Id;

            IntPtr hProcess = IntPtr.Zero;
            IntPtr pDllPath = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;

            try
            {
                // 3. 打开目标进程，获取完全控制句柄
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
                if (hProcess == IntPtr.Zero)
                {
                    DebugHub.Log("yuki", "[Injector] Error: OpenProcess failed. Try running as Administrator.");
                    return false;
                }

                // 4. 在目标进程中为DLL路径分配内存
                byte[] dllPathBytes = Encoding.Unicode.GetBytes(dllPath + "\0");
                pDllPath = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPathBytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (pDllPath == IntPtr.Zero)
                {
                    DebugHub.Log("yuki", "[Injector] Error: VirtualAllocEx failed.");
                    return false;
                }

                // 5. 将DLL路径写入刚刚分配的内存中
                if (!WriteProcessMemory(hProcess, pDllPath, dllPathBytes, (uint)dllPathBytes.Length, out _))
                {
                    DebugHub.Log("yuki", "[Injector] Error: WriteProcessMemory failed.");
                    return false;
                }

                // 6. 获取 LoadLibraryW 函数的地址 
                IntPtr pLoadLibraryW = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
                if (pLoadLibraryW == IntPtr.Zero)
                {
                    DebugHub.Log("yuki", "[Injector] Error: GetProcAddress for LoadLibraryW failed.");
                    return false;
                }

                // 7. 在目标进程中创建一个远程线程，让它去执行 LoadLibraryW，参数就是我们写入的DLL路径
                hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibraryW, pDllPath, 0, out _);
                if (hThread == IntPtr.Zero)
                {
                    DebugHub.Log("yuki", "[Injector] Error: CreateRemoteThread failed.");
                    return false;
                }

            }
            catch (Exception ex)
            {
                DebugHub.Log("yuki", $"[Injector] An exception occurred: {ex.Message}");
                return false;
            }
            finally
            {
                // 8. 清理工作：释放内存和句柄
                if (pDllPath != IntPtr.Zero)
                {
                    VirtualFreeEx(hProcess, pDllPath, 0, MEM_RELEASE);
                }
                if (hThread != IntPtr.Zero)
                {
                    CloseHandle(hThread);
                }
                if (hProcess != IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                }
            }

            DebugHub.Log("yuki","[Injector] Injection successful!");
            return true;
        }
    }
}