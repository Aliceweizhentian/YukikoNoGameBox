using System;
using System.Runtime.InteropServices;

namespace YuyukikikoNoGameBox.utils
{
    internal static class HookDllWrapper
    {
        // 知识点 1: DllImport 特性
        // [DllImport("HookDLL.dll", ...)] 告诉 .NET：接下来声明的这个函数不是在 C# 里实现的，
        // 而是在一个名为 "HookDLL.dll" 的外部库中。
        // EntryPoint: C++ DLL 中函数的实际名称。
        // CallingConvention: 指定函数调用约定，必须与 C++ 侧匹配。__cdecl 是 C/C++ 的标准约定。
        // CharSet: 指定字符串编码，C++中的 wchar_t* 对应 Unicode。
        [DllImport("HookDLL.dll", EntryPoint = "InitializeHook", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I1)] // 将 C++ 的 bool 封送为 C# 的 bool
        public static extern bool InitializeHook(
            [MarshalAs(UnmanagedType.LPWStr)] string moduleName,
            [MarshalAs(UnmanagedType.LPStr)] string signature,
            CppCallback callback,
            UIntPtr adressOffset);

        [DllImport("HookDLL.dll", EntryPoint = "ShutdownHook", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ShutdownHook();

        // 知识点 2: 委托 (Delegate)
        // C++ 中的函数指针 (GenericCallback) 在 C# 中没有直接对应的类型。
        // 我们使用“委托”来代替。委托可以看作是一个能够持有函数引用的“安全”的函数指针。
        // 它的签名必须与 C++ 回调函数的签名严格对应。void(X64RegisterContext* context) -> void(IntPtr contextPtr)
        // 我们用 IntPtr 来接收 C++ 的指针，因为它就是一个代表内存地址的整数。
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CppCallback(IntPtr contextPtr);


        // 知识点 3: 结构体封送 (Struct Marshaling)
        // 为了能在 C# 中读取 C++ 传来的 X64RegisterContext* 指针内容，
        // 我们必须在 C# 中定义一个一模一样的结构体。
        // [StructLayout(LayoutKind.Sequential, ...)]: 保证 C# 结构体的内存布局和 C++ 完全一致。
        // Align = 16: 这非常重要！因为你的 C++ 结构体用了 __declspec(align(16))，这里必须匹配。
        [StructLayout(LayoutKind.Sequential)]
        public struct X64RegisterContext
        {
            // C++: uint64_t Xmm0[2];  C#: 我们把它拆成两个 ulong 字段
            public ulong Xmm0_0;
            public ulong Xmm0_1;
            public ulong Xmm1_0;
            public ulong Xmm1_1;
            public ulong Xmm2_0;
            public ulong Xmm2_1;
            public ulong Xmm3_0;
            public ulong Xmm3_1;
            public ulong Xmm4_0;
            public ulong Xmm4_1;
            public ulong Xmm5_0;
            public ulong Xmm5_1;

            public ulong R15;
            public ulong R14;
            public ulong R13;
            public ulong R12;
            public ulong R11;
            public ulong R10;
            public ulong R9;
            public ulong R8;
            public ulong Rdi;
            public ulong Rsi;
            public ulong Rbp;
            public ulong Rdx;
            public ulong Rcx;
            public ulong Rbx;
            public ulong Rax;
        }
        //----------dll工具----------------

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDllDirectory(string lpPathName);
        //设置dll搜索路径
        public static bool SetupDllDirectory(string targetDllDirectory)
        {
            try
            {
                bool success = SetDllDirectory(targetDllDirectory);
                if (!success)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;

            }
        }
    }
}