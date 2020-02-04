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
        private string SAVE_FILE_NAME = "Course.json";
        public string Name { get; private set; }

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

        public async Task<bool> SaveToStorageFileAsync()
        {
            bool saveResult = false;
            if (StorageFileHelper.IsFilePathValid(DataSaveDirectory))
            {
                try
                {
                    var courseFolder = await StorageFolder.GetFolderFromPathAsync(DataSaveDirectory);
                    var courseMetaFile = await courseFolder.CreateFileAsync(SAVE_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                    using (var fs = await courseMetaFile.OpenStreamForWriteAsync())
                    {
                        JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
#if DEBUG
                        serializerOptions.WriteIndented = true;
#else
                        serializerOptions.WriteIndented = false;
#endif
                        await JsonSerializer.SerializeAsync(fs, this, serializerOptions);
                    }
                    saveResult = true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"Failed to save the course information into the folder {DataSaveDirectory}. The detail message is {ex}");
                    saveResult = false;
                }
            }
            return saveResult;
        }

        public async static Task<Course> LoadFromFile(string dataFileFullPath)
        {
            Course loadResult = null;
            if (StorageFileHelper.IsFilePathValid(dataFileFullPath))
            {
                var courseDataFile = await StorageFile.GetFileFromPathAsync(dataFileFullPath);
                using (var fs = await courseDataFile.OpenStreamForReadAsync())
                {
                    loadResult = await JsonSerializer.DeserializeAsync<Course>(fs);
                }
            }
            return loadResult;
        }

        private JsonSerializerOptions createSerializerOptions()
        {
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
            serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
#if DEBUG
            serializerOptions.WriteIndented = true;
#else
            serializerOptions.WriteIndented = false;
#endif
            return serializerOptions;
        }
    }
}
