#define GBVSR_EXPORTS
#include "gbvsr.h"
#include <windows.h>
#include <tlhelp32.h>
#include <string>
#include <vector>
#include <thread>
#include <atomic>
#include <mutex>

// --- 辅助函数声明 ---
DWORD GetProcessIdByName(const std::wstring &processName);
uintptr_t GetModuleBaseAddress(DWORD processId, const std::wstring &moduleName);
uintptr_t FindDynamicAddress(HANDLE hProcess, uintptr_t baseAddr, const std::vector<uintptr_t> &offsets);

// --- 全局状态变量 ---
struct SharedGameData
{
    int hp_1p = -1;
    int hp_2p = -1;
    int pos_local = -1;
    int pos_park = -1;
};

SharedGameData g_gameData;
std::mutex g_dataMutex;
std::vector<std::thread> g_threads;
std::atomic<bool> g_isRunning = false;

StatusCallback g_statusCallback = nullptr;
GameDataCallback g_dataCallback = nullptr;

GameMode g_currentMode;   
HANDLE g_hProcess = NULL; 

// --- 生产者线程任务 ---
void MonitorTask(HANDLE hProcess, uintptr_t baseAddress, const std::string &validationRule, const std::vector<uintptr_t> &offsets, int &targetValue)
{
    while (g_isRunning)
    {

        uintptr_t address = FindDynamicAddress(hProcess, baseAddress, offsets);
        int latestValue = -1;
        if (address != 0)
        {
            int tempValue = 0;
            SIZE_T bytesRead;
            if (ReadProcessMemory(hProcess, (LPCVOID)address, &tempValue, sizeof(int), &bytesRead) && bytesRead == sizeof(int))
            {
                bool isValid = false;
                if (validationRule == "HP")
                {
                    if (tempValue >= 0 && tempValue <= 20000)
                        isValid = true;
                }
                else if (validationRule == "LOCAL_POS")
                {
                    if (tempValue == 0 || tempValue == 1)
                        isValid = true;
                }
                else if (validationRule == "PARK_POS")
                {
                    if (tempValue == 1 || tempValue == 2)
                        isValid = true;
                }
                if (isValid)
                    latestValue = tempValue;
            }
        }
        {
            std::lock_guard<std::mutex> lock(g_dataMutex);
            targetValue = latestValue;
        }
        Sleep(100);
    }
}

// --- 调度器/消费者线程 ---
void DispatcherThread()
{
    // 不再需要 lastReportedHp 或 lastStatusMessage
    // 只需要一个变量来存储上一次发送的数据快照，以检测变化
    GameDataSnapshot lastSentSnapshot = {-2, -2, -2, -2}; // 初始化为一个不可能的值

    while (g_isRunning)
    {
        GameDataSnapshot currentSnapshot;
        {
            // 从全局数据复制到快照结构体
            std::lock_guard<std::mutex> lock(g_dataMutex);
            currentSnapshot.hp_1p = g_gameData.hp_1p;
            currentSnapshot.hp_2p = g_gameData.hp_2p;
            currentSnapshot.pos_local = g_gameData.pos_local;
            currentSnapshot.pos_park = g_gameData.pos_park;
        }

        // 比较当前快照和上一次发送的快照
        // memcmp 是比较内存块最快的方式
        if (memcmp(&currentSnapshot, &lastSentSnapshot, sizeof(GameDataSnapshot)) != 0)
        {
            // 只要有任何数据变化，就通过回调发送整个结构体
            if (g_dataCallback)
            {
                g_dataCallback(&currentSnapshot);
            }

            // 更新“上一次已发送”的快照
            lastSentSnapshot = currentSnapshot;
        }

        Sleep(50); // 可以稍微降低延迟，因为逻辑变简单了
    }
}

// --- 导出的函数实现 ---
extern "C" GBVSR_API void StartMonitoring(StatusCallback statusCallback, GameDataCallback dataCallback)
{
    if (g_isRunning)
    {
        if (statusCallback)
            statusCallback("Monitoring is already running.");
        return;
    }

    // 保存新的回调函数指针
    g_statusCallback = statusCallback;
    g_dataCallback = dataCallback;

    const std::wstring processName = L"GBVSR-Win64-Shipping.exe";
    DWORD processId = GetProcessIdByName(processName);
    if (processId == 0)
    {
        if (g_statusCallback)
            g_statusCallback("Error: Process not found.");
        return;
    }

    uintptr_t baseAddress = GetModuleBaseAddress(processId, processName);
    if (baseAddress == 0)
    {
        if (g_statusCallback)
            g_statusCallback("Error: Failed to get base address. Run as admin.");
        return;
    }

    // 将句柄存入全局变量
    g_hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, FALSE, processId);
    if (g_hProcess == NULL)
    {
        if (g_statusCallback)
            g_statusCallback("Error: Failed to open process handle.");
        return;
    }

    const std::vector<uintptr_t> offsets_1P_HP{0x06647558, 0x478, 0x38, 0xEE0};
    const std::vector<uintptr_t> offsets_2P_HP{0x06647558, 0x478, 0x90, 0xEE0};
    const std::vector<uintptr_t> offsets_local_pos{0x059A7D4C};
    const std::vector<uintptr_t> offsets_park_pos{0x0663E580, 0x30, 0x640, 0xD0};

    g_isRunning = true;

    // 启动所有线程时，传入全局的句柄
    g_threads.emplace_back(MonitorTask, g_hProcess, baseAddress, "HP", offsets_1P_HP, std::ref(g_gameData.hp_1p));
    g_threads.emplace_back(MonitorTask, g_hProcess, baseAddress, "HP", offsets_2P_HP, std::ref(g_gameData.hp_2p));
    g_threads.emplace_back(MonitorTask, g_hProcess, baseAddress, "LOCAL_POS", offsets_local_pos, std::ref(g_gameData.pos_local));
    g_threads.emplace_back(MonitorTask, g_hProcess, baseAddress, "PARK_POS", offsets_park_pos, std::ref(g_gameData.pos_park));
    g_threads.emplace_back(DispatcherThread);

    if (g_statusCallback)
        g_statusCallback("Monitoring started successfully.");
}

extern "C" GBVSR_API void StopMonitoring()
{
    if (!g_isRunning)
        return;

    g_isRunning = false;

    for (auto &t : g_threads)
    {
        if (t.joinable())
        {
            t.join();
        }
    }
    g_threads.clear();

    // 安全地关闭已打开的进程句柄
    if (g_hProcess != NULL)
    {
        CloseHandle(g_hProcess);
        g_hProcess = NULL;
    }

    g_statusCallback = nullptr;
    g_dataCallback = nullptr;
}

// --- 辅助函数
DWORD GetProcessIdByName(const std::wstring &processName)
{
    PROCESSENTRY32W processEntry;
    processEntry.dwSize = sizeof(PROCESSENTRY32W);
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (hSnapshot == INVALID_HANDLE_VALUE)
        return 0;
    if (Process32FirstW(hSnapshot, &processEntry))
    {
        do
        {
            if (_wcsicmp(processEntry.szExeFile, processName.c_str()) == 0)
            {
                CloseHandle(hSnapshot);
                return processEntry.th32ProcessID;
            }
        } while (Process32NextW(hSnapshot, &processEntry));
    }
    CloseHandle(hSnapshot);
    return 0;
}
uintptr_t GetModuleBaseAddress(DWORD processId, const std::wstring &moduleName)
{
    MODULEENTRY32W moduleEntry;
    moduleEntry.dwSize = sizeof(MODULEENTRY32W);
    HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
    if (hSnapshot == INVALID_HANDLE_VALUE)
        return 0;
    if (Module32FirstW(hSnapshot, &moduleEntry))
    {
        do
        {
            if (_wcsicmp(moduleEntry.szModule, moduleName.c_str()) == 0)
            {
                CloseHandle(hSnapshot);
                return (uintptr_t)moduleEntry.modBaseAddr;
            }
        } while (Module32NextW(hSnapshot, &moduleEntry));
    }
    CloseHandle(hSnapshot);
    return 0;
}
uintptr_t FindDynamicAddress(HANDLE hProcess, uintptr_t baseAddr, const std::vector<uintptr_t> &offsets)
{
    uintptr_t currentAddress = baseAddr;
    for (size_t i = 0; i < offsets.size(); ++i)
    {
        currentAddress += offsets[i];
        if (i < offsets.size() - 1)
        {
            SIZE_T bytesRead;
            uintptr_t nextAddress = 0;
            if (!ReadProcessMemory(hProcess, (LPCVOID)currentAddress, &nextAddress, sizeof(uintptr_t), &bytesRead) || bytesRead != sizeof(uintptr_t) || nextAddress == 0)
            {
                return 0;
            }
            currentAddress = nextAddress;
        }
    }
    return currentAddress;
}
