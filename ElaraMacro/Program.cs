using System.Threading;
using ElaraMacro.Services;

namespace ElaraMacro;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstanceMutex = new Mutex(true, "ElaraMacro_SingleInstance", out var isPrimaryInstance);
        if (!isPrimaryInstance)
        {
            MessageBox.Show("ElaraMacro is already running.", "Elara Macro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var trayContext = new TrayApplicationContext();
        Application.Run(trayContext);
    }
}
