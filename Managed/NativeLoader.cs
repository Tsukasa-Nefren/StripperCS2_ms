using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StripperCS2_ms_adapter
{
    internal static class NativeLoader
    {
        static NativeLoader()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeLoader).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libName, Assembly assembly, DllImportSearchPath? path)
        {
            if (libName != "StripperCS2_ms_adapter" && libName != "StripperCS2_ms_adapter.dll" && libName != "libStripperCS2_ms_adapter.so")
                return IntPtr.Zero;

            var baseDir = Path.GetDirectoryName(assembly.Location) ?? AppContext.BaseDirectory;
            string file =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "StripperCS2_ms_adapter.dll" :
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux)   ? "libStripperCS2_ms_adapter.so" :
                "StripperCS2_ms_adapter";

            var candidate = Path.Combine(baseDir, file);
            if (NativeLibrary.TryLoad(candidate, out var handle))
                return handle;

            // Also try default resolution without path override
            if (NativeLibrary.TryLoad(file, out handle))
                return handle;

            return IntPtr.Zero;
        }
    }
}
