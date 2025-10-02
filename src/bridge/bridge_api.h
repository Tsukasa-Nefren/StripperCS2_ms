#pragma once
#include <stdint.h>

#if defined(_WIN32)
  #define MS_API extern "C" __declspec(dllexport)
#else
  #define MS_API extern "C" __attribute__((visibility("default")))
#endif

struct CSingleWorldRep;

MS_API void SC2_Init(const char* game_dir);
MS_API void SC2_OnLevelInit(const char* map_name, const char* game_dir);
MS_API void SC2_OnLevelShutdown();
MS_API void SC2_OnWorldCreated(CSingleWorldRep* singleWorld);

// Minimal bridge API expected by InterfaceBridge.cs
MS_API int  ms_bridge_init();
MS_API void ms_bridge_shutdown();
MS_API int  ms_bridge_load_gamedata(const char* path_utf8);
MS_API int  ms_bridge_hook_by_name(const char* key_utf8, void* detour_fn_ptr, void** out_handle);
MS_API void ms_bridge_unhook(void* handle);
