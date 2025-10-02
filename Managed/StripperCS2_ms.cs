using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
using Sharp.Shared.Hooks;
namespace StripperCS2_ms_adapter{
internal static class Native{
#if WINDOWS
  const string LIB = "StripperCS2_ms_adapter.dll";
#else
  const string LIB = "libStripperCS2_ms_adapter.so";
#endif
  [DllImport(LIB, CallingConvention=CallingConvention.Cdecl)] internal static extern void SC2_Init(string gameDir);
  [DllImport(LIB, CallingConvention=CallingConvention.Cdecl)] internal static extern void SC2_OnLevelInit(string mapName, string gameDir);
  [DllImport(LIB, CallingConvention=CallingConvention.Cdecl)] internal static extern void SC2_OnLevelShutdown();
  [DllImport(LIB, CallingConvention=CallingConvention.Cdecl)] internal static extern void SC2_OnWorldCreated(nint singleWorldRep);
}
public sealed class StripperCS2_ms : IModSharpModule{
  public string DisplayName => "StripperCS2_ms";
  public string DisplayAuthor => "you";
  private readonly ILogger<StripperCS2_ms> _log;
  private readonly InterfaceBridge _bridge;
  private INativeDetourHook? _hook;
  private static unsafe delegate* unmanaged<nint,nint,nint> _trampoline;
  public StripperCS2_ms(ISharedSystem s,string? dll,string? sharp,Version? ver,Microsoft.Extensions.Configuration.IConfiguration? cfg,bool hot){
    _bridge = new InterfaceBridge(dll!, sharp!, ver!, s);
    _log = s.GetLoggerFactory().CreateLogger<StripperCS2_ms>();
  }
  public bool Init(){ Native.SC2_Init(_bridge.GameDirectory); return true; }
  public void PostInit(){ unsafe{ var addr=_bridge.GameData.GetAddress("IWorldRendererMgr::CreateWorldInternal"); var hook=_bridge.HookManager.CreateDetourHook(); hook.Prepare(addr,(nint)(delegate* unmanaged<nint,nint,nint>)&Detour_CreateWorldInternal); if(!hook.Install()) throw new InvalidOperationException("Detour install failed"); _trampoline=(delegate* unmanaged<nint,nint,nint>)hook.Trampoline; _hook=hook; } }
  public void OnAllModulesLoaded(){} public void OnLibraryConnected(string n){} public void OnLibraryDisconnect(string n){} 
  public void Shutdown(){ _hook?.Uninstall(); _hook=null; Native.SC2_OnLevelShutdown(); }
  public void OnLevelInit(string mapName){ Native.SC2_OnLevelInit(mapName, _bridge.GameDirectory); }
  private static unsafe nint Detour_CreateWorldInternal(nint pThis, nint pSingleWorldRep){ var ret=_trampoline(pThis,pSingleWorldRep); Native.SC2_OnWorldCreated(pSingleWorldRep); return ret; }
}}

