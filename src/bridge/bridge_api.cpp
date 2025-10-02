#include "bridge_api.h"

// Minimal stub to satisfy linking. Replace with real logic when ready.
#if defined(_MSC_VER)
  #pragma comment(lib, "User32.lib")
#endif

SC2_API void SC2_Init(const char* /*game_dir*/) {}
SC2_API void SC2_OnLevelInit(const char* /*map_name*/, const char* /*game_dir*/) {}
SC2_API void SC2_OnLevelShutdown() {}
SC2_API void SC2_OnWorldCreated(CSingleWorldRep* /*singleWorld*/) {}
