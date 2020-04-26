using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public abstract class UwpContentPageViewModel : UwpPageViewModel
    {
        protected MainPageViewModel pageParent;

        public UwpContentPageViewModel(MainPageViewModel parent, IDialogService dialogService): base(dialogService)
        {
            pageParent = parent;
        }

        public void SetAppBusyFlag(bool flag)
        {
            pageParent.IsAppBusy = flag;
        }

        public bool IsAppInBusyStatus => pageParent.IsAppBusy;

    }
}
