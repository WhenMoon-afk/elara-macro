using ElaraMacro.Services;

namespace ElaraMacro;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show($"Unexpected UI error:\n\n{e.Exception}", "Elara Macro", MessageBoxButtons.OK, MessageBoxIcon.Error);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"Unexpected fatal error:\n\n{ex}", "Elara Macro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        Application.Run(new TrayApplicationContext());
    }
}
