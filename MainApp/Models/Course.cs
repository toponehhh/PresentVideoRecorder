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
        public string Name { get; private set; }

        public string DataSaveDirectory { get; private set; }
        public string AudioFilePath { get; set; }
        public string CameraVideoFilePath { get; set; }
        public string ScreenVideoFilePath { get; set; }

        public Course()
        {

        }

        //public static Course CreateNewCourse(string courseName)
        //{
        //    Course newCourse = null;
        //    if(!string.IsNullOrEmpty(courseName.Trim()))
        //    {
        //        newCourse = new Course();
        //        newCourse.Name = courseName.Trim();
        //    }
        //    return newCourse;
        //}

        public async Task<bool> Save()
        {
            bool saveResult = false;
            if (!string.IsNullOrEmpty(DataSaveDirectory))
            {
                try
                {
                    var parentFolder = await StorageFolder.GetFolderFromPathAsync(DataSaveDirectory);
                    var courseFolder = await parentFolder.CreateFolderAsync(Name, CreationCollisionOption.GenerateUniqueName);
                    var courseMetaFile = await courseFolder.CreateFileAsync($"{Name}.json");
                    await FileIO.WriteTextAsync(courseMetaFile, JsonSerializer.Serialize(this));
                    saveResult = true;
                }
                catch(Exception ex)
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
            if(File.Exists(dataFileFullPath))
            {
                using (FileStream fs = File.OpenRead(dataFileFullPath))
                {
                    loadResult = await JsonSerializer.DeserializeAsync<Course>(fs);
                }
            }
            return loadResult;
        }
    }
}
