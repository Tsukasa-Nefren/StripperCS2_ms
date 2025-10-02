// src/bridge/bridge_api.cpp
#include "bridge_api.h"

#include <map>
#include <memory>
#include <string>
#include <vector>
#include <filesystem>
#include <algorithm>

#include <spdlog/spdlog.h>

// StripperCS2 internals
#include "actions/actions.h"
#include "providers/json_provider.h"

// forward decls matching hook.cpp
struct LumpData
{
    CUtlString m_name;
    char pad[0x18];
    CKeyValues3Context* m_allocatorContext;
};

struct CWorld
{
    char pad0[0x1E0];
    CUtlVector<void*> m_vecLumpData;
};

extern std::map<std::pair<std::string, std::string>, std::vector<std::unique_ptr<BaseAction>>> g_mapOverrides;

SC2_API void SC2_Init(const char* game_dir)
{
    (void)game_dir;
    spdlog::set_pattern("%^[%T] [StripperCS2_ms] [%l] %v%$");
}

SC2_API void SC2_OnLevelInit(const char* map_name, const char* game_dir)
{
    using namespace std;
    namespace fs = std::filesystem;

    g_mapOverrides.clear();

    fs::path game(game_dir ? game_dir : "");
    auto globalFilePath = game / "csgo/addons/StripperCS2/global.jsonc";

    if (fs::exists(globalFilePath))
    {
        Providers::JsonProvider provider;
        try {
            g_mapOverrides[make_pair(string("GLOBALOVERRIDE"), string(""))] = provider.Load(globalFilePath.string());
        } catch (const std::exception& e) {
            spdlog::error("Provider failed to parse {}: {}", globalFilePath.string(), e.what());
        }
    }

    fs::path base = fs::path(game_dir ? game_dir : "") / "csgo/addons/StripperCS2/maps";
    base /= map_name ? map_name : "";

    if (!fs::exists(base))
    {
        spdlog::warn("No map overrides found for {}", map_name ? map_name : "(null)");
        return;
    }

    for (const auto& entry : fs::recursive_directory_iterator(base))
    {
        if (!entry.is_regular_file()) continue;

        auto filePath  = entry.path();
        if (filePath.extension() != ".jsonc") continue;

        auto cleanPath = filePath.lexically_relative(base);

        std::string worldName = cleanPath.has_parent_path() ? cleanPath.parent_path().string() : (map_name ? map_name : "");
        std::string lumpName  = cleanPath.stem().string();

        std::transform(worldName.begin(), worldName.end(), worldName.begin(), [](unsigned char c){ return std::tolower(c); });
        std::transform(lumpName.begin(),  lumpName.end(),  lumpName.begin(),  [](unsigned char c){ return std::tolower(c); });

        Providers::JsonProvider provider;
        try {
            g_mapOverrides[std::make_pair(worldName, lumpName)] = provider.Load(filePath.string());
        } catch (const std::exception& e) {
            spdlog::error("Provider failed to parse {}: {}", filePath.string(), e.what());
        }
    }

    spdlog::info("SC2_OnLevelInit: {} rulesets", g_mapOverrides.size());
}

SC2_API void SC2_OnLevelShutdown()
{
    g_mapOverrides.clear();
    spdlog::info("SC2_OnLevelShutdown");
}

SC2_API void SC2_OnWorldCreated(CSingleWorldRep* singleWorld)
{
    if (!singleWorld) return;

    auto pWorld = singleWorld->m_pCWorld;
    auto vecLumpData = (CUtlVector<void*>*)((uint8_t*)pWorld + 0x1E0);

    FOR_EACH_VEC(*vecLumpData, i)
    {
        auto& lump = (*vecLumpData)[i];
        auto lumpData = *(LumpData**)lump;

        auto vecEntityKeyValues = (CUtlVector<CEntityKeyValues*>*)((uint8_t*)lumpData + 0x1220);
        std::string singleWorldName = singleWorld->m_name.Get();
        std::string lumpDataName    = lumpData->m_name.Get();

        std::transform(singleWorldName.begin(), singleWorldName.end(), singleWorldName.begin(), [](unsigned char c){ return std::tolower(c); });
        std::transform(lumpDataName.begin(),    lumpDataName.end(),    lumpDataName.begin(),    [](unsigned char c){ return std::tolower(c); });

        auto it = g_mapOverrides.find({ singleWorldName, lumpDataName });
        if (it != g_mapOverrides.end())
        {
            spdlog::info("Apply map override {} {}", singleWorldName, lumpDataName);
            ApplyMapOverride(it->second, vecEntityKeyValues, lumpData);
        }

        auto itG = g_mapOverrides.find({ "GLOBALOVERRIDE", "" });
        if (itG != g_mapOverrides.end())
        {
            spdlog::info("Apply global overrides");
            ApplyMapOverride(itG->second, vecEntityKeyValues, lumpData);
        }
    }
}

