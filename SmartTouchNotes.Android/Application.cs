using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace SmartTouchNotes.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
#if DIGITAL_INK
            App.CreateRecognizer = () => new AndroidInkRecognizer();
#endif
            return base.CustomizeAppBuilder(builder)
            .WithInterFont();
        }
    }
}
