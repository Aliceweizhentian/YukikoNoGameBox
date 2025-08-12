#pragma once

#ifdef GBVSR_EXPORTS
#define GBVSR_API __declspec(dllexport)
#else
#define GBVSR_API __declspec(dllimport)
#endif

// 定义游戏模式的枚举，C#和C++将通过整数值对应
enum GameMode {
    LOCAL_FIGHT = 1,
    PARK_FIGHT = 2
};

// 步骤1.1: 定义将要传递给C#的数据结构
// 使用 #pragma pack(1) 确保C++和C#之间的内存布局完全一致，避免对齐问题
#pragma pack(push, 1)
struct GameDataSnapshot
{
    int hp_1p;
    int hp_2p;
    int pos_local;
    int pos_park;
};
#pragma pack(pop)

// 步骤1.2: 定义新的回调函数类型
// 它现在只接收一个指向数据快照的指针
typedef void (*GameDataCallback)(const GameDataSnapshot *snapshot);

// 步骤1.3: 更新导出的函数签名
// 我们需要一个通用的状态回调来处理启动/停止时的信息，
// 和一个专门的游戏数据回调来传递实时数据。
typedef void (*StatusCallback)(const char *message);

extern "C" GBVSR_API void StartMonitoring(StatusCallback statusCallback, GameDataCallback dataCallback);
extern "C" GBVSR_API void StopMonitoring();