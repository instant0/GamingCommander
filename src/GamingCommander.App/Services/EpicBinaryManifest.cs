using System.IO.Compression;
using System.Text;

namespace GamingCommander.App.Services;

/// <summary>
/// Reads only the header of an Epic binary <c>.manifest</c> (same layout as
/// <c>tools/decode_manifest.py</c>): app name, build, launch exe.
/// </summary>
internal static class EpicBinaryManifest
{
    public sealed record Header(string AppName, string BuildVersion, string LaunchExe);

    public static bool TryRead(string manifestPath, out Header? header)
    {
        header = null;
        if (!File.Exists(manifestPath))
            return false;
        try
        {
            byte[] raw = File.ReadAllBytes(manifestPath);
            if (raw.Length < 45)
                return false;
            byte storedAs = raw[36];
            byte[] body = (storedAs & 1) != 0 ? Inflate(raw.AsSpan(41)) : raw[41..];
            if (body.Length < 18)
                return false;

            int off = 14;
            if (!ReadFString(body, ref off, out string app))
                return false;
            if (!ReadFString(body, ref off, out string build))
                return false;
            if (!ReadFString(body, ref off, out string launch))
                return false;
            header = new Header(app, build, launch.Replace('\\', '/').TrimStart('/'));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Inflate(ReadOnlySpan<byte> zlib)
    {
        using var input = new MemoryStream(zlib.ToArray());
        using var z = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        z.CopyTo(output);
        return output.ToArray();
    }

    private static bool ReadFString(byte[] body, ref int off, out string value)
    {
        value = "";
        if (off + 4 > body.Length)
            return false;
        int size = BitConverter.ToInt32(body, off);
        off += 4;
        if (size <= 0)
            return true;
        if (off + size > body.Length)
            return false;
        int take = size > 0 ? size - 1 : 0;
        value = Encoding.UTF8.GetString(body, off, Math.Max(0, take));
        off += size;
        return true;
    }
}
