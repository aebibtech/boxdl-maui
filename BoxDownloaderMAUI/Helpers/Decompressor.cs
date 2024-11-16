using System.IO.Compression;

namespace BoxDownloaderMAUI.Helpers;

public class Decompressor
{
    public static async Task<byte[]> GZip(MemoryStream inputStream, CancellationToken cancel = default)
    {
        inputStream.Position = 0;
        using (var outputStream = new MemoryStream())
        {
            using (var compressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                await compressionStream.CopyToAsync(outputStream, cancel);
            }
            return outputStream.ToArray();
        }
    }
    public static async Task<byte[]> Brotli(MemoryStream inputStream, CancellationToken cancel = default)
    {
        inputStream.Position = 0;
        using (var outputStream = new MemoryStream())
        {
            using (var compressionStream = new BrotliStream(inputStream, CompressionMode.Decompress))
            {
                await compressionStream.CopyToAsync(outputStream, cancel);
            }
            return outputStream.ToArray();
        }
    }
    public static async Task<byte[]> Deflate(MemoryStream inputStream, CancellationToken cancel = default)
    {
        inputStream.Position = 0;
        using (var outputStream = new MemoryStream())
        {
            using (var compressionStream = new DeflateStream(inputStream, CompressionMode.Decompress))
            {
                await compressionStream.CopyToAsync(outputStream, cancel);
            }
            return outputStream.ToArray();
        }
    }
}