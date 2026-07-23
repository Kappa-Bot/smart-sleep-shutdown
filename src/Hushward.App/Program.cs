namespace Hushward.App;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        System.Windows.Forms.Application.SetHighDpiMode(
            System.Windows.Forms.HighDpiMode.PerMonitorV2);
        Velopack.VelopackApp.Build()
            .SetAppUserModelId("KappaBot.Hushward")
            .SetAutoApplyOnStartup(false)
            .OnBeforeUninstallFastCallback(_ => RemovePerUserRegistrations())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private static void RemovePerUserRegistrations()
    {
        var executablePath = Environment.ProcessPath ?? "Hushward.App.exe";
        new Hushward.Infrastructure.Startup.WindowsStartupRegistration(executablePath)
            .SetEnabledAsync(false, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        new Hushward.Infrastructure.Scheduling.WindowsTaskSchedulerSync(executablePath)
            .SynchronizeAsync([], CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
}
