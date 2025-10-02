// src/bridge/bridge_api.h
#pragma once
#include <stdint.h>

#ifdef _WIN32
  #define SC2_API extern "C" __declspec(dllexport)
#else
  #define SC2_API extern "C" __attribute__((visibility("default")))
#endif

struct CSingleWorldRep;

SC2_API void SC2_Init(const char* game_dir);
SC2_API void SC2_OnLevelInit(const char* map_name, const char* game_dir);
SC2_API void SC2_OnLevelShutdown();
SC2_API void SC2_OnWorldCreated(CSingleWorldRep* singleWorld);

