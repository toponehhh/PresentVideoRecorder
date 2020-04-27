using GalaSoft.MvvmLight.Command;
using PresentVideoRecorder.ContentPages;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace PresentVideoRecorder.ViewModels
{
    public class MainPageViewModel : UwpPageViewModel
    {
        private ICommand _reLoadCourseCommand;
        private ICommand _createNewCourseCommand;
        private ICommand _navigatePageCommand;

        private bool _allowNavigate;
        private Func<Task<bool>> _stopNavigateCallback;

        public MainPageViewModel(IDialogService dialogService) : base(dialogService)
        {
            _reLoadCourseCommand = new RelayCommand(LoadExistedCourse);
            _createNewCourseCommand = new RelayCommand(CreateNewCourse);
            _navigatePageCommand = new RelayCommand<string>(NavigateToPage);

            _allowNavigate = true;
        }


        public Frame ContentFrame { get; set; }

        public Course _currentWorkingCourse;
        public Course CurrentWorkingCourse 
        { 
            get
            {
                return _currentWorkingCourse;
            }
            private set
            {
                Set(ref _currentWorkingCourse, value);
            }
        }

        public ICommand ReloadCourseCommand
        {
            get
            {
                return _reLoadCourseCommand;
            }
        }

        public ICommand CreateNewCourseCommand
        {
            get
            {
                return _createNewCourseCommand;
            }
        }

        public ICommand NavigateToPageCommand
        {
            get 
            {
                return _navigatePageCommand;
            }
        }

        public bool IsAppBusy { get; set; }

        public async void NavigateToPage(string pageTag)
        {
            if (_stopNavigateCallback != null)
            {
                _allowNavigate = await _stopNavigateCallback.Invoke();
            }
            if (_allowNavigate)
            {
                Type _page = null;

                switch (pageTag)
                {
                    case "EditPage":
                        _page = typeof(EditPage);
                        break;
                    case "PublishPage":
                        _page = typeof(PublishPage);
                        break;
                    case "TransCodePage":
                        _page = typeof(TransCodePage);
                        break;
                    default:
                        _page = null;
                        break;
                }
                if (_page != null)
                {
                    if (CurrentWorkingCourse == null)
                    {
                        LoadExistedCourse();
                    }
                    else
                    {
                        ContentFrame.Navigate(_page);
                    }
                }
            }
        }

        public async void CreateNewCourse()
        {
            if (_stopNavigateCallback != null)
            {
                _allowNavigate = await _stopNavigateCallback.Invoke();
            }
            if (_allowNavigate)
            {
                var createNewCourseVM = await _dialogService.ShowCreateNewCourseDialog();
                if (createNewCourseVM != null)
                {
                    CurrentWorkingCourse = createNewCourseVM.SelectedCourse;
                    ContentFrame.Navigate(typeof(RecordPage));
                }
            }
        }

        public async void LoadExistedCourse()
        {
            var loadExistedCourseVM = await _dialogService.ShowLoadCourseDialog();
            if (loadExistedCourseVM != null)
            {
                CurrentWorkingCourse = loadExistedCourseVM.SelectedCourse;
                ContentFrame.Navigate(loadExistedCourseVM.SelectedActionPage);
            }
        }

        public void LockNavigation(Func<Task<bool>> stopNavigationCallBack = null)
        {
            _allowNavigate = false;
            _stopNavigateCallback = stopNavigationCallBack;
        }

        public void UnlockNavigation()
        {
            _allowNavigate = true;
            _stopNavigateCallback = null;
        }
    }
}
