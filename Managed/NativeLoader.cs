using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StripperCS2_ms_adapter
{
    internal static class NativeLoader
    {
        static IntPtr _handle;

        public static bool EnsureLoaded(string baseDir)
        {
            string file = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "StripperCS2_ms_adapter.dll"
                        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)   ? "libStripperCS2_ms_adapter.so"
                        : "StripperCS2_ms_adapter";
            string candidate = Path.Combine(baseDir, file);
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out _handle)) return true;
            if (NativeLibrary.TryLoad(file, out _handle)) return true;
            return false;
        }

        static NativeLoader()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeLoader).Assembly, Resolve);
        }

        static IntPtr Resolve(string libName, Assembly assembly, DllImportSearchPath? path)
        {
            if (_handle != IntPtr.Zero) return _handle;
            return IntPtr.Zero;
        }
    }
}
