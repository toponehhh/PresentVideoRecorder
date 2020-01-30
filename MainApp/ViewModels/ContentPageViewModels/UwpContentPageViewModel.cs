using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class UwpContentPageViewModel<DataModel> : UwpPageViewModel where DataModel : ModelBase
    {
        protected UwpPageViewModel pageParent;
        protected DataModel innerData;

        public UwpContentPageViewModel(UwpPageViewModel parent, IDialogService dialogService): base(dialogService)
        {
            pageParent = parent;
        }
    }
}
