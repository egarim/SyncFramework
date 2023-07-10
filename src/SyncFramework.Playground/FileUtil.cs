using Microsoft.JSInterop;

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
    }
}
