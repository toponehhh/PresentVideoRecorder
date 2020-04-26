using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.ContentPages;
using PresentVideoRecorder.Helpers;
using PresentVideoRecorder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;

namespace PresentVideoRecorder.ViewModels.ContentDialogViewModels
{
    public class LoadCourseDialogModelViewModel : ContentDialogViewModel
    {
        private Dictionary<string, Type> _loadContinueWorks;
        private ICommand _loadCourseCommand;
        public LoadCourseDialogModelViewModel(IDialogService dialogService) : base(dialogService)
        {
            _pickCourseSaveFolderCommand = new RelayCommand(PickupCourseDataFolder);
            _loadCourseCommand = new RelayCommand(LoadCourseData);

            _loadContinueWorks = new Dictionary<string, Type>();
            _loadContinueWorks.Add(LocalizedStrings.GetResourceString("RecordCourseActionText"), typeof(RecordPage));
            _loadContinueWorks.Add(LocalizedStrings.GetResourceString("EditCourseActionText"), typeof(EditPage));
            _loadContinueWorks.Add(LocalizedStrings.GetResourceString("TanscodeCourseActionText"), typeof(TransCodePage));
            _loadContinueWorks.Add(LocalizedStrings.GetResourceString("UploadCourseActionText"), typeof(PublishPage));
        }

        public ICommand LoadCourseCommand
        {
            get
            {
                return _loadCourseCommand;
            }
        }

        private Type _selectedActionPage;
        
        public Type SelectedActionPage
        {
            get
            {
                return _selectedActionPage;
            }
            set
            {
                Set(ref _selectedActionPage, value);
            }
        }


        public Dictionary<string, Type> LoadContinueWorks
        {
            get
            {
                return _loadContinueWorks;
            }
        }

        public async void PickupCourseDataFolder()
        {
            var courseSaveFolder = await _dialogService.ShowSaveFolderPicker();
            if (courseSaveFolder != null)
            {
                CourseDataPath = courseSaveFolder.Path;
                var courseFilePath = Path.Combine(CourseDataPath, Course.SAVE_FILE_NAME);
                if (await ((StorageFolder)courseSaveFolder).FileExistsAsync(Course.SAVE_FILE_NAME))
                {
                    SelectedCourse = await Course.LoadFromFile(courseFilePath);
                }
                else
                {
                    SelectedCourse = Course.CreateNewCourse(courseSaveFolder.Name);
                }
                SelectedCourse.DataSaveDirectory = courseSaveFolder.Path;
                CourseName = SelectedCourse.Name;
            }
        }

        private async void LoadCourseData()
        {
            //if (!string.IsNullOrEmpty(CourseDataPath))
            //{
            //    SelectedCourse = await Course.LoadFromFile(Path.Combine(CourseDataPath, Course.SAVE_FILE_NAME));
            //}
        }

        private string _courseDataPath;
        public string CourseDataPath
        {
            get
            {
                return _courseDataPath;
            }
            set
            {
                Set(ref _courseDataPath, value);
            }
        }
    }
}
