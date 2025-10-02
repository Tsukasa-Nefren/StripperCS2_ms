using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Microsoft.Extensions.Configuration;

namespace StripperCS2_ms_adapter
{
    internal static class Native
    {
        [DllImport("StripperCS2_ms_adapter", CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_Init(string gameDir);
        [DllImport("StripperCS2_ms_adapter", CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_OnLevelInit(string mapName, string gameDir);
        [DllImport("StripperCS2_ms_adapter", CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_OnLevelShutdown();
        [DllImport("StripperCS2_ms_adapter", CallingConvention = CallingConvention.Cdecl)] internal static extern void SC2_OnWorldCreated(nint singleWorldRep);
        static Native() { _ = typeof(NativeLoader); }
    }

    public sealed class StripperCS2_ms : IModSharpModule
    {
        public string DisplayName   => "StripperCS2_ms";
        public string DisplayAuthor => "you";

        private readonly ILogger<StripperCS2_ms> _log;
        private INativeDetourHook? _hook;

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
            Native.SC2_Init("");
            return true;
        }

        public void PostInit()
        {
            unsafe
            {
                nint fnPtr = (nint)(delegate* unmanaged<nint, nint, nint>)&Detour_CreateWorldInternal;
                if (!InterfaceBridge.TryHookFnPtr("IWorldRendererMgr::CreateWorldInternal", fnPtr, out var hook))
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
            Native.SC2_OnLevelInit(mapName, "");
        }

        [UnmanagedCallersOnly]
        private static nint Detour_CreateWorldInternal(nint pThis, nint pSingleWorldRep)
        {
            try { Native.SC2_OnWorldCreated(pSingleWorldRep); } catch { }
            return 0;
        }
    }
}
