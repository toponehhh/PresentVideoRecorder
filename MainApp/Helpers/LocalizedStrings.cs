using Windows.ApplicationModel.Resources;

namespace PresentVideoRecorder.Helpers
{
    public class LocalizedStrings
    {
        public string this[string key]
        {
            get
            {
                return ResourceLoader.GetForViewIndependentUse().GetString(key);
            }
        }

        public static string GetResourceString(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                return ResourceLoader.GetForViewIndependentUse().GetString(key);
            }
            return string.Empty;
        }
    }
}
