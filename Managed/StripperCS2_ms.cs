using System;
using System.IO;
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
        public string DisplayName => "StripperCS2_ms";
        public string DisplayAuthor => "you";

        readonly ILogger<StripperCS2_ms> _log;
        readonly string _moduleDir;
        INativeDetourHook? _hook;

        public StripperCS2_ms(ISharedSystem sharedSystem, string? dllPath, string? sharpPath, Version? version, IConfiguration? coreConfig, bool hotReload)
        {
            _log = sharedSystem.GetLoggerFactory().CreateLogger<StripperCS2_ms>();
            _moduleDir = Path.GetDirectoryName(dllPath ?? typeof(StripperCS2_ms).Assembly.Location) ?? AppContext.BaseDirectory;
        }

        public bool Init()
        {
            NativeLoader.EnsureLoaded(_moduleDir);
            Native.SC2_Init(_moduleDir);
            return true;
        }

        public void PostInit()
        {
            unsafe
            {
                nint fnPtr = (nint)(delegate* unmanaged<nint, nint, nint>)&Detour_CreateWorldInternal;
                if (!InterfaceBridge.TryHookFnPtr("IWorldRendererMgr::CreateWorldInternal", fnPtr, out var hook)) return;
                _hook = hook;
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
            Native.SC2_OnLevelInit(mapName, _moduleDir);
        }

        [UnmanagedCallersOnly]
        static nint Detour_CreateWorldInternal(nint pThis, nint pSingleWorldRep)
        {
            try { Native.SC2_OnWorldCreated(pSingleWorldRep); } catch { }
            return 0;
        }
    }
}
