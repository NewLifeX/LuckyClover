using NewLife;
using NewLife.Log;
using NewLife.Threading;
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
        MachineInfo.RegisterAsync();

        var set = Setting.Current;
        var star = new StarFactory(set.Server, null, null);
        DefaultTracer.Instance = star.Tracer;

        StartClient();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new FrmMain());
    }

    const String APP_NAME = "LuckyClover";
    static TimerX _timer;
    static StarClient _Client;
    private static void StartClient()
    {
        var set = Setting.Current;
        var server = set.Server;
        if (server.IsNullOrEmpty()) return;

        XTrace.WriteLine("初始化服务端地址：{0}", server);

        var client = new StarClient(server)
        {
            Code = set.Code,
            Secret = set.Secret,
            ProductCode = APP_NAME,

            Setting = set,
            Log = XTrace.Log,
        };

        Application.ApplicationExit += (s, e) => client.Logout("ApplicationExit");

        // 可能需要多次尝试
        client.Open();

        _Client = client;
    }
}