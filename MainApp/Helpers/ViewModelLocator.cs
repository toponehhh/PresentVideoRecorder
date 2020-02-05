using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using PresentVideoRecorder.Models;
using PresentVideoRecorder.ViewModels;
using PresentVideoRecorder.ViewModels.ContentPageViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PresentVideoRecorder.Helpers
{
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<IDialogService, DialogService>();
            SimpleIoc.Default.Register<UwpPageViewModel, MainPageViewModel>();
            //SimpleIoc.Default.Register<RecordPageViewModel>(()=> { return new RecordPageViewModel(SimpleIoc.Default.GetInstance<MainPageViewModel>()); });
            SimpleIoc.Default.Register<RecordPageViewModel>();
            SimpleIoc.Default.Register<EditPageViewModel>();
        }

        public RecordPageViewModel RecordPageVm => SimpleIoc.Default.GetInstance<RecordPageViewModel>();
        public EditPageViewModel EditPageVM => SimpleIoc.Default.GetInstance<EditPageViewModel>();
        
    }
}
