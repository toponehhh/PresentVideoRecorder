using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace PresentVideoRecorder.ViewModels.ContentDialogViewModels
{
    public class CreateNewCourseDialogViewModel: ContentDialogViewModel
    {
        private ICommand _createNewCourseCommand;
        public CreateNewCourseDialogViewModel(IDialogService dialogService):base(dialogService)
        {
            _pickCourseSaveFolderCommand = new RelayCommand(PickupCourseSaveFolder);
            _createNewCourseCommand = new RelayCommand(async () => await SaveNewCourse());
        }

        public ICommand CreateNewCourseCommand
        {
            get
            {
                return _createNewCourseCommand;
            }
        }

        public async Task<bool> SaveNewCourse()
        {
            bool createdCourse = false;
            if (!string.IsNullOrEmpty(CourseName) && !string.IsNullOrEmpty(CourseSavePath))
            {
                var courseSaveParentFolder = await StorageFolder.GetFolderFromPathAsync(CourseSavePath);
                var courseSaveFolder = await courseSaveParentFolder.CreateFolderAsync(CourseName, CreationCollisionOption.OpenIfExists);
                if (SelectedCourse == null)
                {
                    SelectedCourse = Course.CreateNewCourse(CourseName);
                    SelectedCourse.DataSaveDirectory = courseSaveFolder.Path;
                    createdCourse = true;
                }
            }
            return createdCourse;
        }

        public async void PickupCourseSaveFolder()
        {
            var courseSaveFolder = await _dialogService.ShowSaveFolderPicker();
            if (courseSaveFolder != null)
            {
                CourseSavePath = courseSaveFolder.Path;
            }
        }

        private string _courseSavePath;
        public string CourseSavePath
        {
            get
            {
                return _courseSavePath;
            }
            set
            {
                Set(ref _courseSavePath, value);
            }
        }
    }
}
