using Microsoft.Toolkit.Uwp.Helpers;
using PresentVideoRecorder.Dialogs;
using PresentVideoRecorder.ViewModels.ContentDialogViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.Storage;
using Windows.UI.Popups;

namespace PresentVideoRecorder.Helpers
{
    public class DialogService : IDialogService
    {
        public async Task<bool> ShowConfirmMessage(string title, string messageContent)
        {
            return await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
            {
                var msgDialog = createNewMessageDialog(title, messageContent);
                if (msgDialog != null)
                {
                    msgDialog.Commands.Add(new UICommand(LocalizedStrings.GetResourceString("Yes")) { Id = 0 });
                    msgDialog.Commands.Add(new UICommand(LocalizedStrings.GetResourceString("No")) { Id = 1 });

                    msgDialog.DefaultCommandIndex = msgDialog.CancelCommandIndex = 1;

                    var result = await msgDialog.ShowAsync();
                    int resultId = -1;
                    if (int.TryParse(result.Id.ToString(), out resultId))
                    {
                        return resultId == 0 ? true : false;
                    }
                }
                return false;
            });
        }

        public async void ShowInformationMessage(string title, string messageContent)
        {
            await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
            {
                var msgDialog = createNewMessageDialog(title, messageContent);
                if (msgDialog != null)
                {
                    await msgDialog.ShowAsync();
                }
            });
        }

        public async Task<GraphicsCaptureItem> ShowGraphicsCapturePicker()
        {
            var picker = new GraphicsCapturePicker();
            var pickedItem = await picker.PickSingleItemAsync();
            return pickedItem;
        }

        private string ensureTitleContent(string passInTitle)
        {
            if (!string.IsNullOrEmpty(passInTitle) && !string.IsNullOrWhiteSpace(passInTitle))
            {
                return passInTitle.Trim();
            }
            return LocalizedStrings.GetResourceString("AppDisplayName");
        }

        private MessageDialog createNewMessageDialog(string title, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                return new MessageDialog(message, ensureTitleContent(title));
            }
            return null;
        }

        public async Task<IStorageFolder> ShowSaveFolderPicker()
        {
            return await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                folderPicker.FileTypeFilter.Add("*");

                var saveFolder = await folderPicker.PickSingleFolderAsync();
                if (saveFolder != null)
                {
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", saveFolder);
                    return saveFolder;
                }
                else
                {
                    return saveFolder;
                }
            });
        }

        public async Task<CreateNewCourseDialogViewModel> ShowCreateNewCourseDialog()
        {
            return await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
            {
                CreateNewCourseDialog createNewCourseDialog = new CreateNewCourseDialog();
                var dialogResult = await createNewCourseDialog.ShowAsync();
                if (dialogResult == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    return createNewCourseDialog.DataContext as CreateNewCourseDialogViewModel;
                }
                return null;
            });
        }

        public async Task<LoadCourseDialogModelViewModel> ShowLoadCourseDialog()
        {
            return await DispatcherHelper.ExecuteOnUIThreadAsync(async () =>
            {
                OpenExistedCourseContentDialog loadCourseDialog = new OpenExistedCourseContentDialog();
                var dialogResult = await loadCourseDialog.ShowAsync();
                if (dialogResult == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    return loadCourseDialog.DataContext as LoadCourseDialogModelViewModel;
                }
                return null;
            });
        }

    }

    public interface IDialogService
    {
        void ShowInformationMessage(string title, string messageContent);
        Task<bool> ShowConfirmMessage(string title, string messageContent);

        Task<GraphicsCaptureItem> ShowGraphicsCapturePicker();

        Task<IStorageFolder> ShowSaveFolderPicker();

        Task<CreateNewCourseDialogViewModel> ShowCreateNewCourseDialog();
        Task<LoadCourseDialogModelViewModel> ShowLoadCourseDialog();
    }
}
