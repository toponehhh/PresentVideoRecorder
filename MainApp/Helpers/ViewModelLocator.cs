using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using PresentVideoRecorder.Models;
using PresentVideoRecorder.ViewModels;
using PresentVideoRecorder.ViewModels.ContentDialogViewModels;
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
            SimpleIoc.Default.Register<MainPageViewModel>();
            //SimpleIoc.Default.Register<RecordPageViewModel>(()=> { return new RecordPageViewModel(SimpleIoc.Default.GetInstance<MainPageViewModel>()); });
            SimpleIoc.Default.Register<LandingPageViewModel>();
            SimpleIoc.Default.Register<RecordPageViewModel>();
            SimpleIoc.Default.Register<EditPageViewModel>();
            SimpleIoc.Default.Register<TansCodePageViewModel>();
            SimpleIoc.Default.Register<CreateNewCourseDialogViewModel>();
            SimpleIoc.Default.Register<LoadCourseDialogModelViewModel>();
        }

        public RecordPageViewModel RecordPageVm => SimpleIoc.Default.GetInstance<RecordPageViewModel>();
        public EditPageViewModel EditPageVM => SimpleIoc.Default.GetInstance<EditPageViewModel>();
        public TansCodePageViewModel TansCodePageVM => SimpleIoc.Default.GetInstance<TansCodePageViewModel>();
        public LandingPageViewModel LandingPageVM => SimpleIoc.Default.GetInstance<LandingPageViewModel>();
        public MainPageViewModel MainPageVM => SimpleIoc.Default.GetInstance<MainPageViewModel>();

        public CreateNewCourseDialogViewModel CreateNewCourseDialogVM => SimpleIoc.Default.GetInstance<CreateNewCourseDialogViewModel>();
        public LoadCourseDialogModelViewModel LoadCourseDialogModelVM => SimpleIoc.Default.GetInstance<LoadCourseDialogModelViewModel>();
    }
}
