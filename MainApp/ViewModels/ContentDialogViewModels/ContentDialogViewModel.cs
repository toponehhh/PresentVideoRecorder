using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PresentVideoRecorder.ViewModels.ContentDialogViewModels
{
    public class ContentDialogViewModel: UwpViewModelBase
    {
        protected IDialogService _dialogService;
        protected ICommand _pickCourseSaveFolderCommand;
        public ContentDialogViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public ICommand PickCourseSaveFolderCommand
        {
            get
            {
                return _pickCourseSaveFolderCommand;
            }
        }

        public Course SelectedCourse { get; protected set; }

        private string _courseName;
        public string CourseName
        {
            get
            {
                return _courseName;
            }
            set
            {
                Set(ref _courseName, value);
            }
        }
    }
}
