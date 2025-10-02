using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Microsoft.Extensions.Configuration;

namespace StripperCS2_ms_adapter
{
    internal static class Native
    {
#if WINDOWS
        const string LIB = "StripperCS2_ms_adapter.dll";
#else
        const string LIB = "libStripperCS2_ms_adapter.so";
#endif
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_Init(string gameDir);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_OnLevelInit(string mapName, string gameDir);
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_OnLevelShutdown();
        [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_OnWorldCreated(nint singleWorldRep);
    }

    public sealed class StripperCS2_ms : IModSharpModule
    {
        public string DisplayName   => "StripperCS2_ms";
        public string DisplayAuthor => "you";

        private readonly ILogger<StripperCS2_ms> _log;
        private INativeDetourHook? _hook;

        private static unsafe delegate* unmanaged<nint, nint, nint> _trampoline;

        public StripperCS2_ms(ISharedSystem sharedSystem,
                              string? dllPath,
                              string? sharpPath,
                              Version? version,
                              IConfiguration? coreConfig,
                              bool hotReload)
        {
            _log = sharedSystem.GetLoggerFactory().CreateLogger<StripperCS2_ms>();
        }

        public bool Init()
        {
            // If you can resolve your game dir here, pass it; adapter tolerates empty.
            Native.SC2_Init("");
            return true;
        }

        public void PostInit()
        {
            unsafe
            {
                // Use native bridge to hook by gamedata key (requires adapter to provide ms_bridge_* entrypoints).
                if (!InterfaceBridge.TryHook("IWorldRendererMgr::CreateWorldInternal",
                                             (delegate* unmanaged<nint, nint, nint>)&Detour_CreateWorldInternal,
                                             out var hook))
                {
                    _log.LogWarning("TryHook(CreateWorldInternal) failed; detour not installed.");
                    return;
                }
                _hook = hook;
                _log.LogInformation("Installed detour: CreateWorldInternal");
            }
        }

        public void OnAllModulesLoaded() { }
        public void OnLibraryConnected(string name) { }
        public void OnLibraryDisconnect(string name) { }

        public void Shutdown()
        {
            try { _hook?.Dispose(); } catch { }
            _hook = null;
            Native.SC2_OnLevelShutdown();
        }

        public void OnLevelInit(string mapName)
        {
            // If you know the game directory at runtime, pass it here.
            Native.SC2_OnLevelInit(mapName, "");
        }

        [System.Runtime.InteropServices.UnmanagedCallersOnly]
        private static nint Detour_CreateWorldInternal(nint pThis, nint pSingleWorldRep)
        {
            var ret = _trampoline is not null ? _trampoline(pThis, pSingleWorldRep) : 0;
            Native.SC2_OnWorldCreated(pSingleWorldRep);
            return ret;
        }
    }
}
