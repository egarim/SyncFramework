using Microsoft.JSInterop;
using System.IO.Compression;

namespace SyncFramework.Playground
{
    public static class FileUtil
    {
        public async static Task SaveAs(IJSRuntime js, string filename, byte[] data)
        {
            await js.InvokeAsync<object>(
                "save",
                filename,
                Convert.ToBase64String(data));
        }
        public static byte[] CreateZipFromByteArrays(Dictionary<string, byte[]> files)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Create a new ZipArchive object to hold the contents of the zip file
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        // Create a new ZipArchiveEntry in the ZipArchive
                        var zipArchiveEntry = archive.CreateEntry(file.Key, CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open())
                        {
                            zipStream.Write(file.Value, 0, file.Value.Length);
                        }
                    }
                }
                // Return the bytes of the MemoryStream (i.e., the zip file)
                return memoryStream.ToArray();
            }
        }
    }
}
