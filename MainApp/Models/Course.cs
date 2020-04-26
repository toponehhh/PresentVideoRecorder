using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

namespace PresentVideoRecorder.Models
{
    public class Course : ModelBase
    {
        public const string SAVE_FILE_NAME = "Course.json";
        public string Name { get; set; }

        public string DataSaveDirectory { get; set; }
        public List<string> AudioFiles { get; set; }
        public List<string> CameraVideoFiles { get; set; }
        public List<string> ScreenVideoFiles { get; set; }

        public Course()
        {
            AudioFiles = new List<string>();
            CameraVideoFiles = new List<string>();
            ScreenVideoFiles = new List<string>();
        }

        public static Course CreateNewCourse(string courseName)
        {
            Course newCourse = null;
            if (!string.IsNullOrEmpty(courseName.Trim()))
            {
                newCourse = new Course();
                newCourse.Name = courseName.Trim();
            }
            return newCourse;
        }

        protected override async Task<IStorageFile> GetSaveTargetFileAsync()
        {
            if (StorageFileHelper.IsFilePathValid(DataSaveDirectory))
            {
                var courseFolder = await StorageFolder.GetFolderFromPathAsync(DataSaveDirectory);
                var courseMetaFile = await courseFolder.TryGetItemAsync(SAVE_FILE_NAME);
                if (courseMetaFile == null)
                {
                    courseMetaFile = await courseFolder.CreateFileAsync(SAVE_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                }
                return courseMetaFile as IStorageFile;
            }
            return null;
        }

        protected override async void SaveTargetFileAsync(IStorageFile targetFile)
        {
            using (var fs = await targetFile.OpenStreamForWriteAsync())
            {
                JsonSerializerOptions serializerOptions = CreateSerializerOptions();
                await JsonSerializer.SerializeAsync(fs, this, serializerOptions);
            }
        }

        public async static Task<Course> LoadFromFile(string dataFileFullPath)
        {
            Course loadResult = null;
            if (StorageFileHelper.IsFilePathValid(dataFileFullPath))
            {
                var courseDataFile = await StorageFile.GetFileFromPathAsync(dataFileFullPath);
                using (var fs = await courseDataFile.OpenStreamForReadAsync())
                {
                    JsonSerializerOptions serializerOptions = CreateSerializerOptions();
                    loadResult = await JsonSerializer.DeserializeAsync<Course>(fs, serializerOptions);
                }
            }
            return loadResult;
        }
    }
}
