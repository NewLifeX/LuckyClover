using NewLife.Log;
using Stardust;

namespace Installer;

internal static class Program
{
    /// <summary>
    /// 应用程序的主入口点。
    /// </summary>
    [STAThread]
    static void Main()
    {
        XTrace.UseWinForm();

        var star = new StarFactory("http://s.newlifex.com:6600", null, null);
        DefaultTracer.Instance = star.Tracer;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new FrmMain());
    }
}