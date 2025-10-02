using System;
using System.Runtime.InteropServices;
using System.Security;

public interface INativeDetourHook : IDisposable
{
    bool IsAttached { get; }
}

public sealed class NativeDetourHook : INativeDetourHook
{
    IntPtr _handle;
    bool _attached;

    internal NativeDetourHook(IntPtr handle, bool attached)
    {
        _handle = handle;
        _attached = attached;
    }

    public bool IsAttached => _attached && _handle != IntPtr.Zero;

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            try { InterfaceBridge.Unhook(_handle); } catch { }
            _handle = IntPtr.Zero;
            _attached = false;
        }
        GC.SuppressFinalize(this);
    }

    ~NativeDetourHook() { try { Dispose(); } catch { } }
}

[SuppressUnmanagedCodeSecurity]
internal static class InterfaceBridge
{
#if WINDOWS
    const string NativeLib = "StripperCS2_ms_adapter";
#else
    const string NativeLib = "StripperCS2_ms_adapter";
#endif

    [DllImport(NativeLib, EntryPoint = "ms_bridge_init", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Initialize();

    [DllImport(NativeLib, EntryPoint = "ms_bridge_shutdown", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Shutdown();

    [DllImport(NativeLib, EntryPoint = "ms_bridge_load_gamedata", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int LoadGamedata(string path);

    [DllImport(NativeLib, EntryPoint = "ms_bridge_hook_by_name", CallingConvention = CallingConvention.Cdecl)]
    static extern int _HookByName([MarshalAs(UnmanagedType.LPUTF8Str)] string key, IntPtr detour, out IntPtr handle);

    [DllImport(NativeLib, EntryPoint = "ms_bridge_unhook", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Unhook(IntPtr handle);

    public static bool TryHookFnPtr(string key, nint fnPtr, out INativeDetourHook hook)
    {
        if (_HookByName(key, (IntPtr)fnPtr, out var handle) == 0 || handle == IntPtr.Zero)
        {
            hook = new NativeDetourHook(IntPtr.Zero, false);
            return false;
        }
        hook = new NativeDetourHook(handle, true);
        return true;
    }
}
