using GalaSoft.MvvmLight;
using PresentVideoRecorder.Helpers;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace PresentVideoRecorder.Models
{
    public abstract class ModelBase : ObservableObject
    {
        protected abstract Task<IStorageFile> GetSaveTargetFileAsync();
        protected abstract void SaveTargetFileAsync(IStorageFile targetFile);

        public virtual async Task<bool> SaveToStorageFileAsync()
        {
            bool saveResult = false;

            try
            {
                var targetFile = await GetSaveTargetFileAsync();
                if (targetFile != null)
                {
                    SaveTargetFileAsync(targetFile);
                    saveResult = true;
                }
                else
                {
                    Logger.Instance.Error($"Cannot save data into empty file! Save action cancelled!");
                    saveResult = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Failed to save the model information. The detail message is {ex}");
                saveResult = false;
            }

            return saveResult;
        }

        protected static JsonSerializerOptions CreateSerializerOptions()
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
