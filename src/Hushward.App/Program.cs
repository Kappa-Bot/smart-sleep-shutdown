namespace Hushward.App;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        Velopack.VelopackApp.Build()
            .SetAppUserModelId("KappaBot.Hushward")
            .SetAutoApplyOnStartup(false)
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
