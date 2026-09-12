using System.Windows;
using System.Windows.Threading;
using ImageReorder.Services;

namespace ImageReorder;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppLog.StartSession();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLog.Info($"--- Session end · exit code {e.ApplicationExitCode} ---");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Error("Unhandled exception on the UI thread.", e.Exception);

        // Keeping the app alive turns a crash-to-desktop into a reportable error.
        e.Handled = true;

        MessageBox.Show(
            $"{e.Exception.GetType().Name}: {e.Exception.Message}\n\nFull details were written to:\n{AppLog.LogFilePath}",
            "Image Reorder hit an error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        => AppLog.Fatal(
            $"Unhandled exception outside the UI thread (terminating: {e.IsTerminating}).",
            e.ExceptionObject as Exception);

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppLog.Error("Unobserved task exception.", e.Exception);
        e.SetObserved();
    }
}
