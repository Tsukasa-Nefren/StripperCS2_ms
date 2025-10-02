using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StripperCS2_ms_adapter
{
    internal static class NativeLoader
    {
        private static IntPtr _handle;

        static NativeLoader()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeLoader).Assembly, Resolve);
            TryPreload();
        }

        private static void TryPreload()
        {
            var asm = typeof(NativeLoader).Assembly;
            var dir = Path.GetDirectoryName(asm.Location) ?? AppContext.BaseDirectory;
            var file = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "StripperCS2_ms_adapter.dll"
                     : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)   ? "libStripperCS2_ms_adapter.so"
                     : "StripperCS2_ms_adapter";
            var candidate = Path.Combine(dir, file);
            if (File.Exists(candidate))
            {
                if (NativeLibrary.TryLoad(candidate, out _handle))
                {
                    Console.WriteLine($"[StripperCS2_ms] Preloaded native: {candidate}");
                    return;
                }
            }
            Console.WriteLine($"[StripperCS2_ms] Native not preloaded, will rely on resolver. Candidate: {candidate}");
        }

        private static IntPtr Resolve(string libName, Assembly assembly, DllImportSearchPath? path)
        {
            if (libName != "StripperCS2_ms_adapter" && libName != "StripperCS2_ms_adapter.dll" && libName != "libStripperCS2_ms_adapter.so")
                return IntPtr.Zero;

            if (_handle != IntPtr.Zero) return _handle;

            var baseDir = Path.GetDirectoryName(assembly.Location) ?? AppContext.BaseDirectory;
            string file =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "StripperCS2_ms_adapter.dll" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux)   ? "libStripperCS2_ms_adapter.so" :
                "StripperCS2_ms_adapter";

            var candidate = Path.Combine(baseDir, file);
            if (NativeLibrary.TryLoad(candidate, out _handle))
                return _handle;

            if (NativeLibrary.TryLoad(file, out _handle))
                return _handle;

            return IntPtr.Zero;
        }
    }
}
