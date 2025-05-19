using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using SynFrameworkStudio.Module.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SynFrameworkStudio.Module.Controllers
{
    
    public class DeltaRecordController : ViewController
    {
        PopupWindowShowAction ShowDeltaContent;
        /// <summary>
        /// <para>Creates an instance of the <see cref="DeltaRecordController"/> class.</para>
        /// </summary>
        public DeltaRecordController()
        {
            ShowDeltaContent = new PopupWindowShowAction(this, "Show Delta Content", "View");
            ShowDeltaContent.Execute += ShowDeltaContent_Execute;
            ShowDeltaContent.CustomizePopupWindowParams += ShowDeltaContent_CustomizePopupWindowParams;
            
        }
        private void ShowDeltaContent_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var selectedPopupWindowObjects = e.PopupWindowViewSelectedObjects;
            var selectedSourceViewObjects = e.SelectedObjects;
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112723/).
        }
        private void ShowDeltaContent_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var Os = this.Application.CreateObjectSpace(typeof(TextObject));
            var registerNodeRequestParameters = Os.CreateObject<TextObject>();


            var CurrentDelta=  this.View.CurrentObject as IDelta;

            var Descompresed= SerializationHelper.DecompressCore(CurrentDelta.Operation);
            var Modifications=  SerializationHelper.DeserializeCore<List<ModificationCommandData>>(Descompresed);
           
            StringBuilder stringBuilder = new StringBuilder();
            foreach (ModificationCommandData modificationCommandData in Modifications)
            {
                var JsonModification = System.Text.Json.JsonSerializer.Serialize(modificationCommandData, new JsonSerializerOptions { WriteIndented = true });
                stringBuilder.AppendLine(JsonModification);
            }
         




            // Set the e.View parameter to a newly created view (https://docs.devexpress.com/eXpressAppFramework/112723/).
            registerNodeRequestParameters.Content = stringBuilder.ToString();

            e.View = Application.CreateDetailView(Os, registerNodeRequestParameters);
        }
    }
}
