using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace PresentVideoRecorder.Dialogs
{
    public sealed partial class OpenExistedCourseContentDialog : ContentDialog
    {
        public OpenExistedCourseContentDialog()
        {
            this.InitializeComponent();
            //this.Width = Window.Current.Bounds.Width;
        }
    }
}
