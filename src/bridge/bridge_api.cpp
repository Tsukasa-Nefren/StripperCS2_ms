#include "bridge_api.h"
#include <windows.h>

MS_API void SC2_Init(const char* /*game_dir*/) {}
MS_API void SC2_OnLevelInit(const char* /*map_name*/, const char* /*game_dir*/) {}
MS_API void SC2_OnLevelShutdown() {}
MS_API void SC2_OnWorldCreated(CSingleWorldRep* /*singleWorld*/) {}

MS_API int  ms_bridge_init() { return 1; }
MS_API void ms_bridge_shutdown() {}
MS_API int  ms_bridge_load_gamedata(const char* /*path_utf8*/) { return 1; }
MS_API int  ms_bridge_hook_by_name(const char* /*key_utf8*/, void* /*detour_fn_ptr*/, void** out_handle) { if(out_handle) *out_handle = nullptr; return 0; }
MS_API void ms_bridge_unhook(void* /*handle*/) {}
