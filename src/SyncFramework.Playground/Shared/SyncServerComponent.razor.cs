
using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using System.Text;
using MudBlazor;
using SyncFramework.Playground.EfCore;
using Microsoft.JSInterop;

namespace SyncFramework.Playground.Shared
{
    public partial class SyncServerComponent
    {
        [Parameter]
        public IDeltaStore DeltaStore { get; set; }
        [Parameter]
        public string NodeId { get; set; }
        protected override void OnInitialized()
        {
            SyncServerNode syncServerNode = new SyncServerNode(DeltaStore, null, NodeId);
            var Server = new SyncServer(syncServerNode);
            this.HttpClient = new HttpClient(new FakeHandler(Server));
            this.HttpClient.BaseAddress=new Uri("http://FakeHandlerAddress");   
            base.OnInitialized();
        }
        [Inject]
        public IJSRuntime js { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }
        public HttpClient HttpClient { get; set; }
        public Dictionary<IDelta, string> Deltas
        {
            get
            {
                var GetDeltasTask = this.DeltaStore.GetDeltasAsync("", default);
                GetDeltasTask.Wait();
                Dictionary<IDelta, string> keyValuePairs = new Dictionary<IDelta, string>(GetDeltasTask.Result.Count());
                foreach (IDelta delta in GetDeltasTask.Result)
                {
                    var Content = this.DeltaStore.GetDeltaOperations<List<ModificationCommandData>>(delta);
                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (ModificationCommandData modificationCommandData in Content)
                    {
                        var JsonModification = System.Text.Json.JsonSerializer.Serialize(modificationCommandData, new JsonSerializerOptions { WriteIndented = true });
                        stringBuilder.AppendLine(JsonModification);
                    }
                    keyValuePairs.Add(delta, stringBuilder.ToString());
                }
                return keyValuePairs;
            }
        }
        public async void PreviewDelta(string Delta)
        {
            Delta = CleanDelta(Delta);
            DialogOptions fullScreen = new DialogOptions() { FullScreen = true, CloseButton = true };
            var parameters = new DialogParameters<IPhoneNumber>
            {
                { "Content", Delta }
            };

            var dialog = await DialogService.ShowAsync<DeltaPreviewComponent>("Delta", parameters, fullScreen);

        }

        private static string CleanDelta(string Delta)
        {
            Delta = Delta.Replace("\\u0022", "\"").Replace("\\u0060", "\'").Replace("\\n", "");
            return Delta;
        }

        public async void DownloadDelta(KeyValuePair<IDelta, string> Delta)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(CleanDelta(Delta.Value));
            await FileUtil.SaveAs(js, $"{Delta.Key.Index}.json", byteArray);

        }
    }
}
