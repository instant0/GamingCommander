using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace GamingCommander.App;

/// <summary>
/// After publish, support assemblies live in <c>lib/</c>. Debug/output stays flat.
/// The apphost still requires the entry dll + runtimeconfig + deps next to the exe.
/// </summary>
internal static class PublishLayout
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        string lib = Path.Combine(AppContext.BaseDirectory, "lib");
        if (!Directory.Exists(lib))
            return;

        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            if (string.IsNullOrEmpty(name.Name))
                return null;

            string path = Path.Combine(lib, name.Name + ".dll");
            if (!File.Exists(path))
                return null;

            Assembly loaded = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            if (name.Name is "SkiaSharp" or "HarfBuzzSharp")
                NativeLibrary.SetDllImportResolver(loaded, ResolveNative);
            return loaded;
        };
    }

    private static IntPtr ResolveNative(string name, Assembly assembly, DllImportSearchPath? search)
    {
        string lib = Path.Combine(AppContext.BaseDirectory, "lib");
        string file = Path.GetFileName(name);
        if (!file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            file += ".dll";

        foreach (string candidate in new[] { file, "lib" + file })
        {
            string path = Path.Combine(lib, candidate);
            if (File.Exists(path) && NativeLibrary.TryLoad(path, out IntPtr handle))
                return handle;
        }

        return IntPtr.Zero;
    }
}
