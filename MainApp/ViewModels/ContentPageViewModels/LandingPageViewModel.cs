using GalaSoft.MvvmLight.Command;
using PresentVideoRecorder.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PresentVideoRecorder.ViewModels.ContentPageViewModels
{
    public class LandingPageViewModel: UwpContentPageViewModel
    {
        private ICommand _createNewCourseCommand;
        private ICommand _loadCourseCommand;
        public LandingPageViewModel(MainPageViewModel parentPage, IDialogService dialogService) : base(parentPage, dialogService)
        {
            _createNewCourseCommand = new RelayCommand(parentPage.CreateNewCourse);
            _loadCourseCommand = new RelayCommand(parentPage.LoadExistedCourse);
        }

        public ICommand CreateNewCourseCommand
        {
            get
            {
                return _createNewCourseCommand;
            }
        }

        public ICommand LoadCourseCommand
        {
            get
            {
                return _loadCourseCommand;
            }
        }
    }
}
