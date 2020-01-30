using PresentVideoRecorder.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PresentVideoRecorder.ViewModels
{
    public class UwpPageViewModel : UwpViewModelBase
    {
        protected IDialogService _dialogService;
        public UwpPageViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }
    }
}
