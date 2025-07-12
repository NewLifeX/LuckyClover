using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LuckyClover;

#if NET20
delegate void Action();
#endif

internal class Program
{
    private static readonly Dictionary<String, Action> _menus = new(StringComparer.OrdinalIgnoreCase);
    private const String _baseUrl = "http://x.newlifex.com/dotnet";

    private static void Main(String[] args)
    {
        //XTrace.UseConsole();

        var cmd = "";
        if (args.Length >= 1) cmd = args[0];

        if (cmd != "zip" && cmd != "unzip" && cmd != "tar" && cmd != "untar")
        {
            var asm = Assembly.GetEntryAssembly();
            var ver = asm.GetName().Version + "";
            var fver = ver;
            var vers = asm.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            if (vers != null && vers.Length > 0)
            {
                var vatt = vers[0] as AssemblyFileVersionAttribute;
                if (!String.IsNullOrEmpty(vatt.Version)) fver = vatt.Version;
            }

            Log.WriteLine("幸运草 \e[31;1mLuckyClover\e[0m v{0}", fver);
            //Console.WriteLine("无依赖编译为linux-arm/linux-x86/windows，用于自动安装主流.NET运行时");
#if NETFRAMEWORK
            var atts = asm.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            Console.WriteLine((atts[0] as AssemblyDescriptionAttribute).Description);
#else
            Console.WriteLine(asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description);
#endif
            Console.WriteLine("{0}", Environment.OSVersion);
            Console.WriteLine("运行时：\e[34;1m{0}\e[0m", Environment.Version);
            Console.WriteLine("发布：{0:yyyy-MM-dd HH:mm:ss}", GetCompileTime(ver));
            Console.WriteLine();
        }

        var net = new NetRuntime
        {
            BaseUrl = _baseUrl,
            Hashs = NetRuntime.LoadMD5s(),
        };

        // 读取命令行，一般放在最后
        var flag = false;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("http"))
            {
                var url = args[i];
                if (url[url.Length - 1] == '/') url = url.Substring(0, url.Length - 1);
                net.BaseUrl = url;
                flag = true;

                break;
            }
        }
        // 读取本目录 server.txt
        if (!flag)
        {
            var f = Path.GetFullPath("server.txt");
            if (File.Exists(f))
            {
                var url = File.ReadAllText(f).Trim();
                if (!String.IsNullOrEmpty(url))
                {
                    if (url[url.Length - 1] == '/') url = url.Substring(0, url.Length - 1);
                    if (!url.EndsWith("/dotnet", true, null)) url += "/dotnet";
                    net.BaseUrl = url;
                    flag = true;
                }
            }
        }
        // 读取静默安装标记
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("-silent", StringComparison.InvariantCultureIgnoreCase))
            {
                net.Silent = true;

                break;
            }
        }

        // 根据操作系统，自动选择安装NET版本
        _menus["net"] = net.AutoInstallNet;

        _menus["net40"] = () => net.InstallNet40();
        _menus["net45"] = () => net.InstallNet45();
        _menus["net48"] = () => net.InstallNet48();

        var v6 = NetRuntime.Version6;
        var v7 = NetRuntime.Version7;
        var v8 = NetRuntime.Version8;
        var v9 = NetRuntime.Version9;
        _menus["net9"] = () => net.InstallNet("v9.0", v9, null);
        _menus["net9-desktop"] = () => net.InstallNet("v9.0", v9, "desktop");
        _menus["net9-aspnet"] = () => net.InstallNet("v9.0", v9, "aspnet");
        _menus["net9-host"] = () => net.InstallNet("v9.0", v9, "host");
        _menus["net8"] = () => net.InstallNet("v8.0", v8, null);
        _menus["net8-desktop"] = () => net.InstallNet("v8.0", v8, "desktop");
        _menus["net8-aspnet"] = () => net.InstallNet("v8.0", v8, "aspnet");
        _menus["net8-host"] = () => net.InstallNet("v8.0", v8, "host");
        _menus["net6"] = () => net.InstallNet("v6.0", v6, null);
        _menus["net6-desktop"] = () => net.InstallNet("v6.0", v6, "desktop");
        _menus["net6-aspnet"] = () => net.InstallNet("v6.0", v6, "aspnet");
        _menus["net6-host"] = () => net.InstallNet("v6.0", v6, "host");
        _menus["net7"] = () => net.InstallNet("v7.0", v7, null);
        _menus["net7-desktop"] = () => net.InstallNet("v7.0", v7, "desktop");
        _menus["net7-aspnet"] = () => net.InstallNet("v7.0", v7, "aspnet");
        _menus["net7-host"] = () => net.InstallNet("v7.0", v7, "host");

        _menus["md5"] = () => ShowMd5(args);
#if NET45_OR_GREATER || NETCOREAPP
        _menus["zip"] = () => new ZipCommand().Compress(args);
        _menus["unzip"] = () => new ZipCommand().Extract(args);
#endif
#if NET7_0_OR_GREATER
        _menus["tar"] = () => new TarCommand().Compress(args);
        _menus["untar"] = () => new TarCommand().Extract(args);
#endif

        if (String.IsNullOrEmpty(cmd)) cmd = ShowMenu();

        if (_menus.TryGetValue(cmd, out var func))
            func();
        else
            Console.WriteLine("无法识别命令：{0}", cmd);
    }

    private static String ShowMenu()
    {
        var vers = new List<VerInfo>();
        if (NetRuntime.IsWindows)
        {
            vers.AddRange(NetRuntime.Get1To45VersionFromRegistry());
            vers.AddRange(NetRuntime.Get45PlusFromRegistry());
        }
        vers.AddRange(NetRuntime.GetNetCore());

        Console.WriteLine("已安装版本：");
        foreach (var item in vers)
        {
            if (String.IsNullOrEmpty(item.Sp))
                Console.WriteLine("{0,-10}\t{1}", item.Name, item.Version);
            else
                Console.WriteLine("{0,-10}\t{1} Sp{2}", item.Name, item.Version, item.Sp);
        }
        Console.WriteLine("");

        Console.WriteLine("命令：\e[31;1mclover net20|net40|net45|net60|net80\e[0m");
        Console.WriteLine("");

        var ms = new String[_menus.Count];
        _menus.Keys.CopyTo(ms, 0);
        Console.WriteLine("可用命令：\e[34;1m{0}\e[0m", String.Join(", ", ms));

        var line = Console.ReadLine()?.Trim();

        return line;
    }

    private static void ShowMd5(String[] args)
    {
        var pt = "*.*";
        if (args != null && args.Length >= 2) pt = args[1];
        //Console.WriteLine("pt={0}", pt);

        var di = new DirectoryInfo("./");
        foreach (var fi in di.GetFiles(pt))
        {
            Console.WriteLine("{0}\t{1}", fi.Name, NetRuntime.GetMD5(fi.FullName));
        }
    }

    /// <summary>根据版本号计算得到编译时间</summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static DateTime GetCompileTime(String version)
    {
        var ss = version?.Split(['.']);
        if (ss == null || ss.Length < 4) return DateTime.MinValue;

        if (!Int32.TryParse(ss[2], out var d)) d = 0;
        if (!Int32.TryParse(ss[3], out var s)) s = 0;
        var y = DateTime.Today.Year;

        // 指定年月日的版本格式 1.0.yyyy.mmdd
        if (d <= y && d >= y - 10)
        {
            var dt = new DateTime(d, 1, 1);
            if (s > 0)
            {
                if (s >= 200) dt = dt.AddMonths(s / 100 - 1);
                s %= 100;
                if (s > 1) dt = dt.AddDays(s - 1);
            }

            return dt;
        }
        else
        {
            var dt = new DateTime(2000, 1, 1);
            dt = dt.AddDays(d).AddSeconds(s * 2);

            return dt;
        }
    }
}