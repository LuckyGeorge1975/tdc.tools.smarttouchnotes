using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SmartTouchNotes.Core;
using SmartTouchNotes.Views;

namespace SmartTouchNotes;

public partial class App : Application
{
    public static Func<IInkRecognizer> CreateRecognizer { get; set; } = () => new UnavailableRecognizer();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        MainView CreateView()
        {
            var dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create);
            if (string.IsNullOrEmpty(dataDirectory))
                throw new InvalidOperationException("Kein lokales Anwendungsverzeichnis verfügbar.");
            return new MainView(new NoteStore(Path.Combine(dataDirectory, "SmartTouchNotes", "notes.json")),
                CreateRecognizer());
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow { Content = CreateView() };
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
            activity.MainViewFactory = CreateView;
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = CreateView();

        base.OnFrameworkInitializationCompleted();
    }
}
