using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace PresentVideoRecorder.Helpers
{
    public class Logger : IDisposable
    {
        private FileLoggingSession _logSession;
        private LoggingChannel _logChannel;
        private bool _isDisposed = false;

        private static Logger _loggerInstance;

        private bool _isChannelEnabled = false;
        private LoggingLevel _channelLoggingLevel = LoggingLevel.Verbose;

        private const string Prefix = "RecorderApp_";
        public const string DEFAULT_SESSION_NAME = Prefix + "Session";
        public const string DEFAULT_CHANNEL_NAME = Prefix + "Channel";
        private const string LOGGING_ENABLED_SETTING_KEY_NAME = Prefix + "LoggingEnabled";
        private const string LOGFILEGEN_BEFORE_SUSPEND_SETTING_KEY_NAME = Prefix + "LogFileGeneratedBeforeSuspend";
        public const string  APP_LOG_FILE_FOLDER_NAME = Prefix + "LogFiles";

        public bool IsPreparingForSuspend { get; private set; }

        private Logger()
        {
            _logChannel = new LoggingChannel(DEFAULT_CHANNEL_NAME,new LoggingChannelOptions());
            _logChannel.LoggingEnabled += OnChannelLoggingEnabled;

            App.Current.Suspending += OnAppSuspending;
            App.Current.Resuming += OnAppResuming;

            ResumeLoggingIfApplicable();
        }

        void OnChannelLoggingEnabled(ILoggingChannel sender, object args)
        {
            // This method is called when the channel is informing us of channel-related state changes.
            // Save new channel state. These values can be used for advanced logging scenarios where, 
            // for example, it's desired to skip blocks of logging code if the channel is not being
            // consumed by any sessions. 
            _isChannelEnabled = sender.Enabled;
            //channelLoggingLevel = sender.Level;
        }

        public void StartLogging()
        {
            CheckDisposed();

            if (_logSession == null)
            {
                _logSession = new FileLoggingSession(DEFAULT_SESSION_NAME);
                _logSession.LogFileGenerated += LogFileGeneratedHandler;
            }

            _logSession.AddLoggingChannel(_logChannel, _channelLoggingLevel);
        }

        ~Logger()
        {
            Dispose(false);
        }

        private async void LogFileGeneratedHandler(IFileLoggingSession sender, LogFileGeneratedEventArgs args)
        {
            StorageFolder appDefinedLogFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(APP_LOG_FILE_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
            string newLogFileName = "Log-" + GetTimeStamp(true) + ".etl";
            await args.File.MoveAsync(appDefinedLogFolder, newLogFileName);
        }

        async void OnAppSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // Get a deferral before performing any async operations
            // to avoid suspension prior to LoggingScenario completing 
            // PrepareToSuspendAsync().
            var deferral = e.SuspendingOperation.GetDeferral();

            // Prepare logging for suspension.
            await PrepareToSuspendAsync();

            // From LoggingScenario's perspective, it's now okay to 
            // suspend, so release the deferral. 
            deferral.Complete();
        }

        void OnAppResuming(object sender, object e)
        {
            // If logging was active at the last suspend,
            // ResumeLoggingIfApplicable will re-activate 
            // logging.
            ResumeLoggingIfApplicable();
        }

        public void ResumeLoggingIfApplicable()
        {
            CheckDisposed();

            object loggingEnabled;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(LOGGING_ENABLED_SETTING_KEY_NAME, out loggingEnabled) == false)
            {
                ApplicationData.Current.LocalSettings.Values[LOGGING_ENABLED_SETTING_KEY_NAME] = true;
                loggingEnabled = ApplicationData.Current.LocalSettings.Values[LOGGING_ENABLED_SETTING_KEY_NAME];
            }

            if (loggingEnabled is bool && (bool)loggingEnabled == true)
            {
                StartLogging();
            }

            // When the sample suspends, it retains state as to whether or not it had
            // generated a new log file at the last suspension. This allows any
            // UI to be updated on resume to reflect that fact. 
            object LogFileGeneratedBeforeSuspendObject;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(LOGFILEGEN_BEFORE_SUSPEND_SETTING_KEY_NAME, out LogFileGeneratedBeforeSuspendObject) &&
                LogFileGeneratedBeforeSuspendObject != null &&
                LogFileGeneratedBeforeSuspendObject is string)
            {
                ApplicationData.Current.LocalSettings.Values[LOGFILEGEN_BEFORE_SUSPEND_SETTING_KEY_NAME] = null;
            }
        }

        public async Task PrepareToSuspendAsync()
        {
            CheckDisposed();

            if (_logSession != null)
            {
                IsPreparingForSuspend = true;

                try
                {
                    // Before suspend, save any final log file.
                    string finalFileBeforeSuspend = await CloseSessionSaveFinalLogFile();
                    _logSession.Dispose();
                    _logSession = null;
                    // Save values used when the app is resumed or started later.
                    // Logging is enabled.
                    ApplicationData.Current.LocalSettings.Values[LOGGING_ENABLED_SETTING_KEY_NAME] = true;
                    // Save the log file name saved at suspend so the sample UI can be 
                    // updated on resume with that information. 
                    ApplicationData.Current.LocalSettings.Values[LOGFILEGEN_BEFORE_SUSPEND_SETTING_KEY_NAME] = finalFileBeforeSuspend;
                }
                finally
                {
                    IsPreparingForSuspend = false;
                }
            }
            else
            {
                // Save values used when the app is resumed or started later.
                // Logging is not enabled and no log file was saved.
                ApplicationData.Current.LocalSettings.Values[LOGGING_ENABLED_SETTING_KEY_NAME] = false;
                ApplicationData.Current.LocalSettings.Values[LOGFILEGEN_BEFORE_SUSPEND_SETTING_KEY_NAME] = null;
            }
        }

        private async Task<string> CloseSessionSaveFinalLogFile()
        {
            // Save the final log file before closing the session.
            StorageFile finalFileBeforeSuspend = await _logSession.CloseAndSaveToFileAsync();
            if (finalFileBeforeSuspend != null)
            {
                // Get the the app-defined log file folder. 
                StorageFolder appDefinedLogFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(APP_LOG_FILE_FOLDER_NAME, CreationCollisionOption.OpenIfExists);
                // Create a new log file name based on a date/time stamp.
                string newLogFileName = "Log-" + GetTimeStamp(true) + ".etl";
                // Move the final log into the app-defined log file folder. 
                await finalFileBeforeSuspend.MoveAsync(appDefinedLogFolder, newLogFileName);
                // Return the path to the log folder.
                return System.IO.Path.Combine(appDefinedLogFolder.Path, newLogFileName);
            }
            else
            {
                return string.Empty;
            }
        }

        private string GetTimeStamp(bool forFileName = false)
        {
            DateTime now = DateTime.Now;
            if (forFileName)
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                     "{0:D4}{1:D2}{2:D2}_{3:D2}{4:D2}{5:D2}",
                                     now.Year,
                                     now.Month,
                                     now.Day,
                                     now.Hour,
                                     now.Minute,
                                     now.Second);
            }
            else
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                     "{0:D4}-{1:D2}-{2:D2}_{3:D2}:{4:D2}:{5:D2}:{6:D3}",
                                     now.Year,
                                     now.Month,
                                     now.Day,
                                     now.Hour,
                                     now.Minute,
                                     now.Second,
                                     now.Millisecond);
            }
        }

        
        public static Logger Instance
        {
            get
            {
                if (_loggerInstance == null)
                {
                    _loggerInstance = new Logger();
                }
                return _loggerInstance;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed == false)
            {
                _isDisposed = true;

                if (disposing)
                {
                    if (_logChannel != null)
                    {
                        _logChannel.Dispose();
                        _logChannel = null;
                    }

                    if (_logSession != null)
                    {
                        _logSession.Dispose();
                        _logSession = null;
                    }
                }
            }
        }

        

        /// <summary>
        /// Helper function for other methods to call to check Dispose() state.
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("Logger has been disposed before!");
            }
        }

        public void Info(string message)
        {
            _logChannel.LogMessage($"{GetTimeStamp()}[{LoggingLevel.Information}]:{message}", LoggingLevel.Information);
        }

        public void Error(string message)
        {
            _logChannel.LogMessage($"{GetTimeStamp()}[{LoggingLevel.Error}]:{message}", LoggingLevel.Error);
        }

        public void Warning(string message)
        {
            _logChannel.LogMessage($"{GetTimeStamp()}[{LoggingLevel.Warning}]:{message}", LoggingLevel.Warning);
        }

        public void Critical(string message)
        {
            _logChannel.LogMessage($"{GetTimeStamp()}[{LoggingLevel.Critical}]:{message}", LoggingLevel.Critical);
        }
    }
}
